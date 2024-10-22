using System;
using System.Collections.Generic;
using System.Linq;
using Managers;
using Managers.Serializer;
using Model;
using UI;
using Unity.Mathematics;
using UnityEngine;
using VoxelEngine;

namespace Prefabs.Player
{
    /// <summary>
    /// Manage camera movement and place & dig block placements.
    /// Calls the Fire() Weapon method based on the type of the equipped weapon.
    /// </summary>
    /// <seealso cref="Weapon"/>
    public class CameraMovement : MonoBehaviour
    {
        public float FOVMain = 68, FOVWeapon = 54;
        private SceneManager _sm;

        [Header("Params")] [SerializeField] public float sensitivity, smoothing;
        [SerializeField] public float checkIncrement = 0.1f;

        [Header("Components")] [SerializeField]
        private CharacterController characterController;

        [SerializeField] public Transform playerTransform;
        [SerializeField] private Player player;
        [SerializeField] public Weapon weapon;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private Camera weaponCamera;

        [Header("AudioClips")] [SerializeField]
        private AudioClip blockDamageLightClip;

        [SerializeField] private AudioClip blockDamageMediumClip;
        [SerializeField] private AudioClip noBlockDamageClip;

        private Transform _transform;
        private bool _canDig, _canPlace;
        private float _lastDig, _lastPlace, _lastFire = -99f;
        private bool _isPlaceCursorBlocksStarted;
        private float Reach => weapon.WeaponModel?.Distance ?? 0f;
        private Vector2 _mouseLook;
        private Vector2 _smoothV;
        private float _throwingAcc;
        private bool _canThrow = true;
        private const float MaxThrowingAcc = 0.5f;
        private Camera _camera;

        public bool CanDig
        {
            set
            {
                _canDig = value;
                if (value)
                    _canPlace = false;
                if (_sm is null) return;
                _sm.highlightBlock.gameObject.SetActive(false);
                _sm.placeBlock.gameObject.SetActive(false);
            }
        }

        public bool CanPlace
        {
            set
            {
                _canPlace = (player.weapon.Magazine.TryGetValue("block", out var value1)
                    ? value1
                    : Model.Weapon.Blocks[0].Magazine) > 0 && value;
                if (value)
                    _canDig = false;
                if (_sm is null) return;
                _sm.highlightBlock.gameObject.SetActive(false);
                _sm.placeBlock.gameObject.SetActive(false);
            }
        }

        private void Awake()
        {
            _sm = FindObjectOfType<SceneManager>();
            _camera = GetComponent<Camera>();
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            _transform = transform;
            SetSensitivity(BinarySerializer.Instance.Deserialize($"{ISerializer.ConfigsDir}/{SliderSetting.configFiles[SliderSettingType.Volume]}{SliderSetting.configFiles[SliderSettingType.MouseSensitivity]}",
                               SliderSetting.defaultValues[SliderSettingType.MouseSensitivity] *
                               SliderSetting.steps[SliderSettingType.MouseSensitivity]) /
                           SliderSetting.steps[SliderSettingType.MouseSensitivity]);
            SetFOV(BinarySerializer.Instance.Deserialize($"{ISerializer.ConfigsDir}/{SliderSetting.configFiles[SliderSettingType.Volume]}{SliderSetting.configFiles[SliderSettingType.Fov]}",
                       SliderSetting.defaultValues[SliderSettingType.Fov] *
                       SliderSetting.steps[SliderSettingType.Fov]) /
                   SliderSetting.steps[SliderSettingType.Fov]);
        }

