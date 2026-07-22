using System;

namespace Dig.Domain.World
{

public static class WorldDepth
{
    public const int Minimum = 0;

    public const int Maximum = 3;

    public const int LayerCount = Maximum - Minimum + 1;

    public static bool Contains(int z)
    {
        return z >= Minimum && z <= Maximum;
    }

    public static void EnsureValid(int z, string parameterName)
    {
        if (!Contains(z))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                z,
                $"World depth must be between {Minimum} and {Maximum} inclusive.");
        }
    }
}

}
