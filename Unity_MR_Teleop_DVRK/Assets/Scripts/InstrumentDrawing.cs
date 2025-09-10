using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Input;
using System;
using MathNet.Numerics.LinearAlgebra;
using System.Linq;
using TMPro;
using System.Globalization;
using Unity.Mathematics;


public class InstrumentDrawing : MonoBehaviour
{
    public GameObject quad;
    private GameObject cylinderPSM2;
    private GameObject cylinderPSM1;
    private int width = 1300;
    private int height = 1024;
    private Vector2 imageSizePx;
    private Vector2 imageSizeMeters;
    Vector2 tipPxPSM1;
    Vector2 uvPSM1;
    Vector3 worldPosPSM1;
    Vector3 worldPosEndPSM1;
    Vector3 midpointPSM1;
    public static bool smallDistancePSM1 = false;
    float distanceToSurfacePSM1;
    Vector2 tipPxPSM2;
    Vector2 uvPSM2;
    Vector3 tipOnQuadPlanePSM2;
    Vector3 worldPosPSM2;
    Vector3 worldPosEndPSM2;
    public static bool smallDistancePSM2 = false;
    float distanceToSurfacePSM2;
    public Material cylinderMaterial;
    quaternion quatPSM1;
    quaternion quatPSM2;
    public float instrumentLength = 0.2f; //20 cm
    public float instrumentRadius = 0.01f; //1 cm
    MotionFilter cylinderPSM1MotionFilter;
    MotionFilter cylinderPSM2MotionFilter;
    public float smoothingFactor = 0.61f;
    bool PSM1 = true;
    public float maxDistance = 0.07f; // 7 cm

    void Start()
    {
        imageSizePx = new Vector2(width, height);
        Bounds bounds = quad.GetComponent<Renderer>().bounds;
        imageSizeMeters = new Vector2(bounds.size.x, bounds.size.y);
    }

