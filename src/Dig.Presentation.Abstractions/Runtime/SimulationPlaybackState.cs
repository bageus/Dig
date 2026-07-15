using System;

namespace Dig.Presentation.Runtime
{

public enum SimulationPlaybackSpeed
{
    Normal = 1,
    Fast = 2,
    VeryFast = 4,
}

public sealed class SimulationPlaybackState
{
    private double _elapsedSeconds;
    private bool _paused;
    private bool _singleStepRequested;
    private SimulationPlaybackSpeed _speed = SimulationPlaybackSpeed.Normal;

    public bool IsPaused => _paused;

    public SimulationPlaybackSpeed Speed => _speed;

    public int SpeedMultiplier => (int)_speed;

    public string Label => _paused ? "Paused" : $"{SpeedMultiplier}x";

    public void TogglePause()
    {
        _paused = !_paused;
        _singleStepRequested = false;
    }

    public void StepOnce()
    {
        _paused = true;
        _singleStepRequested = true;
    }

    public void SetSpeed(SimulationPlaybackSpeed speed)
    {
        if (!Enum.IsDefined(typeof(SimulationPlaybackSpeed), speed))
        {
            throw new ArgumentOutOfRangeException(nameof(speed));
        }

        _speed = speed;
        _paused = false;
        _singleStepRequested = false;
    }

    public void SlowDown()
    {
        if (_paused)
        {
            return;
        }

        switch (_speed)
        {
            case SimulationPlaybackSpeed.VeryFast:
                SetSpeed(SimulationPlaybackSpeed.Fast);
                break;
            case SimulationPlaybackSpeed.Fast:
                SetSpeed(SimulationPlaybackSpeed.Normal);
                break;
            default:
                TogglePause();
                break;
        }
    }

    public void SpeedUp()
    {
        if (_paused)
        {
            SetSpeed(SimulationPlaybackSpeed.Normal);
            return;
        }

        switch (_speed)
        {
            case SimulationPlaybackSpeed.Normal:
                SetSpeed(SimulationPlaybackSpeed.Fast);
                break;
            case SimulationPlaybackSpeed.Fast:
                SetSpeed(SimulationPlaybackSpeed.VeryFast);
                break;
        }
    }

    public int ConsumeDueTicks(
        double unscaledDeltaSeconds,
        double baseTickIntervalSeconds,
        int maximumTicksPerFrame = 8)
    {
        if (unscaledDeltaSeconds < 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(unscaledDeltaSeconds));
        }

        if (baseTickIntervalSeconds <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(baseTickIntervalSeconds));
        }

        if (maximumTicksPerFrame <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumTicksPerFrame));
        }

        if (_paused)
        {
            if (!_singleStepRequested)
            {
                return 0;
            }

            _singleStepRequested = false;
            return 1;
        }

        double maximumAccumulated = baseTickIntervalSeconds * maximumTicksPerFrame;
        _elapsedSeconds = Math.Min(
            maximumAccumulated,
            _elapsedSeconds + (unscaledDeltaSeconds * SpeedMultiplier));
        int dueTicks = Math.Min(
            maximumTicksPerFrame,
            (int)(_elapsedSeconds / baseTickIntervalSeconds));
        if (dueTicks > 0)
        {
            _elapsedSeconds -= dueTicks * baseTickIntervalSeconds;
        }

        return dueTicks;
    }
}
}
