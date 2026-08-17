using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Turn360To2D
{
    /// <summary>First-person mouse look for inspecting the six projected screens at runtime.</summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public sealed class PanoramaMouseLook : MonoBehaviour
    {
        [Tooltip("Degrees rotated per mouse-input unit.")]
        [SerializeField] private float sensitivity = 2.5f;
        [Tooltip("Locks the cursor immediately when Play mode starts.")]
        [SerializeField] private bool lockCursorOnStart = true;
        private float yaw;
        private float pitch;

        /// <summary>
        /// Compatibility entry point for older scene setup code. The active
        /// SixFaceProjectionOutput owns the calibrated position from now on.
        /// </summary>
        public void ConfigureIdealViewingPoint(float viewingHeight)
        {
            // Only use this when no projector exists, so legacy scene creation
            // can still place a newly created camera once without fighting the
            // projector's editable eye-height and eye-to-front settings.
            if (FindObjectOfType<SixFaceProjectionOutput>() == null)
                transform.position = new Vector3(0f, Mathf.Max(0f, viewingHeight), 0f);
        }

        private void Start()
        {
            Vector3 euler = transform.eulerAngles;
            yaw = euler.y;
            pitch = NormalizeSignedAngle(euler.x);
            if (lockCursorOnStart) SetCursorLocked(true);
        }

        private void Update()
        {
            if (!Application.isPlaying) return;
            if (Input.GetKeyDown(KeyCode.Escape)) SetCursorLocked(false);
            if (Cursor.lockState != CursorLockMode.Locked && Input.GetMouseButtonDown(0)) SetCursorLocked(true);
            if (Cursor.lockState != CursorLockMode.Locked) return;

            yaw += Input.GetAxisRaw("Mouse X") * sensitivity;
            pitch = Mathf.Clamp(pitch - Input.GetAxisRaw("Mouse Y") * sensitivity, -89.9f, 89.9f);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        private void OnDrawGizmos()
        {
            SixFaceProjectionOutput projector = FindObjectOfType<SixFaceProjectionOutput>();
            if (projector == null) return;

            Transform front = FindFace(projector.transform, "Front");
            if (front == null) return;

            // The visible inside surface is the front wall centre minus half its physical thickness.
            Vector3 pointOnFrontPlane = front.position - front.forward * (front.lossyScale.z * 0.5f);
            Vector3 normal = front.forward;
            float distance = Vector3.Dot(pointOnFrontPlane - transform.position, normal);
            Vector3 endpoint = transform.position + normal * distance;

            Gizmos.color = new Color(1f, 0.78f, 0f, 1f);
            Gizmos.DrawLine(transform.position, endpoint);
            Gizmos.DrawSphere(transform.position, 0.07f);
            Gizmos.DrawSphere(endpoint, 0.07f);
            #if UNITY_EDITOR
            Handles.color = Gizmos.color;
            Handles.Label((transform.position + endpoint) * 0.5f + Vector3.up * 0.18f,
                $"Eye → Front inner surface = {Mathf.Abs(distance):0.000} m");
            #endif
        }

        private static Transform FindFace(Transform cube, string directionName)
        {
            foreach (Transform child in cube)
            {
                if (child.name.StartsWith(directionName)) return child;
            }

            return null;
        }

        private void OnDisable()
        {
            if (Cursor.lockState == CursorLockMode.Locked) SetCursorLocked(false);
        }

        private static void SetCursorLocked(bool isLocked)
        {
            Cursor.lockState = isLocked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !isLocked;
        }

        private static float NormalizeSignedAngle(float angle)
        {
            return angle > 180f ? angle - 360f : angle;
        }
    }
}
