using UnityEngine;
using UnityEngine.XR.Hands;
using System.Collections.Generic;
using UnityEngine.XR.Management;

namespace NeuroReachVR.Input
{
    /// <summary>
    /// Advanced hand tracking using Unity XR Hands package
    /// Provides accurate finger joint tracking and pinch detection for Meta Quest 3S
    /// </summary>
    public class HandTrackingXRHands : MonoBehaviour, IInputSource
    {
        [Header("Hand Configuration")]
        [SerializeField] private Handedness handType = Handedness.Right;
        [SerializeField] private float minConfidence = 0.5f;

        [Header("Pinch Detection")]
        [SerializeField] private float pinchThreshold = 0.03f; // 3cm
        [SerializeField] private float unpinchThreshold = 0.05f; // 5cm (hysteresis)

        private XRHandSubsystem handSubsystem;
        private XRHand hand;
        private bool isInitialized;
        private bool isPinching;
        private Vector3 lastValidPosition;
        private Quaternion lastValidRotation;

        public bool IsAvailable => isInitialized && handSubsystem != null;
        public bool IsTracking => IsAvailable && hand.isTracked && GetConfidence() >= minConfidence;
        public Vector3 Position => GetHandPosition();
        public Quaternion Rotation => GetHandRotation();
        public float Confidence => GetConfidence();

        public bool IsPinching => isPinching;
        public float PinchStrength => CalculatePinchStrength();

        private void Start()
        {
            InitializeHandTracking();
        }

        private void Update()
        {
            // Removed auto-init to prevent fighting with InputHandler. 
            // Initialization is now strictly controlled via StartTracking/StopTracking.

            if (IsTracking)
            {
                UpdatePinchState();
                CacheValidPose();
            }
        }

        private void InitializeHandTracking()
        {
            if (isInitialized && handSubsystem != null && handSubsystem.running) return;

            // Get XR Hand subsystem from SubsystemManager
            var subsystems = new List<XRHandSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);

            if (subsystems.Count > 0)
            {
                handSubsystem = subsystems[0];
            }

            if (handSubsystem == null)
            {
                Debug.LogWarning("[HandTrackingXRHands] XR Hand Subsystem not available. Ensure XR Hands package is installed and OpenXR is configured.");
                return;
            }

            // Start the subsystem if not running
            if (!handSubsystem.running)
            {
                 handSubsystem.Start();
            }

            hand = handType == Handedness.Left
                ? handSubsystem.leftHand
                : handSubsystem.rightHand;

            isInitialized = true;
            Debug.Log($"[HandTrackingXRHands] Initialized for {handType} hand using XR Hands package");
        }

        public void StopTracking()
        {
            if (handSubsystem != null && handSubsystem.running)
            {
                handSubsystem.Stop();
                Debug.Log($"[HandTrackingXRHands] Stopped subsystem for {handType}");
            }
            isInitialized = false;
        }

        public void StartTracking()
        {
            InitializeHandTracking();
        }

        private void UpdatePinchState()
        {
            float pinchDistance = GetPinchDistance();

            // Hysteresis to prevent jittering
            if (isPinching)
            {
                if (pinchDistance > unpinchThreshold)
                    isPinching = false;
            }
            else
            {
                if (pinchDistance < pinchThreshold)
                    isPinching = true;
            }
        }

        private float GetPinchDistance()
        {
            if (!hand.isTracked) return float.MaxValue;

            // Get thumb tip and index finger tip joints
            XRHandJoint thumbTip = hand.GetJoint(XRHandJointID.ThumbTip);
            XRHandJoint indexTip = hand.GetJoint(XRHandJointID.IndexTip);

            if (!thumbTip.TryGetPose(out Pose thumbPose) || !indexTip.TryGetPose(out Pose indexPose))
                return float.MaxValue;

            return Vector3.Distance(thumbPose.position, indexPose.position);
        }

        private float CalculatePinchStrength()
        {
            if (!hand.isTracked) return 0f;

            float distance = GetPinchDistance();

            // Normalize: 0 at unpinchThreshold, 1 at 0 distance
            float strength = 1f - Mathf.Clamp01(distance / unpinchThreshold);
            return strength;
        }

