using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExtensionFunctions;
using JetBrains.Annotations;
using Managers;
using Managers.Encoder;
using Managers.Serializer;
using Model;
using Network;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using Collectable = Prefabs.Collectable;

namespace VoxelEngine
{
    public class WorldManager : MonoBehaviour
    {
        private SceneManager _sm;

        public byte BlockTypeIndex(string blockName) =>
            (byte)VoxelData.BlockTypes.ToList().FindIndex(it => it.name == blockName.ToLower());

        public Material material, transparentMaterial;
        [Range(1, 128)] public int chunkSize = 2;
        [Range(1, 512)] public int viewDistance = 2;
        public int atlasCount = 16;
        public float AtlasBlockSize => 1f / atlasCount;
        private Chunk[,] _chunks;
        [ItemCanBeNull] private Chunk[,] _nonSolidChunks;
        [CanBeNull] private readonly List<Chunk> _brokenChunks = new();
        [NonSerialized] public Map Map;
        private Vector3 _playerLastPos;
        [NonSerialized] public bool HasRendered;
        [SerializeField] private GameObject collectable;
        [SerializeField] private GameObject scoreCube;
        [NonSerialized] public List<NetVector3> FreeCollectablesSpawnPoints = new();
        [NonSerialized] public readonly List<Collectable> SpawnedCollectables = new();

        /// <summary>
        /// Set the render distance
        /// </summary>
        /// <param name="value"> A value normalized in [0, 1]. </param>
        public void SetRenderDistance(float value)
        {
            viewDistance = (int)math.lerp(32f, 256f, value);
            if (HasRendered)
                UpdatePlayerPos(_playerLastPos, force: true);
        }

        private void Start()
        {
            _sm = FindObjectOfType<SceneManager>();
            chunkSize = math.max(1, chunkSize);
            SetRenderDistance(BinarySerializer.Instance.Deserialize($"{ISerializer.ConfigsDir}/render_distance", 0.5f));
        }

        private async Task LoadMap(string mapName)
        {
            if (!Map.AvailableMaps.Contains(mapName))
                throw new Exception("This map does not exist! Check the Map.AvailableMaps list.");
            Map = Map.Serializer.Deserialize<Map>(ISerializer.MapsDir + mapName, null);
            if (Map is null)
            {
                _sm.logger.Log($"Downloading the map [{mapName}]...");
                // Download the map from Firebase storage
                var filePath = ISerializer.MapsDir + mapName + ".json.gz.gz";
                await _sm.storageManager.DownloadFileAsync(filePath);

                _sm.logger.Log($"Decompressing the map [{mapName}]...");
                // Decompress the file
                while (filePath.Contains(".gz"))
                    filePath = GzipEncoder.Instance.Decode(filePath);

                _sm.logger.Log($"Deserializing the map [{mapName}]...");
                // Load the newly downloaded map file
                Map = Map.Serializer.Deserialize<Map>(ISerializer.MapsDir + mapName, null);
            }

            Map.DeserializeMap();
        }

        public async Task RenderMap(string mapName)
        {
            await LoadMap(mapName);
            var mapSize = Map.size;
            var chunksX = Mathf.CeilToInt((float)mapSize.x / chunkSize);
            var chunksZ = Mathf.CeilToInt((float)mapSize.z / chunkSize);
            _chunks = new Chunk[chunksX, chunksZ];
            _nonSolidChunks = new Chunk[chunksX, chunksZ];
            for (var x = 0; x < chunksX; x++)
            for (var z = 0; z < chunksZ; z++)
            {
                _chunks[x, z] = new Chunk(new ChunkCoord(x, z, chunkSize), isSolid: true, this);

                _nonSolidChunks[x, z] = new Chunk(new ChunkCoord(x, z, chunkSize), isSolid: false, this);
                if (_nonSolidChunks[x, z].IsEmpty)
                {
                    Destroy(_nonSolidChunks[x, z].ChunkGo);
                    _nonSolidChunks[x, z] = null;
                }
            }

            HasRendered = true;
        }

        public bool IsVoxelInWorld(Vector3Int posNorm) =>
            posNorm.x >= 0 && posNorm.x < Map.size.x && posNorm.y >= 0 && posNorm.y < Map.size.y && posNorm.z >= 0 &&
            posNorm.z < Map.size.z;

        [CanBeNull]
        public BlockType GetVoxel(Vector3Int posNorm) =>
            IsVoxelInWorld(posNorm) ? VoxelData.BlockTypes[Map.Blocks[posNorm.y, posNorm.x, posNorm.z]] : null;

