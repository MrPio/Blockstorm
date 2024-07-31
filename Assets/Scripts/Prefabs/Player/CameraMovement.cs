using System;
using System.Collections.Generic;
using System.Linq;
using Managers;
using Model;
using Network;
using Unity.Mathematics;
using UnityEngine;
using VoxelEngine;

namespace Prefabs.Player
{
    public class CameraMovement : MonoBehaviour
    {
        public const float FOVMain = 68, FOVWeapon = 44;

        [SerializeField] private CharacterController characterController;
        public float sensitivity, smoothing;
        public Transform playerTransform;
        [SerializeField] private Player player;
        public float checkIncrement = 0.1f;
        public Weapon weapon;
        [SerializeField] private AudioClip blockDamageLightClip, blockDamageMediumClip, noBlockDamageClip;
        [SerializeField] private AudioSource audioSource;

        private Transform _highlightBlock, _placeBlock;
        private WorldManager _wm;
        private Transform _transform;
        private bool _canDig, _canPlace;
        private float _lastDig, _lastPlace, _lastFire;
        private ParticleSystem _blockDigEffect;
        private bool _isPlaceCursorBlocksStarted;
        private float Reach => weapon.WeaponModel?.Distance ?? 0f;
        private ClientManager _clientManager;
        private Vector2 _mouseLook;
        private Vector2 _smoothV;
        private float _throwingAcc;
        private bool _canThrow = true;
        private const float MaxThrowingAcc = 0.5f;

        public bool CanDig
        {
            get => _canDig;
            set
            {
                _canDig = value;
                if (value)
                    _canPlace = false;
                if (_highlightBlock is null || _placeBlock is null) return;
                _highlightBlock.gameObject.SetActive(false);
                _placeBlock.gameObject.SetActive(false);
            }
        }