        private Vector3 GetHandPosition()
        {
            if (!hand.isTracked)
                return lastValidPosition;

            // Use wrist position as hand center
            XRHandJoint wrist = hand.GetJoint(XRHandJointID.Wrist);

            if (wrist.TryGetPose(out Pose pose))
                return pose.position;

            return lastValidPosition;
        }

        private Quaternion GetHandRotation()
        {
            if (!hand.isTracked)
                return lastValidRotation;

            XRHandJoint wrist = hand.GetJoint(XRHandJointID.Wrist);

            if (wrist.TryGetPose(out Pose pose))
                return pose.rotation;

            return lastValidRotation;
        }

        private float GetConfidence()
        {
            if (!hand.isTracked) return 0f;

            // XR Hands doesn't expose confidence directly, so we estimate based on tracking state
            // and whether key joints are valid
            XRHandJoint wrist = hand.GetJoint(XRHandJointID.Wrist);
            XRHandJoint indexTip = hand.GetJoint(XRHandJointID.IndexTip);
            XRHandJoint thumbTip = hand.GetJoint(XRHandJointID.ThumbTip);

            int validJoints = 0;
            if (wrist.TryGetPose(out _)) validJoints++;
            if (indexTip.TryGetPose(out _)) validJoints++;
            if (thumbTip.TryGetPose(out _)) validJoints++;

            return validJoints / 3f;
        }

        private void CacheValidPose()
        {
            if (hand.isTracked)
            {
                lastValidPosition = GetHandPosition();
                lastValidRotation = GetHandRotation();
            }
        }

        public Vector3 GetFingerTipPosition(FingerType finger)
        {
            if (!hand.isTracked) return Vector3.zero;

            XRHandJointID jointID = finger switch
            {
                FingerType.Thumb => XRHandJointID.ThumbTip,
                FingerType.Index => XRHandJointID.IndexTip,
                FingerType.Middle => XRHandJointID.MiddleTip,
                FingerType.Ring => XRHandJointID.RingTip,
                FingerType.Pinky => XRHandJointID.LittleTip,
                _ => XRHandJointID.IndexTip
            };

            XRHandJoint joint = hand.GetJoint(jointID);
            if (joint.TryGetPose(out Pose pose))
                return pose.position;

            return Vector3.zero;
        }

        public bool IsFingerExtended(FingerType finger)
        {
            if (!hand.isTracked) return false;

            // Simple heuristic: compare distance from palm to fingertip vs intermediate joint
            XRHandJoint palm = hand.GetJoint(XRHandJointID.Palm);

            XRHandJointID tipID = finger switch
            {
                FingerType.Thumb => XRHandJointID.ThumbTip,
                FingerType.Index => XRHandJointID.IndexTip,
                FingerType.Middle => XRHandJointID.MiddleTip,
                FingerType.Ring => XRHandJointID.RingTip,
                FingerType.Pinky => XRHandJointID.LittleTip,
                _ => XRHandJointID.IndexTip
            };

            XRHandJointID intermediateID = finger switch
            {
                FingerType.Thumb => XRHandJointID.ThumbProximal,
                FingerType.Index => XRHandJointID.IndexIntermediate,
                FingerType.Middle => XRHandJointID.MiddleIntermediate,
                FingerType.Ring => XRHandJointID.RingIntermediate,
                FingerType.Pinky => XRHandJointID.LittleIntermediate,
                _ => XRHandJointID.IndexIntermediate
            };

            if (!palm.TryGetPose(out Pose palmPose)) return false;

            XRHandJoint tip = hand.GetJoint(tipID);
            XRHandJoint intermediate = hand.GetJoint(intermediateID);

            if (!tip.TryGetPose(out Pose tipPose) || !intermediate.TryGetPose(out Pose intermediatePose))
                return false;

            float tipDistance = Vector3.Distance(palmPose.position, tipPose.position);
            float intermediateDistance = Vector3.Distance(palmPose.position, intermediatePose.position);

            // Extended if tip is significantly farther than intermediate joint
            return tipDistance > intermediateDistance * 1.3f;
        }
    }

    public enum FingerType
    {
        Thumb,
        Index,
        Middle,
        Ring,
        Pinky
    }
}
