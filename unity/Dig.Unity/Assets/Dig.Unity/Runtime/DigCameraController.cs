using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigCameraController : MonoBehaviour
    {
        private const float SideViewYaw = 180f;

        [SerializeField]
        private float panSpeed = 8f;

        [SerializeField]
        private float zoomSpeed = 3f;

        [SerializeField]
        private float minimumDistance = 6f;

        [SerializeField]
        private float maximumDistance = 80f;

        [SerializeField]
        private float verticalFieldOfView = 42f;

        [SerializeField]
        private float framingPadding = 1.18f;

        [SerializeField]
        private float orbitDegreesPerPixel = 0.18f;

        [SerializeField]
        private float minimumYaw = -32f;

        [SerializeField]
        private float maximumYaw = 32f;

        [SerializeField]
        private float minimumPitch = -22f;

        [SerializeField]
        private float maximumPitch = 28f;

        private Camera? _camera;
        private SideViewCameraOrbitState? _orbit;
        private Vector3 _focus;
        private Vector3 _previousMousePosition;
        private float _distance = 25f;
        private bool _orbitInputActive;

        public void Initialize(Camera targetCamera, WorldViewModel world)
        {
            _camera = targetCamera;
            _camera.orthographic = false;
            _camera.fieldOfView = verticalFieldOfView;
            _orbit = CreateOrbit();
            Frame(world);
            _previousMousePosition = Input.mousePosition;
        }

        public void Frame(WorldViewModel world)
        {
            _focus = DigSideViewProjection.WorldCenter(world.Width, world.Height);
            EnsureOrbit().Reset();
            if (_camera != null)
            {
                _camera.orthographic = false;
                _camera.fieldOfView = verticalFieldOfView;
                _distance = Mathf.Clamp(
                    SideViewCameraFraming.CalculateDistance(
                        world.Width,
                        world.Height,
                        Mathf.Max(0.1f, _camera.aspect),
                        verticalFieldOfView,
                        framingPadding),
                    minimumDistance,
                    maximumDistance);
            }

            ApplyPose();
        }

        internal void Focus(Vector3 worldPosition)
        {
            _focus = new Vector3(worldPosition.x, worldPosition.y, 0f);
            ApplyPose();
        }

        private void Update()
        {
            if (_camera == null)
            {
                return;
            }

            Vector3 mousePosition = Input.mousePosition;
            HandleOrbit(mousePosition);
            HandleReset();
            HandleZoom();
            HandlePan();
            _previousMousePosition = mousePosition;
            ApplyPose();
        }

        private void HandleOrbit(Vector3 mousePosition)
        {
            bool controlPressed = IsControlPressed();
            if (controlPressed && _orbitInputActive)
            {
                Vector3 delta = mousePosition - _previousMousePosition;
                EnsureOrbit().Rotate(
                    delta.x,
                    delta.y,
                    orbitDegreesPerPixel);
            }

            _orbitInputActive = controlPressed;
        }

        private void HandleReset()
        {
            if (Input.GetKeyDown(KeyCode.Home))
            {
                EnsureOrbit().Reset();
            }
        }

        private void HandleZoom()
        {
            float wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) > 0.001f)
            {
                _distance = Mathf.Clamp(
                    _distance - (wheel * zoomSpeed),
                    minimumDistance,
                    maximumDistance);
            }
        }

        private void HandlePan()
        {
            float horizontal = ReadAxis(
                KeyCode.A,
                KeyCode.LeftArrow,
                KeyCode.D,
                KeyCode.RightArrow);
            float vertical = ReadAxis(
                KeyCode.S,
                KeyCode.DownArrow,
                KeyCode.W,
                KeyCode.UpArrow);
            Vector3 movement = new Vector3(horizontal, vertical, 0f);
            if (movement.sqrMagnitude > 1f)
            {
                movement.Normalize();
            }

            float distanceScale = Mathf.Max(0.4f, _distance / 25f);
            _focus += movement
                * (panSpeed * distanceScale * Time.unscaledDeltaTime);
        }

        private SideViewCameraOrbitState EnsureOrbit()
        {
            _orbit ??= CreateOrbit();
            return _orbit;
        }

        private SideViewCameraOrbitState CreateOrbit()
        {
            return new SideViewCameraOrbitState(
                minimumYaw,
                maximumYaw,
                minimumPitch,
                maximumPitch);
        }

        private static bool IsControlPressed()
        {
            return Input.GetKey(KeyCode.LeftControl)
                || Input.GetKey(KeyCode.RightControl);
        }

        private static float ReadAxis(
            KeyCode negativePrimary,
            KeyCode negativeSecondary,
            KeyCode positivePrimary,
            KeyCode positiveSecondary)
        {
            bool negative = Input.GetKey(negativePrimary)
                || Input.GetKey(negativeSecondary);
            bool positive = Input.GetKey(positivePrimary)
                || Input.GetKey(positiveSecondary);
            return (positive ? 1f : 0f) - (negative ? 1f : 0f);
        }

        private void ApplyPose()
        {
            if (_camera == null)
            {
                return;
            }

            SideViewCameraOrbitState orbit = EnsureOrbit();
            Quaternion rotation = Quaternion.Euler(
                orbit.Pitch,
                SideViewYaw + orbit.Yaw,
                0f);
            _camera.transform.rotation = rotation;
            _camera.transform.position = _focus
                - (rotation * Vector3.forward * _distance);
        }

        private void OnValidate()
        {
            panSpeed = Mathf.Max(0.1f, panSpeed);
            zoomSpeed = Mathf.Max(0.1f, zoomSpeed);
            minimumDistance = Mathf.Max(1f, minimumDistance);
            maximumDistance = Mathf.Max(minimumDistance + 1f, maximumDistance);
            verticalFieldOfView = Mathf.Clamp(verticalFieldOfView, 20f, 80f);
            framingPadding = Mathf.Max(1f, framingPadding);
            orbitDegreesPerPixel = Mathf.Max(0.01f, orbitDegreesPerPixel);
            minimumYaw = Mathf.Min(minimumYaw, maximumYaw - 1f);
            maximumYaw = Mathf.Max(maximumYaw, minimumYaw + 1f);
            minimumPitch = Mathf.Min(minimumPitch, maximumPitch - 1f);
            maximumPitch = Mathf.Max(maximumPitch, minimumPitch + 1f);
        }
    }
}
