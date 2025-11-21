using UnityEngine;
using NeuroReachVR.Tasks;

namespace NeuroReachVR.Core
{
    /// <summary>
    /// Difficulty profile containing all adjustable parameters
    /// Used by AdaptiveDifficultyController to apply difficulty changes
    /// </summary>
    [System.Serializable]
    public class DifficultyProfile
    {
        [Header("Balloon Pop Parameters")]
        public float spawnDistance = 1.5f;
        public float spawnRate = 1f;
        public float heightRange = 1.5f;
        public int maxBalloons = 5;
        
        [Header("Path Tracing Parameters")]
        public float pathWidth = 0.1f;
        public float requiredAccuracy = 0.7f;
        public int pathComplexity = 50; // segments
        public PathType pathType = PathType.Line;
        
        [Header("Spiral Parameters")]
        public float spiralTightness = 1f;
        public float minAngularVelocity = 0.5f;
        public float maxAngularVelocity = 2f;
        
        public DifficultyProfile Clone()
        {
            return new DifficultyProfile
            {
                spawnDistance = this.spawnDistance,
                spawnRate = this.spawnRate,
                heightRange = this.heightRange,
                maxBalloons = this.maxBalloons,
                pathWidth = this.pathWidth,
                requiredAccuracy = this.requiredAccuracy,
                pathComplexity = this.pathComplexity,
                pathType = this.pathType,
                spiralTightness = this.spiralTightness,
                minAngularVelocity = this.minAngularVelocity,
                maxAngularVelocity = this.maxAngularVelocity
            };
        }
    }
    
    public enum DifficultyLevel
    {
        Easy,
        Medium,
        Hard
    }
}