    void Update()
    {
        // Tip Orientation
        quatPSM1 = TipVisualNew.EE1_quat;
        quatPSM2 = TipVisualNew.EE2_quat;

        // Tip position
        tipPxPSM1 = TipVisualNew.tipPositionPSM1;
        tipPxPSM2 = TipVisualNew.tipPositionPSM2;

        // From pixel to world position (in the quad)
        uvPSM1 = new Vector2(tipPxPSM1.x / imageSizePx.x, tipPxPSM1.y / imageSizePx.y);
        uvPSM2 = new Vector2(tipPxPSM2.x / imageSizePx.x, tipPxPSM2.y / imageSizePx.y);
        Vector3 localPointPSM1 = new Vector3(
            (uvPSM1.x - 0.5f) * imageSizeMeters.x,
            (0.5f - uvPSM1.y) * imageSizeMeters.y,
            0f
        );
        //tipOnQuadPlanePSM1 = quad.transform.TransformPoint(localPointPSM1);
        worldPosPSM1 = quad.transform.position + quad.transform.right * localPointPSM1.x + quad.transform.up * localPointPSM1.y + quad.transform.forward * 0;

        Vector3 localPointPSM2 = new Vector3(
            (uvPSM2.x - 0.5f) * imageSizeMeters.x,
            (0.5f - uvPSM2.y) * imageSizeMeters.y,
            0f
        );
        tipOnQuadPlanePSM2 = quad.transform.TransformPoint(localPointPSM2);
        worldPosPSM2 = quad.transform.position + quad.transform.right * localPointPSM2.x + quad.transform.up * localPointPSM2.y + quad.transform.forward * 0;

        // Compute direction in ECM RF (forward vector rotated by quaternion)
        Vector3 dirPSM1_ECM = math.mul(quatPSM1, Vector3.forward);
        Vector3 dirPSM2_ECM = math.mul(quatPSM2, Vector3.forward);

        // Switch X and Y axes to convert from right-handed to Unity's left-handed system
        float tempX1 = dirPSM1_ECM.x;
        dirPSM1_ECM.x = dirPSM1_ECM.y;
        dirPSM1_ECM.y = tempX1;

        float tempX2 = dirPSM2_ECM.x;
        dirPSM2_ECM.x = dirPSM2_ECM.y;
        dirPSM2_ECM.y = tempX2;

        // Rotate direction by 90 degrees around Z axis to align with dvrk ECM reference frame
        Quaternion rot90 = Quaternion.AngleAxis(90f, Vector3.forward);
        dirPSM1_ECM = rot90 * dirPSM1_ECM;
        dirPSM2_ECM = rot90 * dirPSM2_ECM;

        // Transform direction to quadrant/world RF
        Vector3 dirPSM1_Quad = quad.transform.TransformDirection(dirPSM1_ECM);
        Vector3 dirPSM2_Quad = quad.transform.TransformDirection(dirPSM2_ECM);

        // Invert direction to rotate cylinder by 180 degrees
        dirPSM1_Quad *= -1f;
        dirPSM2_Quad *= -1f;

        // Compute end positions for cylinders
        worldPosEndPSM1 = worldPosPSM1 + dirPSM1_Quad * instrumentLength;
        worldPosEndPSM2 = worldPosPSM2 + dirPSM2_Quad * instrumentLength;


        // If calibration not completed or camera open, destroy cylinders and reset filters --> Cylinders re-created only when teleop is on
        /*
        if (!CalibrationScript.calib_completed || MovecameraLikeConsole.isOpen)
        {
            Destroy(cylinderPSM1);
            Destroy(cylinderPSM2);
            return;
        }
        */

        // Create cylinders if needed
        if (cylinderPSM1 == null)
        {
            cylinderPSM1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinderPSM1.GetComponent<Renderer>().material = cylinderMaterial;
            Destroy(cylinderPSM1.GetComponent<Collider>());
        }
        if (cylinderPSM2 == null)
        {
            cylinderPSM2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinderPSM2.GetComponent<Renderer>().material = cylinderMaterial;
            Destroy(cylinderPSM2.GetComponent<Collider>());
        }

        // Set cylinder transforms
        SetCylinderTransform(cylinderPSM1, worldPosPSM1, worldPosEndPSM1, instrumentLength, instrumentRadius, PSM1 = true);
        SetCylinderTransform(cylinderPSM2, worldPosPSM2, worldPosEndPSM2, instrumentLength, instrumentRadius, PSM1 = false);

        // Distance from hand to cylinder:
        //smallDistancePSM1 = IsThumbNearCylinder(cylinderPSM1, worldPosPSM1, worldPosEndPSM1, Handedness.Right, out distanceToSurfacePSM1);
        //smallDistancePSM2 = IsThumbNearCylinder(cylinderPSM2, worldPosPSM2, worldPosEndPSM2, Handedness.Left, out distanceToSurfacePSM2);
        smallDistancePSM1 = IsHandNearCylinder(cylinderPSM1, worldPosPSM1, worldPosEndPSM1, Handedness.Right, maxDistance);
        smallDistancePSM2 = IsHandNearCylinder(cylinderPSM2, worldPosPSM2, worldPosEndPSM2, Handedness.Left, maxDistance);
    }
    private bool IsHandNearCylinder(GameObject cylinder, Vector3 start, Vector3 end, Handedness hand, float threshold)
    {
        // Initialize distance to a large value
        float distanceToSurface = 100f;

        if (cylinder == null)
            return false;

        // Consider every joint of the hand for proximity check (all joint except ring and pinky fingers)
        List<TrackedHandJoint> jointsToConsider = new List<TrackedHandJoint>
        {
            TrackedHandJoint.Wrist,
            TrackedHandJoint.Palm,
            TrackedHandJoint.ThumbMetacarpalJoint,
            TrackedHandJoint.ThumbProximalJoint,
            TrackedHandJoint.ThumbDistalJoint,
            TrackedHandJoint.ThumbTip,
            TrackedHandJoint.IndexMetacarpal,
            TrackedHandJoint.IndexKnuckle,
            TrackedHandJoint.IndexMiddleJoint,
            TrackedHandJoint.IndexDistalJoint,
            TrackedHandJoint.IndexTip,
            TrackedHandJoint.MiddleMetacarpal,
            TrackedHandJoint.MiddleKnuckle,
            TrackedHandJoint.MiddleMiddleJoint,
            TrackedHandJoint.MiddleDistalJoint,
            TrackedHandJoint.MiddleTip,
            TrackedHandJoint.RingMetacarpal,
            TrackedHandJoint.PinkyMetacarpal
        };

        foreach (var joint in jointsToConsider)
        {
            // If joint not found, skip to next
            if (!HandJointUtils.TryGetJointPose(joint, hand, out MixedRealityPose pose))
                continue;

            Vector3 jointPos = pose.Position;

            // Distance computation
            Vector3 A = start;
            Vector3 B = end;
            Vector3 AB = B - A;
            Vector3 AP = jointPos - A;

            float t = Mathf.Clamp01(Vector3.Dot(AP, AB) / AB.sqrMagnitude);
            Vector3 closestPoint = A + t * AB;

            float distanceToAxis = Vector3.Distance(jointPos, closestPoint);
            float radius = cylinder.transform.localScale.x / 2f;

            distanceToSurface = Mathf.Max(0f, distanceToAxis - radius);

            // If any joint is within the threshold, return true immediately --> STOP!
            if (distanceToSurface <= threshold)
            {
                return true;
            }
        }
        // If the loop completes without finding close joints,
        return false;
    }
    private bool IsThumbNearCylinder(GameObject cylinder, Vector3 start, Vector3 end, Handedness hand, out float distanceToSurface)
    {
        distanceToSurface = 100f;

        if (cylinder == null)
            return false;

        if (!HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbTip, hand, out MixedRealityPose pose))
            return false;

        Vector3 thumbPos = pose.Position;

        Vector3 A = start;
        Vector3 B = end;
        Vector3 AB = B - A;
        Vector3 AP = thumbPos - A;

        float t = Mathf.Clamp01(Vector3.Dot(AP, AB) / AB.sqrMagnitude);
        Vector3 closestPoint = A + t * AB;

        float distanceToAxis = Vector3.Distance(thumbPos, closestPoint);
        float radius = cylinder.transform.localScale.x / 2f;

        distanceToSurface = Mathf.Max(0f, distanceToAxis - radius);
        bool result = false;
        if (distanceToSurface <= 0.06f)
        {
            result = true;
        }
        if (distanceToSurface > 0.06f)
        {
            result = false;
        }

        return result;
    }

    // Utility to set cylinder transform
    private void SetCylinderTransform(GameObject cylinder, Vector3 start, Vector3 end, float length, float radius, bool PSM1)
    {
        Vector3 direction = end - start;
        float distance = direction.magnitude;
        Vector3 midpoint = start + direction / 2f;
        cylinder.transform.position = midpoint;
        cylinder.transform.up = direction.normalized;
        cylinder.transform.localScale = new Vector3(radius, length / 2f, radius); // radius=0.03, height=length
    }
}
