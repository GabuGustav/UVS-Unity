using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

[MovedFrom(true, null, null, "WheelVisualizerSync")]
public class WheelVisualSync : MonoBehaviour
{
    [System.Serializable]
    public class WheelPair
    {
        public WheelCollider collider;
        public Transform visualWheel;
    }

    public WheelPair[] wheels;

    // Add small smoothing to reduce jitter from impulse forces
    [Header("Smoothing (helps with hydraulics)")]
    public float positionLerpSpeed = 20f;
    public float rotationLerpSpeed = 20f;

    private void LateUpdate() // Use LateUpdate to run AFTER physics/FixedUpdate
    {
        SyncNow();
    }

    public void SyncNow()
    {
        foreach (var pair in wheels)
        {
            if (pair.collider == null || pair.visualWheel == null) continue;

            Vector3 targetPos;
            Quaternion targetRot;
            pair.collider.GetWorldPose(out targetPos, out targetRot);

            // Smooth position & rotation to avoid jitter from sudden impulses
            pair.visualWheel.position = Vector3.Lerp(pair.visualWheel.position, targetPos, Time.deltaTime * positionLerpSpeed);
            pair.visualWheel.rotation = Quaternion.Slerp(pair.visualWheel.rotation, targetRot, Time.deltaTime * rotationLerpSpeed);
        }
    }
}
