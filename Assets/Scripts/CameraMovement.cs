using UnityEngine;
using VoxelEngine;

public class CameraMovement : MonoBehaviour
{
    public float sensitivity;
    public Transform player;
    private float _rotX, _rotY;
    [SerializeField] private Transform highlightBlock, placeBlock;
    public float checkIncrement = 0.1f;
    public float reach = 8;
    private WorldManager _wm;
    private Transform _transform;
    private Vector3Int _lastSeenBlock;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        _wm = WorldManager.instance;
        _transform = transform;
    }

    private void Update()
    {
        var mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensitivity;
        var mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensitivity;
        _rotX -= mouseY;
        _rotX = Mathf.Clamp(_rotX, -90f, 90f);
        _transform.localRotation = Quaternion.Euler(_rotX, 0f, 0f);
        player.Rotate(Vector3.up * mouseX);
        PlaceCursorBlocks();
    }

    // We use voxels, so we don't have a collider in every cube.
    // The strategy used here is to beam a ray of incremental length until a solid
    // voxel location is reached or the iteration ends.
    private void PlaceCursorBlocks()
    {
        for (var lenght = checkIncrement; lenght < reach; lenght += checkIncrement)
        {
            var pos = Vector3Int.FloorToInt(_transform.position + _transform.forward * lenght);
            var blockType = _wm.GetVoxel(pos);
            if (blockType is { isSolid: true })
            {
                _lastSeenBlock = pos;
                highlightBlock.position = pos + Vector3.one * 0.5f;
                highlightBlock.gameObject.SetActive(true);
                return;
            }
        }

        highlightBlock.gameObject.SetActive(false);
    }
}