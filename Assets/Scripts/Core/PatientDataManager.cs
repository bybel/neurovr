using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NeuroReachVR.Core
{
    /// <summary>
    /// Manages patient sessions, data persistence, CSV export
    /// Handles patient data storage and retrieval
    /// </summary>
    public class PatientDataManager : MonoBehaviour
    {
        [Header("Data Settings")]
        [SerializeField] private string patientDataFileName = "PatientData.json";
        [SerializeField] private bool autoSave = true;
        
        private const string PATIENT_ID_KEY = "CurrentPatientID";
        private const string SESSION_COUNT_KEY = "SessionCount_";
        private const string CSV_HEADER = "PatientID,TaskType,Duration,Score,Timestamp";
        
        private string currentPatientID;
        private Dictionary<string, PatientSessionData> patientSessions;
        private string dataFilePath;
        
        public string CurrentPatientID => currentPatientID;
        
        private void Awake()
        {
            dataFilePath = Path.Combine(Application.persistentDataPath, patientDataFileName);
            patientSessions = new Dictionary<string, PatientSessionData>();
            LoadPatientData();
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && autoSave)
                SavePatientData();
        }
        
        private void OnDestroy()
        {
            if (autoSave)
                SavePatientData();
        }
        
        public void SetCurrentPatient(string patientID)
        {
            if (string.IsNullOrEmpty(patientID))
            {
                Debug.LogWarning("[PatientDataManager] Invalid patient ID");
                return;
            }
            
            currentPatientID = patientID;
            PlayerPrefs.SetString(PATIENT_ID_KEY, patientID);
            PlayerPrefs.Save();
            
            if (!patientSessions.ContainsKey(patientID))
            {
                var session = new PatientSessionData
                {
                    patientID = patientID,
                    firstSessionDate = DateTime.Now,
                    sessionCount = 0
                };
                session.SerializeDates();
                patientSessions[patientID] = session;
            }
            
            IncrementSessionCount(patientID);
        }
        
        public string GetCurrentPatientID()
        {
            if (string.IsNullOrEmpty(currentPatientID))
            {
                currentPatientID = PlayerPrefs.GetString(PATIENT_ID_KEY, "");
            }
            return currentPatientID;
        }
        
        public int GetSessionCount(string patientID)
        {
            if (string.IsNullOrEmpty(patientID))
                patientID = currentPatientID;
            
            if (patientSessions.ContainsKey(patientID))
                return patientSessions[patientID].sessionCount;
            
            return PlayerPrefs.GetInt(SESSION_COUNT_KEY + patientID, 0);
        }
        
        public void RecordSessionCompletion(string patientID, string taskType, float duration, int score)
        {
            if (string.IsNullOrEmpty(patientID))
                patientID = currentPatientID;
            
            if (!patientSessions.ContainsKey(patientID))
            {
                var newSession = new PatientSessionData
                {
                    patientID = patientID,
                    firstSessionDate = DateTime.Now
                };
                newSession.SerializeDates();
                patientSessions[patientID] = newSession;
            }
            
            var session = patientSessions[patientID];
            session.lastSessionDate = DateTime.Now;
            session.totalSessions++;
            session.DeserializeDates();
            session.SerializeDates();
            
            var taskSession = new TaskSessionData
            {
                taskType = taskType,
                duration = duration,
                score = score,
                timestamp = DateTime.Now
            };
            taskSession.SerializeTimestamp();
            
            session.taskSessions.Add(taskSession);
        }
        
        public void ExportPatientDataToCSV(string patientID, string outputPath = null)
        {
            if (string.IsNullOrEmpty(patientID))
                patientID = currentPatientID;
            
            if (!patientSessions.ContainsKey(patientID))
            {
                Debug.LogWarning($"[PatientDataManager] No data found for patient {patientID}");
                return;
            }
            
            if (string.IsNullOrEmpty(outputPath))
            {
                string fileName = $"Patient_{patientID}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                outputPath = Path.Combine(Application.persistentDataPath, fileName);
            }
            
            try
            {
                using (var writer = new StreamWriter(outputPath))
                {
                    writer.WriteLine(CSV_HEADER);
                    
                    var session = patientSessions[patientID];
                    session.DeserializeDates();
                    foreach (var taskSession in session.taskSessions)
                    {
                        taskSession.DeserializeTimestamp();
                        string timestampStr = taskSession.timestamp != default(DateTime) 
                            ? taskSession.timestamp.ToString("yyyy-MM-dd HH:mm:ss") 
                            : taskSession.timestampString;
                        writer.WriteLine($"{patientID},{taskSession.taskType}," +
                                       $"{taskSession.duration:F2},{taskSession.score}," +
                                       $"{timestampStr}");
                    }
                }
                
                Debug.Log($"[PatientDataManager] Exported data to: {outputPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[PatientDataManager] Export failed: {e.Message}");
            }
        }
        
        public List<string> GetAllPatientIDs()
        {
            return patientSessions.Keys.ToList();
        }
        
        public PatientSessionData GetPatientData(string patientID)
        {
            if (string.IsNullOrEmpty(patientID))
                patientID = currentPatientID;
            
            return patientSessions.ContainsKey(patientID) ? patientSessions[patientID] : null;
        }
        
        private void IncrementSessionCount(string patientID)
        {
            if (!patientSessions.ContainsKey(patientID))
                return;
            
            patientSessions[patientID].sessionCount++;
            PlayerPrefs.SetInt(SESSION_COUNT_KEY + patientID, patientSessions[patientID].sessionCount);
            PlayerPrefs.Save();
        }
        
        private void LoadPatientData()
        {
            if (!File.Exists(dataFilePath))
                return;
            
            try
            {
                string json = File.ReadAllText(dataFilePath);
                var data = JsonUtility.FromJson<PatientDataContainer>(json);
                
                if (data != null && data.sessions != null)
                {
                    foreach (var session in data.sessions)
                    {
                        session.DeserializeDates();
                        foreach (var taskSession in session.taskSessions)
                        {
                            taskSession.DeserializeTimestamp();
                        }
                        patientSessions[session.patientID] = session;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PatientDataManager] Load failed: {e.Message}");
            }
        }
        
        private void SavePatientData()
        {
            try
            {
                // Serialize all dates before saving
                foreach (var session in patientSessions.Values)
                {
                    session.SerializeDates();
                    foreach (var taskSession in session.taskSessions)
                    {
                        taskSession.SerializeTimestamp();
                    }
                }
                
                var container = new PatientDataContainer
                {
                    sessions = patientSessions.Values.ToList()
                };
                
                string json = JsonUtility.ToJson(container, true);
                File.WriteAllText(dataFilePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[PatientDataManager] Save failed: {e.Message}");
            }
        }
    }
    
    [System.Serializable]
    public class PatientSessionData
    {
        public string patientID;
        public string firstSessionDateString;
        public string lastSessionDateString;
        public int sessionCount;
        public int totalSessions;
        public List<TaskSessionData> taskSessions = new List<TaskSessionData>();
        
        [System.NonSerialized]
        public DateTime firstSessionDate;
        
        [System.NonSerialized]
        public DateTime lastSessionDate;
        
        public void SerializeDates()
        {
            firstSessionDateString = firstSessionDate.ToString("O");
            lastSessionDateString = lastSessionDate.ToString("O");
        }
        
        public void DeserializeDates()
        {
            if (!string.IsNullOrEmpty(firstSessionDateString))
                DateTime.TryParse(firstSessionDateString, out firstSessionDate);
            if (!string.IsNullOrEmpty(lastSessionDateString))
                DateTime.TryParse(lastSessionDateString, out lastSessionDate);
        }
    }
    
    [System.Serializable]
    public class TaskSessionData
    {
        public string taskType;
        public float duration;
        public int score;
        public string timestampString;
        
        [System.NonSerialized]
        public DateTime timestamp;
        
        public void SerializeTimestamp()
        {
            timestampString = timestamp.ToString("O");
        }
        
        public void DeserializeTimestamp()
        {
            if (!string.IsNullOrEmpty(timestampString))
                DateTime.TryParse(timestampString, out timestamp);
        }
    }
    
    [System.Serializable]
    public class PatientDataContainer
    {
        public List<PatientSessionData> sessions;
    }
}