        // This is used to update the rendered chunks
        public void UpdatePlayerPos(Vector3 playerPos, bool force = false)
        {
            if (!HasRendered || (!force && Vector3.Distance(_playerLastPos, playerPos) < chunkSize * .9))
                return;
            _playerLastPos = playerPos;
            for (var x = 0; x < _chunks.GetLength(0); x++)
            for (var z = 0; z < _chunks.GetLength(1); z++)
            {
                _chunks[x, z].IsActive = math.abs(x * chunkSize - playerPos.x) < viewDistance &&
                                         math.abs(z * chunkSize - playerPos.z) < viewDistance;
                if (_nonSolidChunks[x, z] != null)
                    _nonSolidChunks[x, z].IsActive = _chunks[x, z].IsActive;
            }
        }

        [CanBeNull]
        public Chunk GetChunk(Vector3Int posNorm)
        {
            posNorm /= chunkSize;
            if (posNorm.x < _chunks.GetLength(0) && posNorm.z < _chunks.GetLength(1))
                return _chunks[posNorm.x, posNorm.z];
            return null;
        }

        public void EditVoxels(List<Vector3> positions, byte newID)
        {
            var posNorms = positions.Select(Vector3Int.FloorToInt).ToList();
            foreach (var posNorm in posNorms)
            {
                if (!IsVoxelInWorld(posNorm)) continue;
                if (VoxelData.BlockTypes[Map.Blocks[posNorm.y, posNorm.x, posNorm.z]].blockHealth !=
                    BlockHealth.Indestructible)
                    Map.Blocks[posNorm.y, posNorm.x, posNorm.z] = newID;
                Map.BlocksEdits[posNorm] = newID;
            }

            var chunks = new List<Chunk>();
            foreach (var posNorm in posNorms)
            {
                if (newID == 0)
                    CheckForFlyingMesh(posNorm);
                var chunk = GetChunk(posNorm);
                if (!chunks.Contains(chunk))
                {
                    chunks.Add(chunk);
                    chunk!.UpdateMesh();
                    chunks.AddRange(chunk!.UpdateAdjacentChunks(posNorms.ToArray()));
                }
            }
        }

