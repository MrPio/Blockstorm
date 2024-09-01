using System;
using System.Linq;
using ExtensionFunctions;
using Managers;
using Network;
using Partials;
using Unity.Mathematics;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

namespace Prefabs
{
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(Destroyable))]
    public class Explosive : NetworkBehaviour
    {
        private const float RangeMultiplierForDamage = 4f;
        private SceneManager _sm;

        public GameObject[] explosions;
        [SerializeField] public float delayFactor = 1f;
        [SerializeField] private GameObject damageText;
        [Range(1, 100)] [SerializeField] private float speed;
        [SerializeField] private MeshRenderer[] meshes;
        [SerializeField] private bool isMissile;
        [SerializeField] private ParticleSystem smoke = null;
        [NonSerialized] public ulong AttackerId;
        [SerializeField] private Rigidbody rb;
        [SerializeField] private float maxVelocity = 100f;


        [NonSerialized] public float Damage, ExplosionTime, ExplosionRange, Delay, GroundDamageFactor;
        private Player.Player attackerPlayer;
        private bool _hasExploded;

        private void OnTriggerEnter(Collider other)
        {
            if (!IsHost) return;
            if (!isMissile || _hasExploded) return;

            // Prevent the missile to explode on the player itself
            var player = other.gameObject.GetComponentInParent<Player.Player>();
            if ((player is null || player.OwnerClientId != AttackerId) && !other.gameObject.CompareTag("ScoreCube"))
                Explode();
        }

        // Server/Host only
        private void Explode()
        {
            if (!IsHost) return;
            _hasExploded = true;
            var isSmokeOrGas = false;
            foreach (var explosion in explosions)
            {
                var go = Instantiate(explosion,
                    transform.position,
                    Quaternion.identity);
                go.GetComponent<NetworkObject>().Spawn(true);
                if (go.TryGetComponent(out Gas gas))
                {
                    isSmokeOrGas = true;
                    gas.AttackerId.Value = AttackerId;
                }
            }

            HideRpc(isSmokeOrGas);
            Destroy(gameObject, 8f);

            if (!isSmokeOrGas)
            {
                // Destroy blocks
                var destroyedVoxels =
                    _sm.worldManager.GetNeighborVoxels(transform.position, ExplosionRange * GroundDamageFactor);
                _sm.ClientManager.EditVoxelClientRpc(destroyedVoxels.Select(it => (Vector3)it).ToArray(), 0);

                // Check if any player was hit
                foreach (var player in FindObjectsOfType<Player.Player>())
                {
                    // Skip if the player is dead or inactive or allied with the attacker
                    if (!player.active.Value || player.Status.Value.IsDead || !(
                            player.OwnerClientId == attackerPlayer.OwnerClientId ||
                            player.Team != attackerPlayer.Team) || player.invincible.Value)
                        continue;
                    var distanceFactor = 1 - Vector3.Distance(player.transform.position, transform.position) /
                        (ExplosionRange * RangeMultiplierForDamage);
                    if (distanceFactor <= 0) continue;
                    var damage = (uint)(Damage * distanceFactor);
                    player.DamageClientRpc(damage, "Chest",
                        new NetVector3((transform.position - player.transform.position).normalized +
                                       VectorExtensions.RandomVector3(-0.25f, 0.25f)),
                        attackerPlayer.OwnerClientId, ragdollScale: 1.15f);
                }

                // Checks if there was a hit on a prop
                foreach (var prop in _sm.worldManager.SpawnedProps)
                {
                    if (prop.gameObject.IsDestroyed()) continue;

                    var distanceFactor = 1 - Vector3.Distance(prop.transform.position, transform.position) /
                        (ExplosionRange * GroundDamageFactor * 3);
                    if (distanceFactor <= 0) continue;
                    var damage = (uint)(Damage * distanceFactor) * 100;

                    // Broadcast the damage action
                    _sm.ClientManager.DamagePropRpc(prop.ID, damage, true, AttackerId);
                }
            }
        }

        [Rpc(SendTo.Everyone)]
        private void HideRpc(bool isSmoke)
        {
            if (smoke != null)
                smoke.Stop();
            foreach (var meshRenderer in meshes)
                meshRenderer.enabled = false;
            if (!isSmoke)
                GetComponent<AudioSource>().Play();
        }

        [Rpc(SendTo.Everyone)]
        public void InitializeRpc(NetVector3 forward, uint damage, float explosionTime, float explosionRange,
            float groundDamageFactor, ulong attackerId, float force = 0)
        {
            if (!isMissile)
            {
                rb.AddForce(forward.ToVector3 * math.clamp(6.5f * force, 2.25f, 6.5f),
                    ForceMode.Impulse);
                rb.angularVelocity = VectorExtensions.RandomVector3(-60f, 60f);
            }

            Delay = force;
            Damage = damage;
            ExplosionTime = explosionTime;
            ExplosionRange = explosionRange;
            GroundDamageFactor = groundDamageFactor;
            AttackerId = attackerId;

            if (isMissile)
                rb.velocity = transform.forward * speed;

            _sm = FindObjectOfType<SceneManager>();
            attackerPlayer = FindObjectsOfType<Player.Player>().First(it => it.OwnerClientId == attackerId);

            if (IsHost && !isMissile)
                InvokeRepeating(nameof(Explode), ExplosionTime - delayFactor * Delay, 9999);
        }

        private void Update()
        {
            if (rb.velocity.magnitude > maxVelocity)
                rb.velocity = rb.velocity.normalized * maxVelocity;
        }
    }
}