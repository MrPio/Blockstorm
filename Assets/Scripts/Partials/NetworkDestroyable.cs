using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Partials
{
    public class NetworkDestroyable : NetworkBehaviour
    {
        [SerializeField] private List<GameObject> ifIsMe = new(), ifIsNotMe = new();

        private void Start()
        {
            ifIsMe.ForEach(o =>
            {
                // o.SetActive(IsOwner);
                if (!IsOwner)
                    Destroy(o);
            });
            ifIsNotMe.ForEach(o =>
            {
                // o.SetActive(!IsOwner)
                if (IsOwner)
                    Destroy(o);
            });
        }

        public void SetEnabled(bool value)
        {
            if (IsOwner)
                ifIsMe.ForEach(o => o.SetActive(value));
            else
                ifIsNotMe.ForEach(o => o.SetActive(value));
        }
    }
}