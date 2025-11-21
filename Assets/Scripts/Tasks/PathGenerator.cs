using UnityEngine;
using System.Collections.Generic;

namespace NeuroReachVR.Tasks
{
    /// <summary>
    /// Generates traceable paths (lines, curves, shapes)
    /// </summary>
    public static class PathGenerator
    {
        public static List<Vector3> GenerateLine(Vector3 start, Vector3 end, int segments)
        {
            var path = new List<Vector3>();
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                path.Add(Vector3.Lerp(start, end, t));
            }
            return path;
        }
        
        public static List<Vector3> GenerateCurve(Vector3 start, Vector3 end, Vector3 control, int segments)
        {
            var path = new List<Vector3>();
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                path.Add(BezierPoint(start, control, end, t));
            }
            return path;
        }
        
        public static List<Vector3> GenerateCircle(Vector3 center, float radius, int segments)
        {
            var path = new List<Vector3>();
            for (int i = 0; i <= segments; i++)
            {
                float angle = (i / (float)segments) * 360f * Mathf.Deg2Rad;
                path.Add(center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }
            return path;
        }
        
        public static List<Vector3> GenerateSquare(Vector3 center, float size, int segmentsPerSide)
        {
            var path = new List<Vector3>();
            float halfSize = size * 0.5f;
            Vector3[] corners = new Vector3[]
            {
                center + new Vector3(-halfSize, 0, -halfSize),
                center + new Vector3(halfSize, 0, -halfSize),
                center + new Vector3(halfSize, 0, halfSize),
                center + new Vector3(-halfSize, 0, halfSize)
            };
            
            for (int i = 0; i < 4; i++)
            {
                int next = (i + 1) % 4;
                path.AddRange(GenerateLine(corners[i], corners[next], segmentsPerSide));
            }
            
            return path;
        }
        
        private static Vector3 BezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            float u = 1f - t;
            return u * u * p0 + 2f * u * t * p1 + t * t * p2;
        }
    }
}

