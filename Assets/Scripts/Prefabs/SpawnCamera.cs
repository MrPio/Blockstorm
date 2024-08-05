using ExtensionFunctions;
using Managers;
using Unity.Netcode;
using UnityEngine;

namespace Prefabs
{
    public class SpawnCamera : MonoBehaviour
    {
        private SceneManager _sm;

        public void InitializePosition()
        {
            _sm = FindObjectOfType<SceneManager>();
            var cameraSpawn = _sm.worldManager.Map.cameraSpawns.RandomItem();
            transform.position = cameraSpawn.position;
            transform.rotation = Quaternion.Euler(cameraSpawn.rotation);

            // Render the map at the current position
            // wm.UpdatePlayerPos(cameraSpawn.position); TODO: enable this when the lobby is done and a menu is created

        }
    }
}