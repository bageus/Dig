using System;

namespace Dig.Presentation.World
{
    public sealed class SideViewCameraOrbitState
    {
        public SideViewCameraOrbitState(
            float minimumYaw = -32f,
            float maximumYaw = 32f,
            float minimumPitch = -22f,
            float maximumPitch = 28f)
        {
            if (minimumYaw >= maximumYaw)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumYaw));
            }

            if (minimumPitch >= maximumPitch)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumPitch));
            }

            MinimumYaw = minimumYaw;
            MaximumYaw = maximumYaw;
            MinimumPitch = minimumPitch;
            MaximumPitch = maximumPitch;
        }

        public float MinimumYaw { get; }

        public float MaximumYaw { get; }

        public float MinimumPitch { get; }

        public float MaximumPitch { get; }

        public float Yaw { get; private set; }

        public float Pitch { get; private set; }

        public void Rotate(
            float horizontalDelta,
            float verticalDelta,
            float degreesPerUnit)
        {
            if (degreesPerUnit < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(degreesPerUnit));
            }

            Yaw = Clamp(
                Yaw + (horizontalDelta * degreesPerUnit),
                MinimumYaw,
                MaximumYaw);
            Pitch = Clamp(
                Pitch + (verticalDelta * degreesPerUnit),
                MinimumPitch,
                MaximumPitch);
        }

        public void Reset()
        {
            Yaw = 0f;
            Pitch = 0f;
        }

        private static float Clamp(float value, float minimum, float maximum)
        {
            if (value < minimum)
            {
                return minimum;
            }

            return value > maximum ? maximum : value;
        }
    }
}
