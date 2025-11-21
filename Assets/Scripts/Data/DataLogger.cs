using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NeuroReachVR.Core;
using NeuroReachVR.Tasks;
using NeuroReachVR.Utils;

namespace NeuroReachVR.Data
{
    /// <summary>
    /// Logs performance and kinematic data to CSV files
    /// Async writing to avoid frame drops
    /// NOW WITH: Full time-series export, session ID per task, file rotation
    /// </summary>
    public class DataLogger : MonoBehaviour
    {
        [Header("Logging Settings")]
        [SerializeField] private bool loggingEnabled = true;
        [SerializeField] private string fileName = "NeuroReachVR_Data";
        [SerializeField] private bool appendTimestamp = true;

        [Header("Session Info")]
        [SerializeField] private string patientID = "Unknown";
        [SerializeField] private string sessionID;

        private const int BATCH_SIZE = 10; // Lines per batch before yield
        private const long MAX_FILE_SIZE = 10 * 1024 * 1024; // 10MB

        private StreamWriter writer;
        private StreamWriter kinematicWriter;
        private Queue<string> writeQueue;
        private bool isWriting;
        private string filePath;
        private string kinematicFilePath;
        private int attemptNumber;
        private string currentTaskType;
        private DifficultyLevel currentDifficulty;
        private long currentFileSize;

        private void Awake()
        {
            writeQueue = new Queue<string>();
            // Don't generate session ID here - generate per task session
            InitializeFile();
        }

        private void OnDestroy()
        {
            FlushAndClose();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
                FlushQueue();
        }

        private void InitializeFile()
        {
            if (!loggingEnabled) return;

            try
            {
                string directory = Application.persistentDataPath;
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                string timestamp = appendTimestamp ? DateTime.Now.ToString("yyyyMMdd_HHmmss") : "";
                string fullFileName = $"{fileName}_{timestamp}.csv";
                filePath = Path.Combine(directory, fullFileName);

                writer = new StreamWriter(filePath, true, Encoding.UTF8);
                WriteHeader();
                currentFileSize = 0;

                Debug.Log($"[DataLogger] Logging to: {filePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[DataLogger] Failed to initialize file: {e.Message}");
                loggingEnabled = false;
            }
        }

        private void InitializeKinematicFile()
        {
            if (!loggingEnabled || string.IsNullOrEmpty(sessionID)) return;

            try
            {
                string directory = Application.persistentDataPath;
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string kinematicFileName = $"NeuroReachVR_Kinematic_{sessionID}_{timestamp}.csv";
                kinematicFilePath = Path.Combine(directory, kinematicFileName);

                kinematicWriter = new StreamWriter(kinematicFilePath, false, Encoding.UTF8);

                // Write kinematic header
                string kinematicHeader = "Timestamp,SessionID,PatientID,TaskType,AttemptNumber," +
                                       "SampleTime,PositionX,PositionY,PositionZ," +
                                       "VelocityX,VelocityY,VelocityZ," +
                                       "AccelerationX,AccelerationY,AccelerationZ," +
                                       "RotationX,RotationY,RotationZ,RotationW";
                kinematicWriter.WriteLine(kinematicHeader);
                kinematicWriter.Flush();

                Debug.Log($"[DataLogger] Kinematic logging to: {kinematicFilePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[DataLogger] Failed to initialize kinematic file: {e.Message}");
            }
        }

        private void WriteHeader()
        {
            string header = "Timestamp,PatientID,TaskType,Difficulty,SessionID,CompletionTime,Accuracy,SuccessRate," +
                           "MovementSmoothness,HandPositionX,HandPositionY,HandPositionZ,VelocityX,VelocityY,VelocityZ," +
                           "ErrorCount,AttemptNumber,AdaptiveAdjustment";
            WriteLine(header);
        }

        public void LogTaskAttempt(BaseTask task, KinematicDataCollector kinematicData, bool success,
            float accuracy, int errorCount, string adaptiveAdjustment = "")
        {
            if (!loggingEnabled) return;

            // Validate data
            if (!ValidateData(accuracy, errorCount))
                return;

            attemptNumber++;
            currentTaskType = task.GetType().Name;

            var latestSample = kinematicData.GetLatestSample();
            var allSamples = kinematicData.Samples;
            float smoothness = KinematicsCalculator.CalculateSmoothness(allSamples);

            // Log summary row to main file
            string row = FormatCSVRow(
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                patientID,
                currentTaskType,
                currentDifficulty.ToString(),
                sessionID,
                task.ElapsedTime.ToString("F3"),
                accuracy.ToString("F3"),
                success ? "1" : "0",
                smoothness.ToString("F3"),
                latestSample.position.x.ToString("F3"),
                latestSample.position.y.ToString("F3"),
                latestSample.position.z.ToString("F3"),
                latestSample.velocity.x.ToString("F3"),
                latestSample.velocity.y.ToString("F3"),
                latestSample.velocity.z.ToString("F3"),
                errorCount.ToString(),
                attemptNumber.ToString(),
                adaptiveAdjustment
            );

            WriteLine(row);

            // Log full kinematic time-series data
            LogKinematicTimeSeries(allSamples);
        }

