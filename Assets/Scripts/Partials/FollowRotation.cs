using JetBrains.Annotations;
using UnityEngine;

namespace Partials
{
    public class FollowRotation : MonoBehaviour
    {
        [CanBeNull] public Transform follow;

        private void Update()
        {
            if (follow)
                transform.rotation = follow.rotation;
        }
    }
}