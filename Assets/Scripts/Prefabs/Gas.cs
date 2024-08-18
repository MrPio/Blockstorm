using System;
using System.Collections.Generic;
using ExtensionFunctions;
using Managers;
using Unity.Netcode;
using UnityEngine;
using Weapon = Model.Weapon;

namespace Prefabs
{
    [RequireComponent(typeof(AudioSource))]
    public class Gas : NetworkBehaviour
    {
        [SerializeField] private AudioClip smokeAudioClip;
        [SerializeField] private bool isGas;
        private SceneManager _sm;
        private Weapon _gasWeapon;
        private readonly List<Player.Player> insidePlayers = new();
        [NonSerialized] public NetworkVariable<ulong> AttackerId = new(0);

        private void Awake()
        {
            _sm = FindObjectOfType<SceneManager>();
            _gasWeapon = Weapon.Name2Weapon("gas");
        }

        public override void OnNetworkSpawn()
        {
            GetComponent<AudioSource>().PlayOneShot(smokeAudioClip);
            if (IsHost && isGas)
                InvokeRepeating(nameof(DamageClients), 0.25f, _gasWeapon.Delay);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsHost || !isGas) return;
            if (other.gameObject.CompareTag("Player"))
            {
                var player = other.gameObject.GetComponentInParent<Player.Player>();
                if (insidePlayers.Contains(player)) return;
                insidePlayers.Add(player);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsHost || !isGas) return;
            if (other.gameObject.CompareTag("Player"))
            {
                var player = other.gameObject.GetComponentInParent<Player.Player>();
                if (!insidePlayers.Contains(player)) return;
                insidePlayers.Remove(player);
            }
        }

        private void DamageClients()
        {
            List<Player.Player> toRemove = new();
            foreach (var player in insidePlayers)
            {
                if (!player.active.Value || player.Status.Value.IsDead)
                {
                    toRemove.Add(player);
                    continue;
                }

                player.DamageClientRpc(_gasWeapon.Damage, "Chest",
                    Vector3.up + VectorExtensions.RandomVector3(-0.25f, 0.25f), AttackerId.Value);
            }

            toRemove.ForEach(it => insidePlayers.Remove(it));
        }
    }
}