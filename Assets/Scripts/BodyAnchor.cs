using UnityEngine;

// Attaches to an empty GameObject to simulate a "Waist" or "Chest" tracking point.
// It follows the HMD (Head) position but stays upright, ignoring the head's up/down tilt.
public class BodyAnchor : MonoBehaviour
{
    [Tooltip("Reference to the OVRCameraRig's CenterEyeAnchor")]
    public Transform hmdTransform;

    [Tooltip("Offset from the head position (e.g., 0, -0.4, 0 for chest/belt)")]
    public Vector3 positionOffset = new Vector3(0, -0.4f, 0.15f);

    [Tooltip("Smooth speed for rotation to prevent jitter")]
    public float rotationSmoothness = 5f;

    void LateUpdate()
    {
        if (hmdTransform == null) return;

        // 1. Position: Follow head + offset
        // We apply the offset relative to the head's Y rotation so the holster stays in front of you
        Vector3 flatForward = hmdTransform.forward;
        flatForward.y = 0;
        flatForward.Normalize();

        Vector3 targetPosition = hmdTransform.position + (Vector3.up * positionOffset.y) + (flatForward * positionOffset.z) + (hmdTransform.right * positionOffset.x);
        transform.position = targetPosition;

        // 2. Rotation: Only follow the Y axis (Yaw) of the head
        Quaternion targetRotation = Quaternion.LookRotation(flatForward);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSmoothness);
    }
}