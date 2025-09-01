using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Input;
using System;
using MathNet.Numerics.LinearAlgebra;
using System.Linq;
using TMPro;
using System.Globalization;


public class StartHandTracking : MonoBehaviour
{
    public GameObject quad;
    public float pixelOffsetPSM2 = 0f;
    public float pixelOffsetPSM1 = 0f;
    private GameObject cylinderPSM2;
    private GameObject cylinderPSM1;
    private int width = 1300;
    private int height = 1024;
    private Vector2 imageSizePx;
    private Vector2 imageSizeMeters;
    Vector2 tipPxPSM1;
    Vector2 dirZPxPSM1;
    Vector2 zPxPSM1;
    Vector2 uvPSM1;
    Vector3 tipOnQuadPlanePSM1;
    Vector3 worldPosPSM1;
    Vector3 worldPosEndPSM1;
    Vector2 zEndPxPSM1;
    Vector2 uvEndPSM1;
    Vector3 tipOnQuadPlaneEndPSM1;
    private float zEnd;
    Vector3 directionPSM1;
    float distancePSM1;
    Vector3 midpointPSM1;
    public static bool smallDistancePSM1 = false;
    float distanceToSurfacePSM1;
    Vector2 tipPxPSM2;
    Vector2 dirZPxPSM2;
    Vector2 zPxPSM2;
    Vector2 uvPSM2;
    Vector3 tipOnQuadPlanePSM2;
    Vector3 worldPosPSM2;
    Vector3 worldPosEndPSM2;
    Vector2 zEndPxPSM2;
    Vector2 uvEndPSM2;
    Vector3 tipOnQuadPlaneEndPSM2;
    Vector3 directionPSM2;
    float distancePSM2;
    Vector3 midpointPSM2;
    public static bool smallDistancePSM2 = false;
    float distanceToSurfacePSM2;

    public Material cylinderPSM1Material;
    public Material cylinderPSM2Material;

    void Start()
    {
        imageSizePx = new Vector2(width, height);
        Bounds bounds = quad.GetComponent<Renderer>().bounds;
        imageSizeMeters = new Vector2(bounds.size.x, bounds.size.y);
    }

