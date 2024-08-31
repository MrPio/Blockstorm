using System;
using ExtensionFunctions;
using Managers;
using Model;
using Unity.Netcode;
using UnityEngine;
using Utils;
using VoxelEngine;

namespace Prefabs
{
    public class SpawnCamera : MonoBehaviour
    {
        private SceneManager _sm;

        private void Awake()
        {
            _sm = FindObjectOfType<SceneManager>();
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Space))
                InitializePosition();
        }

        private void OnEnable()
        {
            InitializePosition();
        }

        public void InitializePosition()
        {
            if (!_sm.worldManager.HasRendered) return;
            var map = _sm.worldManager.Map;
            var cameraSpawn = map.cameraSpawns.Count > 0
                ? map.cameraSpawns.RandomItem()
                : new CameraSpawn(new SerializableVector3(map.size.x / 2f, 60, map.size.z / 2f),
                    new SerializableVector3(90, 0, 0), new Utils.Nullable<Team>(Team.None, false));
            transform.position = cameraSpawn.position;
            transform.rotation = Quaternion.Euler(cameraSpawn.rotation);

            // Render the map at the current position
            _sm.worldManager.UpdatePlayerPos(cameraSpawn.position);
        }
    }
}