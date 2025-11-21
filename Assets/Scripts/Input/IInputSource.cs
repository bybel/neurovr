using UnityEngine;

namespace NeuroReachVR.Input
{
    /// <summary>
    /// Unified interface for all input sources (hands, stylus, controllers)
    /// </summary>
    public interface IInputSource
    {
        bool IsAvailable { get; }
        bool IsTracking { get; }
        Vector3 Position { get; }
        Quaternion Rotation { get; }
        float Confidence { get; }
    }
}