    void Update()
    {
        List<Vector2Int> axesPSM1 = TipVisualNew.GetProjectedAxes(TipVisualNew.EE1_pos, TipVisualNew.EE1_quat, 550f);
        List<Vector2Int> axesPSM2 = TipVisualNew.GetProjectedAxes(TipVisualNew.EE2_pos, TipVisualNew.EE2_quat, 550f);
        tipPxPSM1 = TipVisualNew.tipPositionPSM1;
        zPxPSM1 = axesPSM1[3];
        dirZPxPSM1 = (zPxPSM1 - tipPxPSM1);
        tipPxPSM2 = TipVisualNew.tipPositionPSM2;
        zPxPSM2 = axesPSM2[3];
        dirZPxPSM2 = (zPxPSM2 - tipPxPSM2);
        float zPSM2 = 0.0f;
        float zPSM1 = 0.0f;

        Vector2 offsetTipPxPSM1 = tipPxPSM1 + dirZPxPSM1.normalized * pixelOffsetPSM1;
        Vector2 offsetTipPxPSM2 = tipPxPSM2 + dirZPxPSM2.normalized * pixelOffsetPSM2;

        // From pixel to world position (in the quad)
        uvPSM1 = new Vector2(offsetTipPxPSM1.x / imageSizePx.x, offsetTipPxPSM1.y / imageSizePx.y);
        uvPSM2 = new Vector2(offsetTipPxPSM2.x / imageSizePx.x, offsetTipPxPSM2.y / imageSizePx.y);
        Vector3 localPointPSM1 = new Vector3(
            (uvPSM1.x - 0.5f) * imageSizeMeters.x,
            (0.5f - uvPSM1.y) * imageSizeMeters.y,
            0f
        );
        tipOnQuadPlanePSM1 = quad.transform.TransformPoint(localPointPSM1);
        worldPosPSM1 = quad.transform.position + quad.transform.right * localPointPSM1.x + quad.transform.up * localPointPSM1.y + quad.transform.forward * zPSM1;

        Vector3 localPointPSM2 = new Vector3(
            (uvPSM2.x - 0.5f) * imageSizeMeters.x,
            (0.5f - uvPSM2.y) * imageSizeMeters.y,
            0f
        );
        tipOnQuadPlanePSM2 = quad.transform.TransformPoint(localPointPSM2);
        worldPosPSM2 = quad.transform.position + quad.transform.right * localPointPSM2.x + quad.transform.up * localPointPSM2.y + quad.transform.forward * zPSM2;

        // End Pixel Position
        zEndPxPSM1 = offsetTipPxPSM1 + dirZPxPSM1 * 2f;
        zEndPxPSM2 = offsetTipPxPSM2 + dirZPxPSM2 * 2f;

        uvEndPSM1 = new Vector2(zEndPxPSM1.x / imageSizePx.x, zEndPxPSM1.y / imageSizePx.y);
        uvEndPSM2 = new Vector2(zEndPxPSM2.x / imageSizePx.x, zEndPxPSM2.y / imageSizePx.y);

        zEnd = 0f;

        Vector3 localPointEndPSM1 = new Vector3(
            (uvEndPSM1.x - 0.5f) * imageSizeMeters.x,
            (0.5f - uvEndPSM1.y) * imageSizeMeters.y,
            0
        );

        Vector3 localPointEndPSM2 = new Vector3(
            (uvEndPSM2.x - 0.5f) * imageSizeMeters.x,
            (0.5f - uvEndPSM2.y) * imageSizeMeters.y,
            0
        );

        tipOnQuadPlaneEndPSM1 = quad.transform.TransformPoint(localPointEndPSM1);
        tipOnQuadPlaneEndPSM2 = quad.transform.TransformPoint(localPointEndPSM2);

        worldPosEndPSM1 = quad.transform.position + quad.transform.right * localPointEndPSM1.x + quad.transform.up * localPointEndPSM1.y + quad.transform.forward * zEnd;
        worldPosEndPSM2 = quad.transform.position + quad.transform.right * localPointEndPSM2.x + quad.transform.up * localPointEndPSM2.y + quad.transform.forward * zEnd; ;
        /*
if (!CalibrationScript.calib_completed || MovecameraLikeConsole.isOpen)
{
    Destroy(cylinderPSM1);
    Destroy(cylinderPSM2);
    return;
}
*/
        if (cylinderPSM1 == null)
        {
            cylinderPSM1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            if (cylinderPSM1Material != null)
                //cylinderPSM1.GetComponent<Renderer>().material = cylinderPSM1Material;
            cylinderPSM1.GetComponent<Renderer>().enabled = false;
            Destroy(cylinderPSM1.GetComponent<Collider>());
        }

        if (cylinderPSM2 == null)
        {
            cylinderPSM2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            if (cylinderPSM2Material != null)
                //cylinderPSM2.GetComponent<Renderer>().material = cylinderPSM2Material;
            cylinderPSM2.GetComponent<Renderer>().enabled = false;
            Destroy(cylinderPSM2.GetComponent<Collider>());
        }

        directionPSM1 = worldPosPSM1 - worldPosEndPSM1;
        directionPSM2 = worldPosPSM2 - worldPosEndPSM2;

        distancePSM1 = directionPSM1.magnitude;
        distancePSM2 = directionPSM2.magnitude;

        midpointPSM1 = worldPosEndPSM1 + directionPSM1 / 2f;
        midpointPSM2 = worldPosEndPSM2 + directionPSM2 / 2f;

        cylinderPSM1.transform.position = midpointPSM1;
        cylinderPSM2.transform.position = midpointPSM2;

        cylinderPSM1.transform.up = directionPSM1.normalized;
        cylinderPSM2.transform.up = directionPSM2.normalized;

        cylinderPSM1.transform.localScale = new Vector3(0.05f, distancePSM1 / 2f, 0.14f);
        cylinderPSM2.transform.localScale = new Vector3(0.05f, distancePSM2 / 2f, 0.14f);

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
        if (distanceToSurface <= 0.08f)
        {
            result = true;
        }
        if (distanceToSurface > 0.08f)
        {
            result = false;
        }

        return result;
    }
}
