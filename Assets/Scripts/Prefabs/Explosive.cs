using System;
using System.Collections.Generic;
using System.Linq;
using ExtensionFunctions;
using Managers;
using Network;
using Partials;
using TMPro;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

namespace Prefabs
{
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(Destroyable))]
    public class Explosive : MonoBehaviour
    {
        private SceneManager _sm;

        public GameObject[] explosions;
        [SerializeField] public float delayFactor = 1f;
        [SerializeField] private GameObject damageText;
        [Range(1, 100)] [SerializeField] private float speed;
        [SerializeField] private MeshRenderer[] meshes;
        [SerializeField] private bool isMissile;

        [NonSerialized] public float Damage, ExplosionTime, ExplosionRange, Delay;
        private Player.Player player;

        private void Start()
        {
            _sm = FindObjectOfType<SceneManager>();
            player = GameObject.FindGameObjectsWithTag("Player").First(it => it.GetComponent<NetworkObject>().IsOwner)
                .GetComponent<Player.Player>();

            // Set explosion condition
            if (isMissile)
                GetComponent<Rigidbody>().velocity = transform.forward * speed;
            else
                InvokeRepeating(nameof(Explode), ExplosionTime - delayFactor * Delay, 9999);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isMissile) return;

            // Prevent the missile to explode on the player itself
            if (!other.gameObject.GetComponent<Player.Player>()?.IsOwner ?? true)
                Explode();
        }

        private void Explode()
        {
            foreach (var explosion in explosions)
            {
                var go = Instantiate(explosion, transform.position, Quaternion.identity);
                go.GetComponent<NetworkObject>().Spawn();
            }

            foreach (var meshRenderer in meshes)
                meshRenderer.enabled = false;
            Destroy(gameObject, 10f);
            GetComponent<AudioSource>().Play();

            // Destroy blocks
            var destroyedVoxels = transform.position.GetNeighborVoxels(ExplosionRange);
            _sm.clientManager.EditVoxelClientRpc(destroyedVoxels.Select(it => (Vector3)it).ToArray(), 0);


            // Checks if there was a hit on an enemy
            var colliders = new Collider[100];
            Physics.OverlapSphereNonAlloc(transform.position, ExplosionRange, colliders,
                1 << LayerMask.NameToLayer("Enemy"));
            var hitEnemies = new List<ulong>();
            foreach (var enemy in colliders.Where(it => it is not null))
            {
                var attackedPlayer = enemy.transform.GetComponentInParent<Player.Player>();
                if (hitEnemies.Contains(attackedPlayer.OwnerClientId))
                    continue;
                var distanceFactor =
                    1 - Vector3.Distance(enemy.transform.position, transform.position) / (ExplosionRange * 2.5f);
                var damage = (uint)(Damage * distanceFactor);

                if (!attackedPlayer.Status.Value.IsDead)
                {
                    // Spawn the damage text
                    var damageTextGo = Instantiate(damageText, _sm.worldCanvas.transform);
                    damageTextGo.transform.position = enemy.transform.position - player.cameraTransform.forward * 0.35f;
                    damageTextGo.transform.rotation = player.transform.rotation;
                    damageTextGo.GetComponent<FollowRotation>().follow = player.transform;
                    damageTextGo.GetComponentInChildren<TextMeshProUGUI>().Apply(text =>
                    {
                        text.text = damage.ToString();
                        text.color = Color.Lerp(Color.white, Color.red, distanceFactor);
                    });
                    damageTextGo.transform.localScale = Vector3.one * math.sqrt(distanceFactor + 0.5f);

                    // Send the damage to the enemy
                    attackedPlayer.DamageClientRpc(damage, enemy.transform.gameObject.name,
                        new NetVector3(transform.position - player.transform.position),
                        player.OwnerClientId, ragdollScale: 1.15f);
                    hitEnemies.Add(attackedPlayer.OwnerClientId);
                }
            }

            // Check if the player hit himself
            if (!player.Status.Value.IsDead)
            {
                var distanceFactor = 1 - Vector3.Distance(player.transform.position, transform.position) /
                    (ExplosionRange * 2.5f);
                if (distanceFactor > 0)
                {
                    var damage = (uint)(player.Status.Value.Grenade!.Damage * distanceFactor);
                    player.DamageClientRpc(damage, "Chest",
                        new NetVector3(transform.position - player.transform.position),
                        player.OwnerClientId, ragdollScale: 1.15f);
                }
            }
        }
    }
}