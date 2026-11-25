using Unity.VisualScripting;
using UnityEngine;

public class ClimbableLedge : MonoBehaviour
{

    [SerializeField] private Collider _climbableCollider;
    private const float _grabPointDepth = 0.1f;
    private const float _maxRayLength = 0.3f;
    void Reset()
    {
        _climbableCollider = GetComponent<Collider>();
    }
    public bool TryGetLedgePoints(RaycastHit wallHit, out Vector3 grabPoint)
    {
        grabPoint = default;

        Vector3 topOrigin = wallHit.point - (wallHit.normal * _grabPointDepth);
        topOrigin.y = _climbableCollider.bounds.max.y + _grabPointDepth;

        if (!_climbableCollider.Raycast(new Ray(topOrigin, Vector3.down), out RaycastHit topHit, _maxRayLength))
        {
            return false;
        }
        grabPoint = new Vector3(wallHit.point.x, topHit.point.y, wallHit.point.z);

        return true;
    }
}
