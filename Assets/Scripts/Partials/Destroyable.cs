using UnityEngine;

namespace Partials
{
    public class Destroyable:MonoBehaviour
    {
        public float lifespan;
        public Destroyable(float lifespan)
        {
            this.lifespan = lifespan;
        }

        private float _startTime;

        private void Start()
        {
            _startTime = Time.time;
        }

        private void FixedUpdate()
        {
            if(Time.time-_startTime>lifespan)
                Destroy(gameObject);
        }
    }
}