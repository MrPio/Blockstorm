using System;
using ExtensionFunctions;
using Model;
using UnityEngine;
using Random = UnityEngine.Random;
using Weapon = Model.Weapon;

namespace Prefabs
{
    public class Collectable : MonoBehaviour
    {
        [SerializeField] private GameObject ammoLight;
        [SerializeField] private GameObject hpLight;
        [SerializeField] private GameObject weaponLight;
        [SerializeField] private GameObject collectableContainer;
        [SerializeField] private AudioClip lootAudioClip;

        [NonSerialized] public Model.Collectable Model = new(CollectableType.Weapon, Weapon.Primaries[0]);

        private void Start()
        {
            ammoLight.SetActive(Model.Type is CollectableType.Ammo);
            hpLight.SetActive(Model.Type is CollectableType.Hp);
            weaponLight.SetActive(Model.Type is CollectableType.Weapon);
            var item = Resources.Load<GameObject>($"Prefabs/weapons/collectable/{Model.Item.Name.ToUpper()}");
            Instantiate(item,
                Vector3.zero,
                Quaternion.Euler(0f, 0f, Random.Range(-180, 180f)),
                collectableContainer.transform);

            // TODO: spawn with random Z rotation
        }

        private void OnTriggerEnter(Collider other)
        {
            var player = other.gameObject.GetComponentInParent<Player.Player>();
            if (player.IsOwner)
            {
                player.audioSource.PlayOneShot(lootAudioClip);
                Destroy(gameObject);
                // TODO: add to inventory, add HP, add Ammo
            }
        }
    }
}