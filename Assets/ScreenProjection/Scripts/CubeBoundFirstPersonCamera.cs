using UnityEngine;

namespace Turn360To2D
{
    /// <summary>
    /// First-person camera constrained to the usable inner volume of a BoxCollider.
    /// The clearance is measured from every wall in metres, even when the room
    /// Cube has non-uniform scaling.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CubeBoundFirstPersonCamera : MonoBehaviour
    {
        [Header("Room Boundary")]
        [Tooltip("BoxCollider on the Cube that represents the six-wall room.")]
        [SerializeField] private BoxCollider roomBounds;
        [Tooltip("Minimum distance from every interior wall, in metres.")]
        [Min(0f)] [SerializeField] private float wallClearance = 1f;

        [Header("Controls")]
        [Min(0f)] [SerializeField] private float moveSpeed = 3f;
        [Min(0f)] [SerializeField] private float mouseSensitivity = 2.5f;
        [SerializeField] private bool lockCursorOnStart = true;
        [SerializeField] private bool showPositionOverlay = true;

        private float yaw;
        private float pitch;

        private void Awake()
        {
            if (roomBounds == null) roomBounds = FindObjectOfType<BoxCollider>();
            ClampIntoRoom();
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
            HandleCursor();
            if (Cursor.lockState == CursorLockMode.Locked) HandleLook();
            HandleMovement();
        }

        private void HandleCursor()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) SetCursorLocked(false);
            if (Cursor.lockState != CursorLockMode.Locked && Input.GetMouseButtonDown(0)) SetCursorLocked(true);
        }

        private void HandleLook()
        {
            yaw += Input.GetAxisRaw("Mouse X") * mouseSensitivity;
            pitch = Mathf.Clamp(pitch - Input.GetAxisRaw("Mouse Y") * mouseSensitivity, -89.9f, 89.9f);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        private void HandleMovement()
        {
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
            float forwardInput = GetKeyAxis(KeyCode.W, KeyCode.S);
            float strafeInput = GetKeyAxis(KeyCode.D, KeyCode.A);
            float verticalInput = 0f;
            if (Input.GetKey(KeyCode.E)) verticalInput += 1f;
            if (Input.GetKey(KeyCode.Q)) verticalInput -= 1f;

            Vector3 move = forward * forwardInput
                + right * strafeInput
                + Vector3.up * verticalInput;
            if (move.sqrMagnitude > 1f) move.Normalize();

            transform.position = ClampToRoom(transform.position + move * moveSpeed * Time.deltaTime);
        }

        private void ClampIntoRoom() => transform.position = ClampToRoom(transform.position);

        private Vector3 ClampToRoom(Vector3 worldPosition)
        {
            if (roomBounds == null) return worldPosition;

            Transform room = roomBounds.transform;
            Vector3 local = room.InverseTransformPoint(worldPosition);
            Vector3 halfSize = roomBounds.size * 0.5f;
            Vector3 scale = room.lossyScale;
            Vector3 localClearance = new Vector3(
                wallClearance / Mathf.Max(0.0001f, Mathf.Abs(scale.x)),
                wallClearance / Mathf.Max(0.0001f, Mathf.Abs(scale.y)),
                wallClearance / Mathf.Max(0.0001f, Mathf.Abs(scale.z)));
            Vector3 minimum = roomBounds.center - halfSize + localClearance;
            Vector3 maximum = roomBounds.center + halfSize - localClearance;

            local.x = ClampAxis(local.x, minimum.x, maximum.x, roomBounds.center.x);
            local.y = ClampAxis(local.y, minimum.y, maximum.y, roomBounds.center.y);
            local.z = ClampAxis(local.z, minimum.z, maximum.z, roomBounds.center.z);
            return room.TransformPoint(local);
        }

        private static float ClampAxis(float value, float minimum, float maximum, float fallback)
        {
            return minimum > maximum ? fallback : Mathf.Clamp(value, minimum, maximum);
        }

        private void OnGUI()
        {
            if (!Application.isPlaying || !showPositionOverlay) return;
            GUI.Label(new Rect(12f, Screen.height - 58f, 420f, 46f),
                $"WASD move | Q/E down/up | Position: {transform.position:F2}");
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

        private static float NormalizeSignedAngle(float angle) => angle > 180f ? angle - 360f : angle;

        private static float GetKeyAxis(KeyCode positive, KeyCode negative)
        {
            return (Input.GetKey(positive) ? 1f : 0f) - (Input.GetKey(negative) ? 1f : 0f);
        }
    }
}