        private void LateUpdate()
        {
            if (_sm.pauseMenu.activeSelf)
                return;

            var mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

            // Handle camera rotation with smoothing
            _smoothV.x = Mathf.Lerp(_smoothV.x, mouseDelta.x, 1f / (smoothing + 1f));
            _smoothV.y = Mathf.Lerp(_smoothV.y, mouseDelta.y, 1f / (smoothing + 1f));

            // Calculate rotation based on mouse input and sensitivity
            _mouseLook += _smoothV * (sensitivity * Time.deltaTime * _camera.fieldOfView / FOVMain);

            // Clamp vertical rotation to prevent inverted view
            _mouseLook.y = Mathf.Clamp(_mouseLook.y, -90f, 90f);

            // Apply rotation to the camera
            transform.localRotation = Quaternion.AngleAxis(-_mouseLook.y, Vector3.right);
            playerTransform.rotation *= Quaternion.AngleAxis(_mouseLook.x, Vector3.up);
            _mouseLook -= Vector2.right * _mouseLook.x;
            player.CameraRotationX.Value = (byte)(-(int)_mouseLook.y + 135);

            // Handle green & red block indicators
            if ((_canDig || _canPlace) && !_isPlaceCursorBlocksStarted)
            {
                _isPlaceCursorBlocksStarted = true;
                InvokeRepeating(nameof(PlaceCursorBlocks), 0f, 0.1f);
            }
            else if (!_canDig && !_canPlace && _isPlaceCursorBlocksStarted)
            {
                _isPlaceCursorBlocksStarted = false;
                CancelInvoke(nameof(PlaceCursorBlocks));
            }

            // If I am digging with a melee weapon
            if (Input.GetMouseButton(0) && weapon.WeaponModel is { Type: WeaponType.Melee } &&
                Time.time - _lastDig > weapon.WeaponModel.Delay)
            {
                _lastDig = Time.time;
                weapon.Fire();
            }

            // If I am placing with an equipped block
            else if (Input.GetMouseButton(0) && weapon.WeaponModel is { Type: WeaponType.Block } &&
                     _sm.placeBlock.gameObject.activeSelf &&
                     Time.time - _lastPlace > weapon.WeaponModel.Delay)
            {
                _lastPlace = Time.time;
                if (weapon.Magazine[weapon.WeaponModel.GetNetName] > 0 || Input.GetMouseButtonDown(0))
                    weapon.Fire();
                _sm.placeBlock.gameObject.SetActive(false);
            }

            // If I am firing with a weapon
            else if (Input.GetMouseButton(0) && (weapon.WeaponModel?.IsGun ?? false) &&
                     Time.time - _lastFire > weapon.WeaponModel.Delay)
            {
                if (weapon.Magazine[weapon.WeaponModel.GetNetName] > 0)
                {
                    _lastFire = Time.time;
                    weapon.Fire();
                }
                else if (Input.GetMouseButtonDown(0))
                    weapon.audioSource.PlayOneShot(weapon.noAmmoClip);
            }

            // If I am throwing a grenade
            else if (Input.GetKey(KeyCode.G))
            {
                _lastFire = Time.time;
                _throwingAcc += Time.deltaTime;
            }

            // Throw the grenade
            if ((Input.GetKeyUp(KeyCode.G) || _throwingAcc > MaxThrowingAcc) &&
                player.Status.Value.Grenade is not null && player.Status.Value.LeftGrenades > 0)
            {
                _lastFire = Time.time;
                if (_canThrow)
                    weapon.ThrowGrenade(_throwingAcc / MaxThrowingAcc);
                _canThrow = false;
                _throwingAcc = 0;
            }

            // Throw the secondary grenade
            if ((Input.GetKeyUp(KeyCode.H) || _throwingAcc > MaxThrowingAcc) &&
                player.Status.Value.GrenadeSecondary is not null && player.Status.Value.LeftSecondaryGrenades > 0)
            {
                _lastFire = Time.time;
                if (_canThrow)
                    weapon.ThrowGrenade(_throwingAcc / MaxThrowingAcc, true);
                _canThrow = false;
                _throwingAcc = 0;
            }

            if (Input.GetKeyUp(KeyCode.G))
                _canThrow = true;

            if (Input.GetMouseButtonUp(0) && weapon.WeaponModel is { Type: WeaponType.Block } && _canPlace)
                _lastPlace -= weapon.WeaponModel.Delay * 0.65f;

            if (Input.GetMouseButtonUp(0) && _sm.blockDigEffect.isPlaying)
                _sm.blockDigEffect.Stop();

            // Handle HighlightArea
            if (weapon.WeaponModel is not null && weapon.WeaponModel.Name.ToUpper() == "TACT")
            {
                var newHighlightAreaPos =
                    _transform.position + new Vector3(_transform.forward.x + 0.01f, 0, _transform.forward.z + 0.01f)
                        .normalized * 80;
                var ray = new Ray(_transform.position + _transform.forward * 0.45f, _transform.forward);
                if (Physics.Raycast(ray, out var hit, 80, 1 << LayerMask.NameToLayer("Ground")))
                    if (hit.collider is not null)
                        newHighlightAreaPos = new Vector3(hit.point.x, 0, hit.point.z);
                _sm.highlightArea.transform.position =
                    new Vector3(newHighlightAreaPos.x, _sm.highlightArea.transform.position.y, newHighlightAreaPos.z);
                _sm.highlightArea.SetRange(Input.GetMouseButton(1));
            }
        }

