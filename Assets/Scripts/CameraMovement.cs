using System.Collections.Generic;
using System.Linq;
using Managers;
using Model;
using Network;
using Unity.Mathematics;
using UnityEngine;
using VoxelEngine;

public class CameraMovement : MonoBehaviour
{
    public const float FOVMain = 68, FOVWeapon=44;
    private Transform _highlightBlock, _placeBlock;
    [SerializeField] private CharacterController characterController;
    public float sensitivity;
    public Transform player;
    private float _rotX, _rotY;
    public float checkIncrement = 0.1f;
    private float Reach => weaponManager.WeaponModel?.distance ?? 0f;
    private WorldManager _wm;
    private Transform _transform;
    private bool _canDig, _canPlace;
    private float _lastDig, _lastPlace, _lastFire;
    public WeaponManager weaponManager;
    private ParticleSystem _blockDigEffect;

    [SerializeField] private AudioClip blockDamageLightClip, blockDamageMediumClip, noBlockDamageClip;

    [SerializeField] private AudioSource audioSource;
    public bool CanDig
    {
        get => _canDig;
        set
        {
            _canDig = value;
            if (value)
                _canPlace = false;
            _highlightBlock.gameObject.SetActive(false);
            _placeBlock.gameObject.SetActive(false);
        }
    }

    public bool CanPlace
    {
        get => _canPlace;
        set
        {
            _canPlace = InventoryManager.Instance.blocks > 0 && value;
            if (value)
                _canDig = false;
            _highlightBlock.gameObject.SetActive(false);
            _placeBlock.gameObject.SetActive(false);
        }
    }

    private void Awake()
    {
        _highlightBlock = GameObject.FindWithTag("HighlightBlock").transform;
        _highlightBlock.gameObject.SetActive(false);
        _placeBlock = GameObject.FindWithTag("PlaceBlock").transform;
        _placeBlock.gameObject.SetActive(false);
        _blockDigEffect = GameObject.FindWithTag("BlockDigEffect").GetComponent<ParticleSystem>();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        _wm = WorldManager.instance;
        _transform = transform;
    }

    private void LateUpdate()
    {
        // Handle camera rotation
        var mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensitivity *(WeaponManager.isAiming?0.66f:1f);
        var mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensitivity *(WeaponManager.isAiming?0.66f:1f);
        _rotX -= mouseY;
        _rotX = Mathf.Clamp(_rotX, -90f, 90f);
        _transform.localRotation = Quaternion.Euler(_rotX, 0f, 0f);
        player.Rotate(Vector3.up * mouseX);

        // Handle green&red block indicators
        if (_canDig || _canPlace)
            PlaceCursorBlocks();

        // If I am digging with a melee weapon
        if (weaponManager.WeaponModel is { type: WeaponType.Melee } &&
            Input.GetMouseButton(0) && Time.time - _lastDig > weaponManager.WeaponModel.Delay)
        {
            _lastDig = Time.time;
            if (_highlightBlock.gameObject.activeSelf)
            {
                var highlightBlockPos = _highlightBlock.transform.position;
                var pos = Vector3Int.FloorToInt(highlightBlockPos);
                var block = _wm.map.GetBlock(pos);
                _blockDigEffect.transform.position = highlightBlockPos;
                _blockDigEffect.GetComponent<Renderer>().material =
                    Resources.Load<Material>(
                        $"Textures/texturepacks/blockade/Materials/blockade_{(block.topID + 1):D1}");
                _blockDigEffect.Play();
                if (new List<string>() { "crate", "crate", "window", "hay", "barrel", "log" }.Any(it =>
                        block.name.Contains(it)))
                    audioSource.PlayOneShot(blockDamageLightClip);
                else if (block.blockHealth == BlockHealth.Indestructible)
                    audioSource.PlayOneShot(noBlockDamageClip);
                else
                    audioSource.PlayOneShot(blockDamageMediumClip);
                ServerManager.instance.DamageVoxelServerRpc(_highlightBlock.transform.position, InventoryManager.Instance.melee!.damage);
                
            }
            weaponManager.Fire();
        }

        // If I am placing with an equipped block
        else if (weaponManager.WeaponModel is { type: WeaponType.Block } && _placeBlock.gameObject.activeSelf &&
                 Input.GetMouseButton(0) &&
                 Time.time - _lastPlace > weaponManager.WeaponModel.Delay)
        {
            _lastPlace = Time.time;
            ServerManager.instance.EditVoxelServerRpc(_placeBlock.transform.position, InventoryManager.Instance.blockId);
            // _wm.EditVoxel(_placeBlock.transform.position, InventoryManager.Instance.blockId);
            _placeBlock.gameObject.SetActive(false);
            weaponManager.Fire();
        }

        // If I am firing with a weapon
        else if (weaponManager.WeaponModel is { type: WeaponType.Primary } or { type: WeaponType.Secondary } or
                     { type: WeaponType.Tertiary } && Input.GetMouseButton(0) &&
                 Time.time - _lastFire > weaponManager.WeaponModel.Delay)
        {
            _lastFire = Time.time;
            weaponManager.Fire();
        }

        if (weaponManager.WeaponModel is { type: WeaponType.Block } && _canPlace && Input.GetMouseButtonUp(0))
            _lastPlace -= weaponManager.WeaponModel.Delay * 0.65f;

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
        angleFactor = angleFactor < 0 ? 0 : angleFactor;
        var minDistance = _canPlace ? 1.04f + 1f * Mathf.Pow(angleFactor, 4.85f) - headOffset : 0;
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