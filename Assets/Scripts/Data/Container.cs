using System.Collections.Generic;
using Manager;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
public class Container : MonoBehaviour
{
    public static Voxel emptyVoxel = new() { id = 0 };
    public Vector3 containerPosition;
    private Dictionary<Vector3, Voxel> _data;
    private MeshRenderer _meshRenderer;
    private MeshFilter _meshFilter;
    private MeshCollider _meshCollider;
    private MeshData _meshData;

    public Voxel this[Vector3 index]
    {
        get => _data.ContainsKey(index) ? _data[index] : emptyVoxel;
        set
        {
            if (_data.ContainsKey(index))
                _data[index] = value;
            else _data.Add(index, value);
        }
    }

    public void ClearData()
    {
        _data.Clear();
    }

    public void Initialize(Material material, Vector3 position)
    {
        ConfigureComponents();
        _data = new Dictionary<Vector3, Voxel>();
        _meshRenderer.sharedMaterial = material;
        containerPosition = position;
    }

    private void ConfigureComponents()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
        _meshFilter = GetComponent<MeshFilter>();
        _meshCollider = GetComponent<MeshCollider>();
    }

    public void GenerateMesh()
    {
        _meshData.ClearData();

        var counter = 0;
        var faceVertices = new Vector3[4];
        var faceUVs = new Vector2[4];

        // Definition before foreach for performance purposes
        Vector3 blockPos;
        Voxel block;
        VoxelColor voxelColor;
        Color voxelColorAlpha;
        Vector2 voxelSmoothness;

        foreach (var kvp in _data)
        {
            // We don't need to draw this block if not visible
            if (!kvp.Value.IsSolid)
                continue;

            blockPos = kvp.Key;
            block = kvp.Value;
            voxelColor = WorldManager.Instance.worldColors[block.id - 1];
            voxelColorAlpha = voxelColor.color;
            voxelColorAlpha.a = 1;
            voxelSmoothness = new Vector2(voxelColor.metallic, voxelColor.smoothness);

            //Iterate over each face direction
            for (var i = 0; i < 6; i++)
            {
                // We don't need to draw this face if it is covered
                if (this[blockPos + VoxelFaceChecks[i]].IsSolid)
                    continue;

                //Draw this face
                //Collect the appropriate vertices from the default vertices and add the block position
                for (var j = 0; j < 4; j++)
                {
                    faceVertices[j] = VoxelVertices[VoxelVertexIndex[i, j]] + blockPos;
                    faceUVs[j] = VoxelUVs[j];
                }

                for (var j = 0; j < 6; j++)
                {
                    _meshData.vertices.Add(faceVertices[VoxelTris[i, j]]);
                    _meshData.UVs.Add(faceUVs[VoxelTris[i, j]]);
                    _meshData.colors.Add(voxelColorAlpha);
                    _meshData.UVs2.Add(voxelSmoothness);
                    _meshData.triangles.Add(counter++);
                }
            }
        }
    }

    public void UploadMesh()
    {
        _meshData.UploadMesh();

        if (_meshRenderer == null)
            ConfigureComponents();

        _meshFilter.mesh = _meshData.mesh;
        if (_meshData.vertices.Count > 3)
            _meshCollider.sharedMesh = _meshData.mesh;
    }

    #region Mesh Data

    public struct MeshData
    {
        public Mesh mesh;
        public List<Vector3> vertices;
        public List<int> triangles;
        public List<Vector2> UVs, UVs2;
        public List<Color> colors;
        public bool initialized;

        public void ClearData()
        {
            if (!initialized)
            {
                vertices = new List<Vector3>();
                triangles = new List<int>();
                UVs = new List<Vector2>();
                UVs2 = new List<Vector2>();
                colors = new List<Color>();

                initialized = true;
                mesh = new Mesh();
            }
            else
            {
                vertices.Clear();
                triangles.Clear();
                UVs.Clear();
                UVs2.Clear();
                colors.Clear();
                mesh.Clear();
            }
        }

        public void UploadMesh(bool sharedVertices = false)
        {
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0, false);
            mesh.SetUVs(0, UVs);
            mesh.SetUVs(2, UVs2);
            mesh.SetColors(colors);

            mesh.Optimize();

            mesh.RecalculateNormals();

            mesh.RecalculateBounds();

            mesh.UploadMeshData(false);
        }
    }

    #endregion

    #region Static Variables

    static readonly Vector3[] VoxelVertices = new Vector3[8]
    {
        new Vector3(0, 0, 0), //0
        new Vector3(1, 0, 0), //1
        new Vector3(0, 1, 0), //2
        new Vector3(1, 1, 0), //3

        new Vector3(0, 0, 1), //4
        new Vector3(1, 0, 1), //5
        new Vector3(0, 1, 1), //6
        new Vector3(1, 1, 1), //7
    };

    static readonly Vector3[] VoxelFaceChecks = new Vector3[6]
    {
        new Vector3(0, 0, -1), //back
        new Vector3(0, 0, 1), //front
        new Vector3(-1, 0, 0), //left
        new Vector3(1, 0, 0), //right
        new Vector3(0, -1, 0), //bottom
        new Vector3(0, 1, 0) //top
    };

    // To map the faces to the vertexes
    /*
     *    6-------7 
     *    |\       \
     *    | 2-------3
     *    4 |     5 |
     *     \0_______1
     */
    static readonly int[,] VoxelVertexIndex = new int[6, 4]
    {
        { 0, 1, 2, 3 },
        { 4, 5, 6, 7 },
        { 4, 0, 6, 2 },
        { 5, 1, 7, 3 },
        { 0, 1, 4, 5 },
        { 2, 3, 6, 7 },
    };

    static readonly Vector2[] VoxelUVs = new Vector2[4]
    {
        new Vector2(0, 0),
        new Vector2(0, 1),
        new Vector2(1, 0),
        new Vector2(1, 1)
    };

    static readonly int[,] VoxelTris = new int[6, 6]
    {
        { 0, 2, 3, 0, 3, 1 },
        { 0, 1, 2, 1, 3, 2 },
        { 0, 2, 3, 0, 3, 1 },
        { 0, 1, 2, 1, 3, 2 },
        { 0, 1, 2, 1, 3, 2 },
        { 0, 2, 3, 0, 3, 1 },
    };

    #endregion
}