        public bool CanPlace
        {
            get => _canPlace;
            set
            {
                _canPlace = InventoryManager.Instance.Blocks > 0 && value;
                if (value)
                    _canDig = false;
                if (_highlightBlock is null || _placeBlock is null) return;
                _highlightBlock.gameObject.SetActive(false);
                _placeBlock.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            _blockDigEffect = GameObject.FindWithTag("BlockDigEffect").GetComponent<ParticleSystem>();
            _clientManager = GameObject.FindWithTag("ClientServerManagers").GetComponentInChildren<ClientManager>();
            _highlightBlock = _clientManager.HighlightBlock;
            _placeBlock = _clientManager.PlaceBlock;
            _highlightBlock.gameObject.SetActive(false);
            _placeBlock.gameObject.SetActive(false);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            _wm = GameObject.FindWithTag("WorldManager").GetComponent<WorldManager>();
            _transform = transform;
        }

        private void LateUpdate()
        {
            var mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

            // Handle camera rotation with smoothing
            _smoothV.x = Mathf.Lerp(_smoothV.x, mouseDelta.x, 1f / (smoothing + 1f));
            _smoothV.y = Mathf.Lerp(_smoothV.y, mouseDelta.y, 1f / (smoothing + 1f));

            // Calculate rotation based on mouse input and sensitivity
            _mouseLook += _smoothV * (sensitivity * Time.deltaTime);

            // Clamp vertical rotation to prevent inverted view
            _mouseLook.y = Mathf.Clamp(_mouseLook.y, -90f, 90f);

            // Apply rotation to the camera
            transform.localRotation = Quaternion.AngleAxis(-_mouseLook.y, Vector3.right);
            playerTransform.rotation *= Quaternion.AngleAxis(_mouseLook.x, Vector3.up);
            _mouseLook -= Vector2.right * _mouseLook.x;
            player.CameraRotationX.Value = (byte)(-(int)_mouseLook.y + 128);

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
            if (weapon.WeaponModel is { Type: WeaponType.Melee } &&
                Input.GetMouseButton(0) && Time.time - _lastDig > weapon.WeaponModel.Delay)
            {
                _lastDig = Time.time;
                if (_highlightBlock.gameObject.activeSelf)
                {
                    var highlightBlockPos = _highlightBlock.transform.position;
                    var pos = Vector3Int.FloorToInt(highlightBlockPos);
                    var block = _wm.Map.GetBlock(pos);
                    _blockDigEffect.transform.position = highlightBlockPos;
                    _blockDigEffect.GetComponent<Renderer>().material =
                        Resources.Load<Material>(
                            $"Textures/texturepacks/blockade/Materials/blockade_{(block.topID + 1):D1}");
                    _blockDigEffect.Play();
                    if (new List<string> { "crate", "crate", "window", "hay", "barrel", "log" }.Any(it =>
                            block.name.Contains(it)))
                        audioSource.PlayOneShot(blockDamageLightClip);
                    else if (block.blockHealth == BlockHealth.Indestructible)
                        audioSource.PlayOneShot(noBlockDamageClip);
                    else
                        audioSource.PlayOneShot(blockDamageMediumClip);
                    _clientManager.DamageVoxelRpc(_highlightBlock.transform.position,
                        InventoryManager.Instance.Melee!.Damage);
                }

                weapon.Fire();
            }

            // If I am placing with an equipped block
            else if (weapon.WeaponModel is { Type: WeaponType.Block } && _placeBlock.gameObject.activeSelf &&
                     Input.GetMouseButton(0) &&
                     Time.time - _lastPlace > weapon.WeaponModel.Delay)
            {
                _lastPlace = Time.time;
                _clientManager.EditVoxelClientRpc(new[] { _placeBlock.transform.position },
                    InventoryManager.Instance.BlockId);
                // _wm.EditVoxel(_placeBlock.transform.position, InventoryManager.Instance.blockId);
                _placeBlock.gameObject.SetActive(false);
                weapon.Fire();
            }

            // If I am firing with a weapon
            else if (weapon.WeaponModel is { Type: WeaponType.Primary } or { Type: WeaponType.Secondary } or
                         { Type: WeaponType.Tertiary } && Input.GetMouseButton(0) &&
                     Time.time - _lastFire > weapon.WeaponModel.Delay)
            {
                _lastFire = Time.time;
                weapon.Fire();
            }

            // If I am throwing a grenade
            else if (Input.GetKey(KeyCode.G))
            {
                _lastFire = Time.time;
                _throwingAcc += Time.deltaTime;
            }

            // Throw the grenade
            if ((_throwingAcc > MaxThrowingAcc || Input.GetKeyUp(KeyCode.G)) &&
                InventoryManager.Instance.Grenade is not null && InventoryManager.Instance.LeftGrenades > 0)
            {
                _lastFire = Time.time;
                if (_canThrow)
                    weapon.ThrowGrenade(_throwingAcc / MaxThrowingAcc);
                _canThrow = false;
                _throwingAcc = 0;
            }

            if (Input.GetKeyUp(KeyCode.G))
                _canThrow = true;

            if (weapon.WeaponModel is { Type: WeaponType.Block } && _canPlace && Input.GetMouseButtonUp(0))
                _lastPlace -= weapon.WeaponModel.Delay * 0.65f;

            if (_blockDigEffect.isPlaying && Input.GetMouseButtonUp(0))
                _blockDigEffect.Stop();
        }

        /**
     * We use voxels, so we don't have a collider in every cube.
     * The strategy used here is to beam a ray of incremental length until a solid
     * voxel location is reached or the iteration ends.
     */
        private void PlaceCursorBlocks()
        {
            var angleFactor = Mathf.Sin(Mathf.Deg2Rad * transform.eulerAngles.x);
            var headOffset = angleFactor < 0 ? -angleFactor * .3f : 0;
            // angleFactor = angleFactor < 0 ? 0 : angleFactor;
            var lastPos = Vector3Int.FloorToInt(_transform.position);
            for (var lenght = checkIncrement; lenght < Reach; lenght += checkIncrement)
            {
                var pos = Vector3Int.FloorToInt(_transform.position + _transform.forward * (lenght));
                var blockType = _wm.GetVoxel(pos);
                if (_canDig && blockType != null && blockType.name != "air")
                {
                    if (!blockType.isSolid) return;
                    _highlightBlock.position = pos + Vector3.one * 0.5f;
                    _highlightBlock.gameObject.SetActive(true);
                    _placeBlock.gameObject.SetActive(false);
                    return;
                }

                if (_canPlace && blockType is { isSolid: true })
                {
                    var newPos = lastPos + Vector3.one * 0.5f;
                    if (_wm.GetVoxel(Vector3Int.FloorToInt(newPos))?.name != "air")
                        return;
                    var characterPos = characterController.transform.position;
                    var distanceXZ = Vector3.Distance(new Vector3(newPos.x, 0, newPos.z),
                        new Vector3(characterPos.x, 0, characterPos.z));
                    var distanceY = math.abs(newPos.y - characterPos.y);
                    var intersect = distanceXZ < characterController.radius + 0.25f &&
                                    distanceY < characterController.height - 0.2f;
                    _placeBlock.position = newPos;

                    // Check enemy intersection
                    if (Physics.CheckSphere(newPos, 0.5f, 1 << LayerMask.NameToLayer("Enemy")))
                    {
                        _placeBlock.gameObject.SetActive(false);
                        _highlightBlock.gameObject.SetActive(false);
                        return;
                    }

                    _placeBlock.gameObject.SetActive(!intersect);
                    _highlightBlock.gameObject.SetActive(false);
                    return;
                }

                lastPos = pos;
            }

            _highlightBlock.gameObject.SetActive(false);
            _placeBlock.gameObject.SetActive(false);
        }
    }
}