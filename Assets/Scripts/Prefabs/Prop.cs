using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ExtensionFunctions;
using Managers;
using Model;
using Partials;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;
using Weapon = Model.Weapon;

namespace Prefabs
{
    [RequireComponent(typeof(AudioSource))]
    public class Prop : MonoBehaviour
    {
        private const float AnimationHitDuration = 0.3f;
        private const float AnimationHitScale = 0.25f;
        private SceneManager _sm;
        private Rigidbody _rb;
        private AudioSource _as;
        [NonSerialized] public ushort ID;
        [SerializeField] public float Hp = 200;
        [SerializeField] private string lootWeapon;
        [SerializeField] private AnimationCurve[] animationCurves;
        private float _acc = -1f;
        private Vector3 _startPosition;
        private List<AnimationCurve> _animationCurves;


        [Header("AudioClips")] [SerializeField]
        private List<AudioClip> hitAudioClips;

        [SerializeField] private AudioClip destructionAudioClip;

        private void Awake()
        {
            _sm = FindObjectOfType<SceneManager>();
            _rb = FindObjectOfType<Rigidbody>();
            _as = FindObjectOfType<AudioSource>();
            _rb.isKinematic = true;
        }

        public void Initialize()
        {
            _rb.isKinematic = false;
        }

        private void Update()
        {
            if (_acc > -0.01 && _acc < AnimationHitDuration)
            {
                _acc += Time.deltaTime;
                var t = _acc / AnimationHitDuration;
                transform.position = _startPosition +
                                     new Vector3(_animationCurves[0].Evaluate(t), _animationCurves[1].Evaluate(t),
                                         _animationCurves[2].Evaluate(t)) * AnimationHitScale;
            }
            else if (_acc > AnimationHitDuration)
            {
                // _rb.isKinematic = false;
                transform.position = _startPosition;
                _acc = -1;
            }
        }

        private void DestroyProp(bool explode = false, bool isMeTheAttacker = false)
        {
            if (_rb.IsDestroyed()) return;
            Destroy(_rb);
            foreach (var mesh in transform.GetComponentsInChildren<MeshRenderer>())
            {
                var rb = mesh.AddComponent<Rigidbody>();
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                rb.mass = 100;
                rb.AddForce(VectorExtensions.RandomVector3(-1f, 1f).normalized * (explode ? 1750f : 750f),
                    ForceMode.Impulse);
                rb.angularVelocity = VectorExtensions.RandomVector3(-1f, 1f).normalized *
                                     Random.Range(0, explode ? 100f : 30f);
            }

            gameObject.AddComponent<Destroyable>();
            if (isMeTheAttacker && lootWeapon is not null && lootWeapon != "")
                StartCoroutine(Collect());
            return;

            IEnumerator Collect()
            {
                yield return new WaitForSeconds(0.2f);
                _as.Play();
                var weapon = Weapon.Name2Weapon(lootWeapon);
                var player = FindObjectsOfType<Player.Player>().First(it => it.IsOwner);
                Collectable.LootCollectable(player,
                    new Model.Collectable(CollectableType.Weapon, Vector3.zero, weaponType: weapon.Type)
                        { WeaponItem = weapon });
            }
        }

        /// <summary>
        /// Take damage
        /// </summary>
        /// <param name="damage"> The damage amount</param>
        /// <param name="explode"> If the prop should explode</param>
        /// <param name="isMeTheAttacker">If I am the one who damaged the prop</param>
        /// <returns> Whenever the prop was destroyed.</returns>
        public bool Hit(uint damage, bool explode, bool isMeTheAttacker)
        {
            Hp -= damage;
            if (Hp <= 0)
            {
                _as.PlayOneShot(destructionAudioClip);
                DestroyProp(explode, isMeTheAttacker);
            }
            else
            {
                _as.PlayOneShot(hitAudioClips.RandomItem());
                // _rb.isKinematic = true;
                _animationCurves = animationCurves.ToList().RandomSublist(3);
                _acc = 0;
                _startPosition = transform.position;
            }

            return Hp <= 0;
        }
    }
}