        public bool DamageVoxel(Vector3 pos, uint damage)
        {
            var posNorm = Vector3Int.FloorToInt(pos);
            if (Map.DamageBlock(Vector3Int.FloorToInt(pos), damage) <= 0)
            {
                EditVoxels(new() { posNorm }, 0);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Starting from a given voxel, the neighboring voxels are visited recursively until
        /// a detached structure is found or until the bottom of the map is reached.
        /// </summary>
        /// <param name="posNorm">The position of the starting voxel.</param>
        /// <param name="visited">The list of already visited blocks.
        /// This is used to implement an acyclic <b>Depth-first</b> search.</param>
        /// <param name="stopVoxels">A list of voxels that, if found, invalidate the recursion.
        /// This is used to reduce computation when checking for flying structures around a destroyed voxel</param>
        /// <returns>
        /// A list of block positions if a flying structure was found, or null otherwise.
        /// </returns>
        private List<Vector3Int> GetAdjacentSolids(Vector3Int posNorm, List<Vector3Int> visited = null,
            List<Vector3Int> stopVoxels = null)
        {
            visited ??= new List<Vector3Int>();
            stopVoxels ??= new List<Vector3Int>();
            visited.Add(posNorm);
            var totalAdjacentSolids = new List<Vector3Int> { posNorm };

            // Explore all the neighbouring voxels.
            foreach (var adjacent in VoxelData.AdjacentVoxels)
            {
                var newPos = posNorm + adjacent;

                // Skip this voxel if already visited or terminate if is a stop voxel.
                if (visited.Contains(newPos))
                    continue;
                if (stopVoxels.Contains(newPos))
                    return null;

                // Get the voxel at the current position
                var adjacentVoxel = GetVoxel(newPos);

                // Terminate the recursion if an indestructible voxel is reached.
                if (newPos.y < 1 || adjacentVoxel is null ||
                    adjacentVoxel is { blockHealth: BlockHealth.Indestructible })
                    return null;

                // Skip the block if non-solid
                if (adjacentVoxel is not { isSolid: true })
                    continue;

                // Continue the depth-first search
                var adjacentSolids = GetAdjacentSolids(newPos, visited);

                // No flying structure can be found if any recursion cap condition is met.
                if (adjacentSolids == null)
                    return null;

                totalAdjacentSolids.AddRange(adjacentSolids);

                // Stop the recursion if when too many voxels have been visited.
                // This reduces the cost of recursion.
                if (totalAdjacentSolids.Count > 4000)
                    return null;
            }

            return totalAdjacentSolids;
        }

        /// <summary>
        /// Destroying a voxel can create a flying group of voxels.
        /// Therefore, for each neighbouring voxel we check if a flying structure has been created.
        /// A fall animation is created for each flying structure detected and those voxels are removed from their chunks.
        /// </summary>
        /// <param name="posNorm">The position of the destroyed block.</param>
        private void CheckForFlyingMesh(Vector3Int posNorm)
        {
            var chunksToUpdate = new List<Chunk>();
            var stopVoxels = new List<Vector3Int>();
            foreach (var adjacent in VoxelData.AdjacentVoxels)
            {
                // Skip if the block is an air block or an indestructible block.
                var newPos = posNorm + adjacent;
                var adjacentVoxel = GetVoxel(newPos);
                if (adjacentVoxel is { isSolid: false } or { blockHealth: BlockHealth.Indestructible })
                    continue;

                // Check if there is a flying structure that branches off from this neighboring voxel.
                var flyingBlocks = GetAdjacentSolids(posNorm + adjacent, stopVoxels: stopVoxels);

                // If no flying structure has been found, try the next neighboring voxel.
                if (flyingBlocks == null)
                    continue;

                // Remove the flying blocks from their chunks and update their meshes.
                stopVoxels.AddRange(flyingBlocks);
                var removedBlocks = new Dictionary<Vector3Int, byte>();
                foreach (var block in flyingBlocks)
                {
                    removedBlocks[new Vector3Int(block.x, block.y, block.z)] = Map.Blocks[block.y, block.x, block.z];
                    Map.Blocks[block.y, block.x, block.z] = 0;
                    Map.BlocksEdits[block] = 0;
                    var chunk = GetChunk(block);
                    if (!chunksToUpdate.Contains(chunk))
                        chunksToUpdate.Add(chunk);
                }

                // Spawn a falling group of voxels.
                _brokenChunks!.Add(new Chunk(removedBlocks, this));
            }

            foreach (var chunk in chunksToUpdate)
                chunk.UpdateMesh();
        }

        public List<Vector3Int> GetNeighborVoxels(Vector3 position, float range)
        {
            var voxels = new List<Vector3Int>();
            for (var x = position.x - range; x < position.x + range; x++)
            for (var y = position.y - range; y < position.y + range; y++)
            for (var z = position.z - range; z < position.z + range; z++)
            {
                var pos = new Vector3(x, y, z);
                if (Vector3.Distance(position + Vector3.one * 0.5f, pos) <= range)
                {
                    var posNorm = Vector3Int.FloorToInt(pos);
                    if (GetVoxel(posNorm) is { isSolid: true })
                        voxels.Add(posNorm);
                }
            }

            return voxels;
        }

        // Server only
        public void SpawnCollectables()
        {
            var spawnPoints = Resources.Load<GameObject>($"Prefabs/collectablePoints/{Map.name}");
            var transforms = spawnPoints.GetComponentsInChildren<Transform>().Where(it => it != spawnPoints.transform)
                .ToList();
            FreeCollectablesSpawnPoints = transforms.Select(it => (NetVector3)it.position).ToList();
            foreach (var collectablesSpawnPoint in
                     transforms.RandomSublist((int)(transforms.Count / 1.5)).ToList())
                SpawnCollectableWithID(collectablesSpawnPoint.position, log: false);
        }

        /// <summary>
        /// Spawn a collectable at the given ID location, if not already occupied by another collectable.
        /// This updates `FreeCollectablesSpawnPoints` and `SpawnedCollectables` lists  
        /// </summary>
        /// <param name="id"> The id of the new collectable. </param>
        /// <param name="model"> The collectable model. If not provided, a random collectable will be spawned. </param>
        public void SpawnCollectableWithID(NetVector3 id, Model.Collectable model = null, bool log = false)
        {
            if (SpawnedCollectables.Any(it => it.Model.ID == id)) return;
            if (log) Debug.Log($"Spawning collectable at {id}");
            var newCollectable =
                Instantiate(collectable, id, Quaternion.identity,
                    GameObject.FindGameObjectWithTag("CollectablesContainer").transform).GetComponent<Collectable>();
            SpawnedCollectables.Add(newCollectable);
            FreeCollectablesSpawnPoints.Remove(id);
            newCollectable.Initialize(model ?? Model.Collectable.GetRandomCollectable(id));
        }

        public void SpawnScoreCube()
        {
            var scoreCubeGo = Instantiate(scoreCube, (Vector3Int)Map.scoreCubePosition, Quaternion.identity);
            scoreCubeGo.GetComponent<NetworkObject>().Spawn(true);
        }
    }
}