using System;
using Prefabs.Player;
using UnityEngine;
using Weapon = Model.Weapon;

public class Collectable : MonoBehaviour
{
    [SerializeField] private GameObject ammoLight;
    [SerializeField] private GameObject hpLight;
    [SerializeField] private GameObject weaponLight;
    [SerializeField] private GameObject collectableContainer;
    [SerializeField] private AudioClip lootAudioClip;

    [NonSerialized]
    public Model.Collectable Model = new(global::Model.Collectable.CollectableType.Weapon, Weapon.Primaries[0]);

    private void Start()
    {
        ammoLight.SetActive(Model.Type is global::Model.Collectable.CollectableType.Ammo);
        hpLight.SetActive(Model.Type is global::Model.Collectable.CollectableType.Hp);
        weaponLight.SetActive(Model.Type is global::Model.Collectable.CollectableType.Weapon);
        var item = Resources.Load<GameObject>($"Prefabs/weapons/collectable/{Model.Item.Name.ToUpper()}");
        Instantiate(item, collectableContainer.transform);
        // TODO: spawn with random Z rotation
    }

    private void OnTriggerEnter(Collider other)
    {
        var player = other.gameObject.GetComponentInParent<Player>();
        if (player.IsOwner)
        {
            player.audioSource.PlayOneShot(lootAudioClip);
            Destroy(gameObject);
            // TODO: add to inventory, add HP, add Ammo
        }
    }
}