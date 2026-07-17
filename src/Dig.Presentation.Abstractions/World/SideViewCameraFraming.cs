using System;

namespace Dig.Presentation.World
{
    public static class SideViewCameraFraming
    {
        public static float CalculateDistance(
            float width,
            float height,
            float aspect,
            float verticalFieldOfViewDegrees,
            float padding)
        {
            if (width <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            if (aspect <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(aspect));
            }

            if (verticalFieldOfViewDegrees <= 1f || verticalFieldOfViewDegrees >= 179f)
            {
                throw new ArgumentOutOfRangeException(nameof(verticalFieldOfViewDegrees));
            }

            if (padding < 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(padding));
            }

            double halfVerticalRadians = verticalFieldOfViewDegrees * Math.PI / 360d;
            double verticalTangent = Math.Tan(halfVerticalRadians);
            double horizontalTangent = verticalTangent * aspect;
            double verticalDistance = (height * 0.5d) / verticalTangent;
            double horizontalDistance = (width * 0.5d) / horizontalTangent;
            return (float)(Math.Max(verticalDistance, horizontalDistance) * padding);
        }
    }
}