        /**
     * We use voxels, so we don't have a collider in every cube.
     * The strategy used here is to beam a ray of incremental length until a solid
     * voxel location is reached or the iteration ends.
     */
        private void PlaceCursorBlocks()
        {
            var lastPos = Vector3Int.FloorToInt(_transform.position);
            for (var length = checkIncrement; length < Reach; length += checkIncrement)
            {
                var pos = Vector3Int.FloorToInt(_transform.position + _transform.forward * (length));
                var blockType = _sm.worldManager.GetVoxel(pos);
                if (_canDig && blockType != null && blockType.name != "air")
                {
                    if (!blockType.isSolid) return;
                    _sm.highlightBlock.position = pos + Vector3.one * 0.5f;
                    _sm.highlightBlock.gameObject.SetActive(true);
                    _sm.placeBlock.gameObject.SetActive(false);
                    return;
                }

                if (_canPlace && blockType is { isSolid: true })
                {
                    var newPos = lastPos + Vector3.one * 0.5f;
                    if (_sm.worldManager.GetVoxel(Vector3Int.FloorToInt(newPos))?.name != "air")
                        return;
                    var characterPos = characterController.transform.position;
                    var distanceXZ = Vector3.Distance(new Vector3(newPos.x, 0, newPos.z),
                        new Vector3(characterPos.x, 0, characterPos.z));
                    var distanceY = math.abs(newPos.y - characterPos.y);
                    var intersect = distanceXZ < characterController.radius + 0.25f &&
                                    distanceY < characterController.height - 0.2f;
                    _sm.placeBlock.position = newPos;

                    // Check enemy intersection
                    if (Physics.CheckSphere(newPos, 0.5f, 1 << LayerMask.NameToLayer("Enemy")))
                    {
                        _sm.placeBlock.gameObject.SetActive(false);
                        _sm.highlightBlock.gameObject.SetActive(false);
                        return;
                    }

                    _sm.placeBlock.gameObject.SetActive(!intersect);
                    _sm.highlightBlock.gameObject.SetActive(false);
                    return;
                }

                lastPos = pos;
            }

            _sm.highlightBlock.gameObject.SetActive(false);
            _sm.placeBlock.gameObject.SetActive(false);
        }

        /// <summary>
        /// Set the camera sensitivity
        /// </summary>
        /// <param name="value"> A value normalized in [0, 1]. </param>
        public void SetSensitivity(float value) =>
            sensitivity = (value * 4f + 0.25f) * 100f;

        /// <summary>
        /// Set the fov
        /// </summary>
        /// <param name="value"> A value normalized in [0, 1]. </param>
        public void SetFOV(float value)
        {
            FOVMain = math.lerp(68 - 10f, 68 + 10f, value);
            FOVWeapon = math.lerp(52 - 6f, 52 + 6f, value);
            _camera.fieldOfView = FOVMain;
            weaponCamera.fieldOfView = FOVWeapon;
        }
    }
}