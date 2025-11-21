namespace NeuroReachVR.Utils
{
    /// <summary>
    /// Centralized constants - eliminates magic numbers and strings
    /// </summary>
    public static class NeuroVRConstants
    {
        // Player Prefs Keys
        public const string PATIENT_ID_KEY = "CurrentPatientID";
        public const string LAST_SESSION_KEY = "LastSessionDate";

        // Pooling
        public const int BALLOON_POOL_MULTIPLIER = 2;

        // Path Tracing
        public const float PATH_TOLERANCE_MULTIPLIER = 1.5f;
        public const float DEFAULT_PATH_WIDTH = 0.1f;

        // Data Logging
        public const long MAX_LOG_FILE_SIZE = 10 * 1024 * 1024; // 10MB
        public const int LOG_BATCH_SIZE = 10;
        public const int MAX_KINEMATIC_SAMPLES = 3000;

        // Sampling Rates
        public const float DEFAULT_KINEMATIC_SAMPLE_RATE = 60f; // Hz
        public const float DEFAULT_SMOOTHING_FACTOR = 0.3f;

        // Tremor Analysis
        public const float MIN_TREMOR_FREQUENCY = 3f; // Hz
        public const float MAX_TREMOR_FREQUENCY = 8f; // Hz
        public const int FFT_SIZE = 512;

        // UI
        public const float UI_FADE_DURATION = 0.3f;
        public const float UI_SLIDE_DURATION = 0.4f;
        public const float UI_SCALE_DURATION = 0.25f;

        // Haptics
        public const float SUCCESS_HAPTIC_DURATION = 0.1f;
        public const float SUCCESS_HAPTIC_AMPLITUDE = 0.5f;
        public const float ERROR_HAPTIC_DURATION = 0.2f;
        public const float ERROR_HAPTIC_AMPLITUDE = 0.8f;

        // Difficulty
        public const float TARGET_TIME_EASY = 5f;
        public const float TARGET_TIME_MEDIUM = 3f;
        public const float TARGET_TIME_HARD = 2f;
        public const int MIN_ATTEMPTS_BEFORE_ADJUSTMENT = 5;
        public const int MIN_ATTEMPTS_AT_LEVEL = 10;
    }
}
