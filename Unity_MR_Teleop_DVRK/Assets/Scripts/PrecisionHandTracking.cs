using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Input;
using System;

public class PrecisionHandTracking : MonoBehaviour
{
    public GameObject quad;
    public static bool notPreciseHandtracking = false;

    public class JointTrackingState
    {
        public Vector3 previousPosition;
        public Vector3 previousVelocity;
        public float lastUpdateTime;
    }

    // Chiave: (mano, joint)
    private Dictionary<(Handedness, TrackedHandJoint), JointTrackingState> jointStates 
        = new Dictionary<(Handedness, TrackedHandJoint), JointTrackingState>();

    void Update()
    {
        float currentTime = Time.time;
        notPreciseHandtracking = false;

        foreach (Handedness handedness in new[] { Handedness.Left, Handedness.Right })
        {
            foreach (TrackedHandJoint joint in Enum.GetValues(typeof(TrackedHandJoint)))
            {
                if (HandJointUtils.TryGetJointPose(joint, handedness, out MixedRealityPose pose))
                {
                    Vector3 currentPosition = pose.Position;
                    var key = (handedness, joint);

                    if (!jointStates.ContainsKey(key))
                    {
                        jointStates[key] = new JointTrackingState
                        {
                            previousPosition = currentPosition,
                            previousVelocity = Vector3.zero,
                            lastUpdateTime = currentTime
                        };
                        continue;
                    }

                    JointTrackingState state = jointStates[key];
                    float deltaTime = currentTime - state.lastUpdateTime;

                    if (deltaTime <= Mathf.Epsilon)
                        continue;

                    Vector3 velocity = (currentPosition - state.previousPosition) / deltaTime;
                    Vector3 acceleration = (velocity - state.previousVelocity) / deltaTime;

                    float accelerationThreshold = 12.0f;

                    if (acceleration.magnitude > accelerationThreshold)
                    {
                        Debug.LogWarning($"Tracking unreliable --> {joint} - Hand {(handedness == Handedness.Right ? "right" : "left")} - acceleration = {acceleration.magnitude:F2} m/s²");
                        notPreciseHandtracking = true;
                    }

                    state.previousVelocity = velocity;
                    state.previousPosition = currentPosition;
                    state.lastUpdateTime = currentTime;
                }
            }
        }
    }
}
