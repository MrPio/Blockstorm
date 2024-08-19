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
            var cameraSpawn = _sm.worldManager.Map.cameraSpawns.RandomItem();
            transform.position = cameraSpawn.position;
            transform.rotation = Quaternion.Euler(cameraSpawn.rotation);

            // Render the map at the current position
            _sm.worldManager.UpdatePlayerPos(cameraSpawn.position);
        }
    }
}