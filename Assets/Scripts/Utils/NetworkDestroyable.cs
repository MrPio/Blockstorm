using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Utils
{
    public class NetworkDestroyable : NetworkBehaviour
    {
        [SerializeField] private List<GameObject> ifIsMe = new(), ifIsNotMe = new();

        private void Start()
        {
            print(IsOwner);
            ifIsMe.ForEach(o => o.SetActive(IsOwner));
            ifIsNotMe.ForEach(o => o.SetActive(!IsOwner));
        }
    }
}