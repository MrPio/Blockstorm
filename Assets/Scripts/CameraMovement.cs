using System.Collections.Generic;
using System.Linq;
using Managers;
using Model;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using VoxelEngine;

public class CameraMovement : MonoBehaviour
{
    public const float FOV = 68;
    [SerializeField] private Transform highlightBlock, placeBlock;
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
    [SerializeField] private ParticleSystem blockDigEffect;

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
            highlightBlock.gameObject.SetActive(false);
            placeBlock.gameObject.SetActive(false);
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
            highlightBlock.gameObject.SetActive(false);
            placeBlock.gameObject.SetActive(false);
        }
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
            if (highlightBlock.gameObject.activeSelf)
            {
                var highlightBlockPos = highlightBlock.transform.position;
                var pos = Vector3Int.FloorToInt(highlightBlockPos);
                var block = _wm.map.GetBlock(pos);
                blockDigEffect.transform.position = highlightBlockPos;
                blockDigEffect.GetComponent<Renderer>().material =
                    Resources.Load<Material>(
                        $"Textures/texturepacks/blockade/Materials/blockade_{(block.topID + 1):D1}");
                blockDigEffect.Play();
                if (new List<string>() { "crate", "crate", "window", "hay", "barrel", "log" }.Any(it =>
                        block.name.Contains(it)))
                    audioSource.PlayOneShot(blockDamageLightClip);
                else if (block.blockHealth == BlockHealth.Indestructible)
                    audioSource.PlayOneShot(noBlockDamageClip);
                else
                    audioSource.PlayOneShot(blockDamageMediumClip);
                if (_wm.DamageBlock(highlightBlock.transform.position, InventoryManager.Instance.melee!.damage))
                    highlightBlock.gameObject.SetActive(false);
                
            }
            weaponManager.Fire();
        }

        // If I am placing with an equipped block
        else if (weaponManager.WeaponModel is { type: WeaponType.Block } && placeBlock.gameObject.activeSelf &&
                 Input.GetMouseButton(0) &&
                 Time.time - _lastPlace > weaponManager.WeaponModel.Delay)
        {
            _lastPlace = Time.time;
            _wm.EditVoxel(placeBlock.transform.position, InventoryManager.Instance.blockId);
            placeBlock.gameObject.SetActive(false);
            weaponManager.Fire();
        }

        // If I am firing with a weapon
        else if (weaponManager.WeaponModel is { type: WeaponType.Primary } or { type: WeaponType.Secondary } or
                     { type: WeaponType.Tertiary } && Input.GetMouseButton(0) &&
                 Time.time - _lastFire > weaponManager.WeaponModel.Delay)
        {
            print("ok");
            _lastFire = Time.time;
            weaponManager.Fire();
        }

        if (weaponManager.WeaponModel is { type: WeaponType.Block } && _canPlace && Input.GetMouseButtonUp(0))
            _lastPlace -= weaponManager.WeaponModel.Delay * 0.65f;

        if (blockDigEffect.isPlaying && Input.GetMouseButtonUp(0))
            blockDigEffect.Stop();
    }

    // We use voxels, so we don't have a collider in every cube.
    // The strategy used here is to beam a ray of incremental length until a solid
    // voxel location is reached or the iteration ends.
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
                highlightBlock.position = pos + Vector3.one * 0.5f;
                highlightBlock.gameObject.SetActive(true);
                placeBlock.gameObject.SetActive(false);
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
                placeBlock.position = newPos;
                placeBlock.gameObject.SetActive(!intersect);
                highlightBlock.gameObject.SetActive(false);
                return;
            }

            lastPos = pos;
        }

        highlightBlock.gameObject.SetActive(false);
        placeBlock.gameObject.SetActive(false);
    }


}