namespace NeuroReachVR.Core
{
    /// <summary>
    /// Interface for tasks to report performance metrics
    /// Enables adaptive difficulty system integration
    /// </summary>
    public interface ITaskPerformanceReporter
    {
        void ReportAttempt(float completionTime, bool success, float accuracy);
    }
}

