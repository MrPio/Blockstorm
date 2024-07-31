using System.Linq;
using ExtensionFunctions;
using Network;
using UnityEngine;
using VoxelEngine;

public class Grenade : MonoBehaviour
{
    public GameObject[] explosions;
    [SerializeField] private float explosionTime = 2.5f;
    [SerializeField] private float explosionRange = 2f;
    private WorldManager _wm;
    private ClientManager _cm;

    private void Start()
    {
        InvokeRepeating(nameof(Explode), explosionTime, 9999);
        _wm = GameObject.FindWithTag("WorldManager").GetComponent<WorldManager>();
        _cm = GameObject.FindWithTag("ClientServerManagers").GetComponentInChildren<ClientManager>();
    }

    private void Explode()
    {
        foreach (var explosion in explosions)
            Instantiate(explosion, transform.position, Quaternion.identity);
        Destroy(gameObject, 1f);
        GetComponent<AudioSource>().Play();

        // Destroy blocks
        var destroyedVoxels = transform.position.GetNeighborVoxels(explosionRange);
        _cm.EditVoxelClientRpc(destroyedVoxels.Select(it => (Vector3)it).ToArray(), 0);
        
        // TODO: deal players damage
        // TODO: make grenade + effect network objects
    }
}