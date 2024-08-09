using System;
using ExtensionFunctions;
using Managers;
using Unity.Netcode;
using UnityEngine;

namespace Prefabs
{
    public class SpawnCamera : MonoBehaviour
    {
        private SceneManager _sm;

        private void Awake()
        {
            _sm = FindObjectOfType<SceneManager>();
        }

        public void InitializePosition()
        {
            var cameraSpawn = _sm.worldManager.Map.cameraSpawns.RandomItem();
            transform.position = cameraSpawn.position;
            transform.rotation = Quaternion.Euler(cameraSpawn.rotation);

            // Render the map at the current position
            _sm.worldManager.UpdatePlayerPos(cameraSpawn.position);

        }
    }
}