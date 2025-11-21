using System.Collections.Generic;
using System.Linq;

namespace NeuroReachVR.Core
{
    /// <summary>
    /// Tracks performance metrics for adaptive difficulty
    /// Lightweight data structure for metric collection
    /// </summary>
    public class PerformanceMetrics
    {
        private readonly Queue<float> completionTimes = new Queue<float>();
        private readonly Queue<bool> successHistory = new Queue<bool>();
        private readonly Queue<float> accuracyHistory = new Queue<float>();
        private readonly int historySize;
        
        public PerformanceMetrics(int historySize = 10)
        {
            this.historySize = historySize;
        }
        
        public void RecordAttempt(float completionTime, bool success, float accuracy)
        {
            AddToQueue(completionTimes, completionTime);
            AddToQueue(successHistory, success);
            AddToQueue(accuracyHistory, accuracy);
        }
        
        public float SuccessRate => successHistory.Count > 0 
            ? successHistory.Count(s => s) / (float)successHistory.Count 
            : 0f;
        
        public float AverageCompletionTime => completionTimes.Count > 0 
            ? completionTimes.Average() 
            : 0f;
        
        public float AverageAccuracy => accuracyHistory.Count > 0 
            ? accuracyHistory.Average() 
            : 0f;
        
        public int AttemptCount => successHistory.Count;
        
        private void AddToQueue<T>(Queue<T> queue, T item)
        {
            queue.Enqueue(item);
            if (queue.Count > historySize)
                queue.Dequeue();
        }
        
        public void Clear()
        {
            completionTimes.Clear();
            successHistory.Clear();
            accuracyHistory.Clear();
        }
    }
}

