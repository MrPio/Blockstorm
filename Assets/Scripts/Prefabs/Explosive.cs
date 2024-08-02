using System;
using System.Collections;
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
    public class Explosive : NetworkBehaviour
    {
        private const float RangeMultiplierForDamage = 5f;
        private SceneManager _sm;

        public GameObject[] explosions;
        [SerializeField] public float delayFactor = 1f;
        [SerializeField] private GameObject damageText;
        [Range(1, 100)] [SerializeField] private float speed;
        [SerializeField] private MeshRenderer[] meshes;
        [SerializeField] private bool isMissile;
        [SerializeField] private ParticleSystem smoke = null;
        [NonSerialized] public ulong AttackerId;

        [NonSerialized] public float Damage, ExplosionTime, ExplosionRange, Delay;
        private Player.Player attackerPlayer;
        private bool _hasExploded;

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer && !IsHost) return;
            if (!isMissile || _hasExploded) return;

            // Prevent the missile to explode on the player itself
            var player = other.gameObject.GetComponentInParent<Player.Player>();
            if (player is null || player.OwnerClientId != AttackerId)
                Explode();
        }

        // Server/Host only
        private void Explode()
        {
            _hasExploded = true;
            foreach (var explosion in explosions)
                _sm.serverManager.SpawnPrefabServerRpc(
                    explosion.name,
                    transform.position,
                    Quaternion.identity.eulerAngles
                );
            HideRpc();
            Destroy(gameObject, 8f);

            // Destroy blocks
            var destroyedVoxels = _sm.worldManager.GetNeighborVoxels(transform.position, ExplosionRange);
            _sm.clientManager.EditVoxelClientRpc(destroyedVoxels.Select(it => (Vector3)it).ToArray(), 0);


            // Checks if there was a hit on an enemy
            var colliders = new Collider[100];
            Physics.OverlapSphereNonAlloc(transform.position, ExplosionRange * RangeMultiplierForDamage, colliders,
                1 << LayerMask.NameToLayer("Enemy"));
            var hitEnemies = new List<ulong>();
            foreach (var enemy in colliders.Where(it => it is not null))
            {
                var attackedPlayer = enemy.transform.GetComponentInParent<Player.Player>();
                if (hitEnemies.Contains(attackedPlayer.OwnerClientId))
                    continue;
                var distanceFactor =
                    1 - Vector3.Distance(enemy.transform.position, transform.position) /
                    (ExplosionRange * RangeMultiplierForDamage);
                var damage = (uint)(Damage * distanceFactor);

                if (!attackedPlayer.Status.Value.IsDead)
                {
                    // Check if the enemy is allied
                    if (attackedPlayer.IsOwner ||
                        attackedPlayer.Status.Value.Team != attackerPlayer.Status.Value.Team)
                    {
                        // Spawn the damage text
                        var damageTextGo = Instantiate(damageText, _sm.worldCanvas.transform);
                        damageTextGo.transform.position =
                            enemy.transform.position - attackerPlayer.cameraTransform.forward * 0.35f;
                        damageTextGo.transform.rotation = attackerPlayer.transform.rotation;
                        damageTextGo.GetComponent<FollowRotation>().follow = attackerPlayer.transform;
                        damageTextGo.GetComponentInChildren<TextMeshProUGUI>().Apply(text =>
                        {
                            text.text = damage.ToString();
                            text.color = Color.Lerp(Color.white, Color.red, distanceFactor);
                        });
                        damageTextGo.transform.localScale = Vector3.one * math.sqrt(distanceFactor + 0.5f);

                        // Send the damage to the enemy
                        attackedPlayer.DamageClientRpc(damage, enemy.transform.gameObject.name,
                            new NetVector3(transform.position - attackerPlayer.transform.position),
                            attackerPlayer.OwnerClientId, ragdollScale: 1.15f);
                        hitEnemies.Add(attackedPlayer.OwnerClientId);
                    }
                }
            }

            // Check if the player hit himself
            if (!attackerPlayer.Status.Value.IsDead)
            {
                var distanceFactor = 1 - Vector3.Distance(attackerPlayer.transform.position, transform.position) /
                    (ExplosionRange * RangeMultiplierForDamage);
                if (distanceFactor > 0)
                {
                    var damage = (uint)(attackerPlayer.Status.Value.Grenade!.Damage * distanceFactor);
                    attackerPlayer.DamageClientRpc(damage, "Chest",
                        new NetVector3(transform.position - attackerPlayer.transform.position),
                        attackerPlayer.OwnerClientId, ragdollScale: 1.15f);
                }
            }
        }

        [Rpc(SendTo.Everyone)]
        private void HideRpc()
        {
            if (smoke != null)
                smoke.Stop();
            foreach (var meshRenderer in meshes)
                meshRenderer.enabled = false;
            GetComponent<AudioSource>().Play();
        }

        [Rpc(SendTo.Everyone)]
        public void InitializeRpc(NetVector3 forward, uint damage, float explosionTime, float explosionRange,
            ulong attackerId, float force = 0)
        {
            if (!isMissile)
            {
                var rb = GetComponent<Rigidbody>();
                rb.AddForce(forward.ToVector3 * math.clamp(6.5f * force, 2.25f, 6.5f),
                    ForceMode.Impulse);
                rb.angularVelocity = VectorExtensions.RandomVector3(-60f, 60f);
            }

            Delay = force;
            Damage = damage;
            ExplosionTime = explosionTime;
            ExplosionRange = explosionRange;
            AttackerId = attackerId;

            if (isMissile)
                GetComponent<Rigidbody>().velocity = transform.forward * speed;

            _sm = FindObjectOfType<SceneManager>();
            attackerPlayer = FindObjectsOfType<Player.Player>().First(it => it.OwnerClientId == attackerId);

            if (IsServer || IsHost)
            {
                // Set explosion condition
                if (!isMissile)
                    InvokeRepeating(nameof(Explode), ExplosionTime - delayFactor * Delay, 9999);
            }
        }
    }
}