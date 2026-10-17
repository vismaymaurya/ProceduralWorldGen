using UnityEngine;

namespace ProceduralTerrain
{
    public class DioramaCameraController : MonoBehaviour
    {
        [Header("Target & Framing")]
        public Vector3 focalPoint = Vector3.zero;
        public float distance = 65f;
        public float minDistance = 15f;
        public float maxDistance = 140f;

        [Header("Angles")]
        public float pitch = 45f;
        public float yaw = 45f;
        public float minPitch = 20f;
        public float maxPitch = 80f;

        [Header("Speeds & Smoothing")]
        public float orbitSpeed = 4.0f;
        public float zoomSpeed = 8.0f;
        public float panSpeed = 25.0f;
        public float smoothTime = 0.12f;

        private Vector3 currentVelocity;
        private Vector3 targetPosition;
        private float currentDistance;
        private float currentPitch;
        private float currentYaw;

        private Vector3 lastMousePos;

        private void Start()
        {
            currentDistance = distance;
            currentPitch = pitch;
            currentYaw = yaw;
            UpdateCameraTransformImmediate();
        }

        private void LateUpdate()
        {
            HandleInput();
            UpdateCameraTransform();
        }

        private void HandleInput()
        {
            // Zoom via Mouse Scroll
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                distance = Mathf.Clamp(distance - scroll * zoomSpeed * 10f, minDistance, maxDistance);
            }

            // Orbit via Right Mouse Button or Alt + Left Click
            if (Input.GetMouseButtonDown(1) || (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButtonDown(0)))
            {
                lastMousePos = Input.mousePosition;
            }
            else if (Input.GetMouseButton(1) || (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(0)))
            {
                Vector3 delta = Input.mousePosition - lastMousePos;
                yaw += delta.x * orbitSpeed * 0.1f;
                pitch = Mathf.Clamp(pitch - delta.y * orbitSpeed * 0.1f, minPitch, maxPitch);
                lastMousePos = Input.mousePosition;
            }

            // Pan via Middle Mouse Button or Keyboard WASD
            if (Input.GetMouseButtonDown(2))
            {
                lastMousePos = Input.mousePosition;
            }
            else if (Input.GetMouseButton(2))
            {
                Vector3 delta = Input.mousePosition - lastMousePos;
                Vector3 right = transform.right;
                Vector3 forward = Vector3.Cross(right, Vector3.up).normalized;

                focalPoint -= (right * delta.x + forward * delta.y) * (distance * 0.002f);
                lastMousePos = Input.mousePosition;
            }

            // Keyboard Pan
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
            {
                Vector3 right = transform.right;
                Vector3 forward = Vector3.Cross(right, Vector3.up).normalized;
                focalPoint += (right * h + forward * v) * (panSpeed * Time.deltaTime);
            }
        }

        private void UpdateCameraTransform()
        {
            currentDistance = Mathf.Lerp(currentDistance, distance, Time.deltaTime / smoothTime);
            currentPitch = Mathf.Lerp(currentPitch, pitch, Time.deltaTime / smoothTime);
            currentYaw = Mathf.Lerp(currentYaw, yaw, Time.deltaTime / smoothTime);

            Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);
            Vector3 offset = rotation * new Vector3(0, 0, -currentDistance);

            transform.position = focalPoint + offset;
            transform.rotation = rotation;
        }

        public void UpdateCameraTransformImmediate()
        {
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
            Vector3 offset = rotation * new Vector3(0, 0, -distance);
            transform.position = focalPoint + offset;
            transform.rotation = rotation;
        }

        public void FocusOn(Vector3 point)
        {
            focalPoint = point;
        }
    }
}
