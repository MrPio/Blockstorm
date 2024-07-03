using Unity.Mathematics;
using UnityEngine;
using VoxelEngine;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] private Transform highlightBlock, placeBlock;
    [SerializeField] private CharacterController characterController;
    public float sensitivity;
    public Transform player;
    private float _rotX, _rotY;
    public float checkIncrement = 0.1f;
    public float reach = 8;
    private WorldManager _wm;
    private Transform _transform;
    private bool _canDig, _canPlace = true;

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
            _canPlace = value;
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
        var mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensitivity;
        var mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensitivity;
        _rotX -= mouseY;
        _rotX = Mathf.Clamp(_rotX, -90f, 90f);
        _transform.localRotation = Quaternion.Euler(_rotX, 0f, 0f);
        player.Rotate(Vector3.up * mouseX);
        if (_canDig || _canPlace)
            PlaceCursorBlocks();
        if (highlightBlock.gameObject.activeSelf && Input.GetMouseButtonDown(0))
        {
            _wm.EditVoxel(highlightBlock.transform.position, 0);
            highlightBlock.gameObject.SetActive(false);
        }
        else if (placeBlock.gameObject.activeSelf && Input.GetMouseButtonDown(0))
        {
            _wm.EditVoxel(placeBlock.transform.position, 6);
            placeBlock.gameObject.SetActive(false);
        }
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
        for (var lenght = checkIncrement; lenght < reach; lenght += checkIncrement)
        {
            var pos = Vector3Int.FloorToInt(_transform.position + _transform.forward * (lenght));
            var blockType = _wm.GetVoxel(pos);
            if (_canDig && blockType is { isSolid: true })
            {
                highlightBlock.position = pos + Vector3.one * 0.5f;
                highlightBlock.gameObject.SetActive(true);
                placeBlock.gameObject.SetActive(false);
                return;
            }

            if (_canPlace && blockType is { isSolid: true })
            {
                var newPos = lastPos + Vector3.one * 0.5f;
                var characterPos = characterController.transform.position;
                var distanceXZ = Vector3.Distance(new Vector3(newPos.x, 0, newPos.z),
                    new Vector3(characterPos.x, 0, characterPos.z));
                var distanceY = math.abs(newPos.y - characterPos.y);
                var intersect = distanceXZ < characterController.radius+0.25f && distanceY < characterController.height-0.2f;
                placeBlock.position = newPos;
                print($"{distanceXZ}/{distanceY}");
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