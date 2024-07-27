using UnityEngine;

namespace Partials
{
    public class Activable : MonoBehaviour
    {
        [SerializeField] private string animationOnEnable;
        public void Disable() => gameObject.SetActive(false);

        public void Enable() => gameObject.SetActive(true);

        private void OnEnable()
        {
            if(animationOnEnable!=null)
                GetComponent<Animator>().SetTrigger(Animator.StringToHash(animationOnEnable));
        }
    }
}