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
            (uvPSM1.x - 0.5f) *  imageSizeMeters.x,
            (0.5f - uvPSM1.y) *  imageSizeMeters.y,
            0f
        );
        //tipOnQuadPlanePSM1 = quad.transform.TransformPoint(localPointPSM1);
        worldPosPSM1 = quad.transform.position + quad.transform.right * localPointPSM1.x + quad.transform.up * localPointPSM1.y + quad.transform.forward * 0;

        Vector3 localPointPSM2 = new Vector3(
            (uvPSM2.x - 0.5f) *  imageSizeMeters.x,
            (0.5f - uvPSM2.y) *  imageSizeMeters.y,
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

        // Rotazione di 90 gradi attorno all'asse X
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
        SetCylinderTransform(cylinderPSM1, worldPosPSM1, worldPosEndPSM1, instrumentLength, instrumentRadius);
        SetCylinderTransform(cylinderPSM2, worldPosPSM2, worldPosEndPSM2, instrumentLength, instrumentRadius);


        // Distance from hand to cylinder:
        smallDistancePSM1 = IsThumbNearCylinder(cylinderPSM1, worldPosPSM1, worldPosEndPSM1, Handedness.Right, out distanceToSurfacePSM1);
        smallDistancePSM2 = IsThumbNearCylinder(cylinderPSM2, worldPosPSM2, worldPosEndPSM2, Handedness.Left, out distanceToSurfacePSM2);
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
    private void SetCylinderTransform(GameObject cylinder, Vector3 start, Vector3 end, float length, float radius)
    {
        Vector3 direction = end - start;
        float distance = direction.magnitude;
        Vector3 midpoint = start + direction / 2f;

        cylinder.transform.position = midpoint;
        cylinder.transform.up = direction.normalized;
        cylinder.transform.localScale = new Vector3(radius, length / 2f, radius); // radius=0.03, height=length
    }
}
