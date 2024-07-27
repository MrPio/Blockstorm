using ExtensionFunctions;
using Unity.Netcode;
using UnityEngine;
using VoxelEngine;

public class SpawnCamera : MonoBehaviour
{
    /*public override void OnNetworkSpawn()
    {
        print($"Connected! {IsOwner}");
        if (IsOwner)
            gameObject.SetActive(false);
    }*/

    private void Start()
    {
        var wm = WorldManager.instance;
        var cameraSpawn = wm.map.cameraSpawns.RandomItem();
        transform.position = cameraSpawn.position;
        transform.rotation = Quaternion.Euler(cameraSpawn.rotation);

        // Render the map at the current position
        wm.UpdatePlayerPos(cameraSpawn.position);

        // Disable spawn camera on player spawn
        NetworkManager.Singleton.OnClientConnectedCallback += Disable;
    }

    private void Disable(ulong clientID)
    {
        gameObject.SetActive(false);
    }
}