        private void LogKinematicTimeSeries(List<KinematicSample> samples)
        {
            if (kinematicWriter == null || samples == null || samples.Count == 0) return;

            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

                foreach (var sample in samples)
                {
                    string row = FormatCSVRow(
                        timestamp,
                        sessionID,
                        patientID,
                        currentTaskType,
                        attemptNumber.ToString(),
                        sample.timestamp.ToString("F3"),
                        sample.position.x.ToString("F3"),
                        sample.position.y.ToString("F3"),
                        sample.position.z.ToString("F3"),
                        sample.velocity.x.ToString("F3"),
                        sample.velocity.y.ToString("F3"),
                        sample.velocity.z.ToString("F3"),
                        sample.acceleration.x.ToString("F3"),
                        sample.acceleration.y.ToString("F3"),
                        sample.acceleration.z.ToString("F3"),
                        sample.rotation.x.ToString("F3"),
                        sample.rotation.y.ToString("F3"),
                        sample.rotation.z.ToString("F3"),
                        sample.rotation.w.ToString("F3")
                    );

                    kinematicWriter.WriteLine(row);
                }

                kinematicWriter.Flush();
            }
            catch (Exception e)
            {
                Debug.LogError($"[DataLogger] Failed to log kinematic data: {e.Message}");
            }
        }

        // Removed duplicate - now using KinematicsCalculator.CalculateSmoothness()

        public void LogSessionStart(string taskType, DifficultyLevel difficulty)
        {
            currentTaskType = taskType;
            currentDifficulty = difficulty;
            attemptNumber = 0;

            // Generate NEW session ID for this task session
            sessionID = Guid.NewGuid().ToString();
            Debug.Log($"[DataLogger] New session started: {sessionID}");

            // Initialize kinematic data file for this session
            InitializeKinematicFile();
        }

        public void SetPatientID(string id)
        {
            patientID = string.IsNullOrEmpty(id) ? "Unknown" : id;
        }

        public void SetDifficulty(DifficultyLevel difficulty)
        {
            currentDifficulty = difficulty;
        }

        private string FormatCSVRow(params string[] values)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0) sb.Append(",");
                string value = values[i] ?? "";
                // Escape commas and quotes
                if (value.Contains(",") || value.Contains("\""))
                {
                    value = "\"" + value.Replace("\"", "\"\"") + "\"";
                }
                sb.Append(value);
            }
            return sb.ToString();
        }

        private void WriteLine(string line)
        {
            if (!loggingEnabled || writer == null) return;

            writeQueue.Enqueue(line);
            currentFileSize += line.Length;

            // Check file size and rotate if needed
            if (currentFileSize >= MAX_FILE_SIZE)
            {
                RotateLogFile();
            }

            if (!isWriting)
                StartCoroutine(WriteQueueCoroutine());
        }

        private IEnumerator WriteQueueCoroutine()
        {
            if (isWriting) yield break;
            isWriting = true;

            bool hasError = false;
            string errorMessage = "";

            int lineCount = 0;
            while (writeQueue.Count > 0)
            {
                string line = null;
                try
                {
                    line = writeQueue.Dequeue();
                    writer.WriteLine(line);
                    lineCount++;
                }
                catch (Exception e)
                {
                    hasError = true;
                    errorMessage = e.Message;
                    break;
                }

                // Yield every few lines to avoid frame drops (outside try-catch)
                if (lineCount % BATCH_SIZE == 0)
                    yield return null;
            }

            try
            {
                if (!hasError)
                    writer.Flush();
            }
            catch (Exception e)
            {
                hasError = true;
                errorMessage = e.Message;
            }
            finally
            {
                isWriting = false;
                if (hasError)
                    Debug.LogError($"[DataLogger] Write error: {errorMessage}");
            }
        }

        private void FlushQueue()
        {
            if (writer != null && writeQueue.Count > 0)
            {
                try
                {
                    while (writeQueue.Count > 0)
                    {
                        writer.WriteLine(writeQueue.Dequeue());
                    }
                    writer.Flush();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[DataLogger] Flush error: {e.Message}");
                }
            }
        }

        private void FlushAndClose()
        {
            FlushQueue();

            if (writer != null)
            {
                writer.Close();
                writer.Dispose();
                writer = null;
            }

            if (kinematicWriter != null)
            {
                kinematicWriter.Close();
                kinematicWriter.Dispose();
                kinematicWriter = null;
            }
        }

        private void RotateLogFile()
        {
            Debug.Log("[DataLogger] Rotating log file - max size reached");

            FlushQueue();

            if (writer != null)
            {
                writer.Close();
                writer.Dispose();
            }

            // Create new file with timestamp
            InitializeFile();
        }

        public void SetLoggingEnabled(bool enabled)
        {
            loggingEnabled = enabled;
            if (!enabled)
                FlushAndClose();
        }

        public string GetFilePath()
        {
            return filePath;
        }

        private bool ValidateData(float accuracy, int errorCount)
        {
            if (float.IsNaN(accuracy) || float.IsInfinity(accuracy))
            {
                Debug.LogWarning($"[DataLogger] Invalid accuracy: {accuracy}");
                return false;
            }

            if (accuracy < 0f || accuracy > 1f)
            {
                Debug.LogWarning($"[DataLogger] Accuracy out of range: {accuracy}");
                return false;
            }

            if (errorCount < 0)
            {
                Debug.LogWarning($"[DataLogger] Invalid error count: {errorCount}");
                return false;
            }

            return true;
        }
    }
}
