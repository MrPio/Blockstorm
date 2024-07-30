using ExtensionFunctions;
using Unity.Netcode;
using UnityEngine;
using VoxelEngine;

namespace Prefabs.SpawnCamera
{
    public class SpawnCamera : MonoBehaviour
    {
        private void Start()
        {
            var wm = GameObject.FindWithTag("WorldManager").GetComponent<WorldManager>();
            var cameraSpawn = wm.Map.cameraSpawns.RandomItem();
            transform.position = cameraSpawn.position;
            transform.rotation = Quaternion.Euler(cameraSpawn.rotation);

            // Render the map at the current position
            // wm.UpdatePlayerPos(cameraSpawn.position); TODO: enable this when the lobby is done and a menu is created

            // Disable spawn camera on player spawn
            NetworkManager.Singleton.OnClientConnectedCallback += _ => gameObject.SetActive(false);
        }
    }
}