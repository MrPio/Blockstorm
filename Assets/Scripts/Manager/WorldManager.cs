using UnityEngine;
using Random = UnityEngine.Random;

namespace Manager
{
    public class WorldManager : MonoBehaviour
    {
        public Material worldMaterial;
        public VoxelColor[] worldColors;
        private Container _container;

        private void Start()
        {
            if (_instance != null)
            {
                if (_instance != this)
                    Destroy(this);
            }
            else
                _instance = this;

            var containerGo = new GameObject("Container");
            containerGo.transform.parent = transform;
            _container = containerGo.AddComponent<Container>();
            _container.Initialize(worldMaterial, Vector3.zero);
            for (var x = 0; x < 16; x++)
            for (var z = 0; z < 16; z++)
            for (var y = 0; y < Random.Range(1, 8); y++)
                _container[new Vector3(x, y, z)] = new Voxel { id = 1 };

            _container.GenerateMesh();
            _container.UploadMesh();
        }

        private static WorldManager _instance;

        public static WorldManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<WorldManager>();
                return _instance;
            }
        }
    }
}