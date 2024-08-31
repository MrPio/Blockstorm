using UnityEngine;

public class HighlightArea : MonoBehaviour
{
    private const float SmallRange = 36f;
    private const float BigRange = 54f;
    private bool _isAiming;
    public float Range => _isAiming ? BigRange : SmallRange;

    public void SetRange(bool isAiming)
    {
        _isAiming = isAiming;
        var y = Range / 36f * 18f;
        transform.position = new Vector3(transform.position.x, y, transform.position.z);
    }
}