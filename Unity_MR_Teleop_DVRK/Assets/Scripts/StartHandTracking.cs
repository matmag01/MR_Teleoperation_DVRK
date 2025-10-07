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
        // Get projected axes and tip positions
        List<Vector2Int> axesPSM1 = TipVisualNew.GetProjectedAxes(TipVisualNew.EE1_pos, TipVisualNew.EE1_quat, 600f);
        List<Vector2Int> axesPSM2 = TipVisualNew.GetProjectedAxes(TipVisualNew.EE2_pos, TipVisualNew.EE2_quat, 600f);
        tipPxPSM1 = TipVisualNew.tipPositionPSM1;
        zPxPSM1 = axesPSM1[3];
        dirZPxPSM1 = (zPxPSM1 - tipPxPSM1);
        tipPxPSM2 = TipVisualNew.tipPositionPSM2;
        zPxPSM2 = axesPSM2[3];
        dirZPxPSM2 = zPxPSM2 - tipPxPSM2;

        // Cylinder creation
        cylinderPSM1 = EnsureCylinder(cylinderPSM1, cylinderPSM1Material);
        cylinderPSM2 = EnsureCylinder(cylinderPSM2, cylinderPSM2Material);

        // Calculate positions
        uvPSM1 = new Vector2(tipPxPSM1.x / imageSizePx.x, tipPxPSM1.y / imageSizePx.y);
        uvPSM2 = new Vector2(tipPxPSM2.x / imageSizePx.x, tipPxPSM2.y / imageSizePx.y);

        tipOnQuadPlanePSM1 = PixelToWorld(uvPSM1, quad, imageSizeMeters);
        tipOnQuadPlanePSM2 = PixelToWorld(uvPSM2, quad, imageSizeMeters);

        worldPosPSM1 = tipOnQuadPlanePSM1;
        worldPosPSM2 = tipOnQuadPlanePSM2;

        // End Pixel Position
        zEndPxPSM1 = tipPxPSM1 + dirZPxPSM1;
        zEndPxPSM2 = tipPxPSM2 + dirZPxPSM2;

        uvEndPSM1 = new Vector2(zEndPxPSM1.x / imageSizePx.x, zEndPxPSM1.y / imageSizePx.y);
        uvEndPSM2 = new Vector2(zEndPxPSM2.x / imageSizePx.x, zEndPxPSM2.y / imageSizePx.y);

        tipOnQuadPlaneEndPSM1 = PixelToWorld(uvEndPSM1, quad, imageSizeMeters);
        tipOnQuadPlaneEndPSM2 = PixelToWorld(uvEndPSM2, quad, imageSizeMeters);

        worldPosEndPSM1 = tipOnQuadPlaneEndPSM1;
        worldPosEndPSM2 = tipOnQuadPlaneEndPSM2;

        // Cylinder transform setup
        SetupCylinderTransform(cylinderPSM1, worldPosPSM1, worldPosEndPSM1);
        SetupCylinderTransform(cylinderPSM2, worldPosPSM2, worldPosEndPSM2);

        // Distance from hand to cylinder:
        smallDistancePSM1 = IsThumbNearCylinder(cylinderPSM1, worldPosPSM1, worldPosEndPSM1, Handedness.Right, out distanceToSurfacePSM1);
        smallDistancePSM2 = IsThumbNearCylinder(cylinderPSM2, worldPosPSM2, worldPosEndPSM2, Handedness.Left, out distanceToSurfacePSM2);
    }

    // Utility to create cylinder if needed and assign material
    private GameObject EnsureCylinder(GameObject cylinder, Material mat)
    {
        if (cylinder == null)
        {
            cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            if (mat != null)
                cylinder.GetComponent<Renderer>().material = mat;
            cylinder.GetComponent<Renderer>().enabled = false;
            Destroy(cylinder.GetComponent<Collider>());
        }
        return cylinder;
    }

    // Utility to convert pixel UV to world position on quad
    private Vector3 PixelToWorld(Vector2 uv, GameObject quad, Vector2 imageSizeMeters)
    {
        Vector3 localPoint = new Vector3(
            (uv.x - 0.5f) * imageSizeMeters.x,
            (0.5f - uv.y) * imageSizeMeters.y,
            0f
        );
        return quad.transform.TransformPoint(localPoint);
    }

    // Utility to set cylinder transform
    private void SetupCylinderTransform(GameObject cylinder, Vector3 start, Vector3 end)
    {
        Vector3 direction = start - end;
        float distance = direction.magnitude;
        Vector3 midpoint = end + direction / 2f;

        cylinder.transform.position = midpoint;
        cylinder.transform.up = direction.normalized;
        cylinder.transform.localScale = new Vector3(0.05f, distance / 2f, 0.14f);
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
