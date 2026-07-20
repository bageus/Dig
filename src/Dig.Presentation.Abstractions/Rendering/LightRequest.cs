using System;

namespace Dig.Presentation.Rendering
{
public sealed class LightRequest
{
    public LightRequest(string requestId, RealtimeLightKind kind,
        RealtimeLightPriority priority,
        double worldX, double worldY, double worldZ,
        double range, double intensity,
        double red, double green, double blue,
        bool castsShadows, long version)
    {
        if (string.IsNullOrWhiteSpace(requestId)) throw new ArgumentException(nameof(requestId));
        if (!Enum.IsDefined(typeof(RealtimeLightKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        if (!Enum.IsDefined(typeof(RealtimeLightPriority), priority)) throw new ArgumentOutOfRangeException(nameof(priority));
        if (range <= 0d || range > 64d) throw new ArgumentOutOfRangeException(nameof(range));
        if (intensity <= 0d || intensity > 16d) throw new ArgumentOutOfRangeException(nameof(intensity));
        ValidateColor(red, nameof(red));
        ValidateColor(green, nameof(green));
        ValidateColor(blue, nameof(blue));
        if (version < 0) throw new ArgumentOutOfRangeException(nameof(version));
        RequestId = requestId.Trim();
        Kind = kind;
        Priority = priority;
        WorldX = worldX;
        WorldY = worldY;
        WorldZ = worldZ;
        Range = range;
        Intensity = intensity;
        Red = red;
        Green = green;
        Blue = blue;
        CastsShadows = castsShadows;
        Version = version;
    }

    public string RequestId { get; }
    public RealtimeLightKind Kind { get; }
    public RealtimeLightPriority Priority { get; }
    public double WorldX { get; }
    public double WorldY { get; }
    public double WorldZ { get; }
    public double Range { get; }
    public double Intensity { get; }
    public double Red { get; }
    public double Green { get; }
    public double Blue { get; }
    public bool CastsShadows { get; }
    public long Version { get; }

    private static void ValidateColor(double value, string name)
    {
        if (value < 0d || value > 1d) throw new ArgumentOutOfRangeException(name);
    }
}
}
