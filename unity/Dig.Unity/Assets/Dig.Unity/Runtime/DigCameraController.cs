using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigCameraController : MonoBehaviour
    {
        [SerializeField]
        private float panSpeed = 8f;

        [SerializeField]
        private float zoomSpeed = 4f;

        [SerializeField]
        private float minimumZoom = 4f;

        [SerializeField]
        private float maximumZoom = 30f;

        private Camera? _camera;
        private Vector3 _focus;
        private float _yaw;
        private const float Pitch = 55f;
        private const float Distance = 25f;

        public void Initialize(Camera targetCamera, WorldViewModel world)
        {
            _camera = targetCamera;
            _camera.orthographic = true;
            Frame(world);
        }

        public void Frame(WorldViewModel world)
        {
            _focus = new Vector3(
                (world.Width - 1) * 0.5f,
                0f,
                (world.Height - 1) * 0.5f);
            if (_camera != null)
            {
                _camera.orthographicSize = Mathf.Clamp(
                    Mathf.Max(world.Width, world.Height) * 0.58f,
                    minimumZoom,
                    maximumZoom);
            }

            ApplyPose();
        }

        private void Update()
        {
            if (_camera == null)
            {
                return;
            }

            HandleRotation();
            HandleZoom();
            HandlePan();
            ApplyPose();
        }

        private void HandleRotation()
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                _yaw -= 90f;
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                _yaw += 90f;
            }
        }

        private void HandleZoom()
        {
            float wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) > 0.001f && _camera != null)
            {
                _camera.orthographicSize = Mathf.Clamp(
                    _camera.orthographicSize - (wheel * zoomSpeed),
                    minimumZoom,
                    maximumZoom);
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
            Quaternion heading = Quaternion.Euler(0f, _yaw, 0f);
            Vector3 movement = (heading * Vector3.right * horizontal)
                + (heading * Vector3.forward * vertical);
            if (movement.sqrMagnitude > 1f)
            {
                movement.Normalize();
            }

            _focus += movement * (panSpeed * Time.unscaledDeltaTime);
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

            Quaternion rotation = Quaternion.Euler(Pitch, _yaw, 0f);
            _camera.transform.rotation = rotation;
            _camera.transform.position = _focus - (rotation * Vector3.forward * Distance);
        }
    }
}
