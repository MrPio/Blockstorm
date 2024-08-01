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
    public class Grenade : MonoBehaviour
    {
        private SceneManager _sm;

        public GameObject[] explosions;
        [SerializeField] public float delayFactor = 1f;
        [SerializeField] private GameObject damageText;
        [NonSerialized] public float ExplosionTime;
        [NonSerialized] public float ExplosionRange;
        private Player.Player player;
        [NonSerialized] public float Delay;

        private void Start()
        {
            _sm = FindObjectOfType<SceneManager>();
            InvokeRepeating(nameof(Explode), ExplosionTime - delayFactor * Delay, 9999);
            player = GameObject.FindGameObjectsWithTag("Player").First(it => it.GetComponent<NetworkObject>().IsOwner)
                .GetComponent<Player.Player>();
        }

        private void Explode()
        {
            foreach (var explosion in explosions)
            {
                var go = Instantiate(explosion, transform.position, Quaternion.identity);
                go.GetComponent<NetworkObject>().Spawn();
            }

            Destroy(gameObject, 1f);
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
                var damage = (uint)(player.Status.Value.Grenade!.Damage * distanceFactor);

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