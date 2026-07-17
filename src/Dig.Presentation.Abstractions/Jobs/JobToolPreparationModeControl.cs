using System;
using Dig.Application.Jobs;

namespace Dig.Presentation.Jobs
{

public sealed class JobToolPreparationModeControl : IJobToolPreparationModeSource
{
    public JobToolPreparationModeControl(
        JobToolPreparationMode initialMode = JobToolPreparationMode.Automatic)
    {
        Validate(initialMode);
        Mode = initialMode;
    }

    public JobToolPreparationMode Mode { get; private set; }

    public string Label => Mode switch
    {
        JobToolPreparationMode.Automatic => "Automatic",
        JobToolPreparationMode.Suggest => "Suggest only",
        _ => throw new InvalidOperationException("Unsupported tool preparation mode."),
    };

    public bool Select(JobToolPreparationMode mode)
    {
        Validate(mode);
        if (Mode == mode)
        {
            return false;
        }

        Mode = mode;
        return true;
    }

    private static void Validate(JobToolPreparationMode mode)
    {
        if (!Enum.IsDefined(typeof(JobToolPreparationMode), mode))
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }
    }
}
}
