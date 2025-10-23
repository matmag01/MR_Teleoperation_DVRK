using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Input;
using System;
using MathNet.Numerics.LinearAlgebra;
using System.Linq;
using TMPro;
using System.Globalization;

public class TipVisualNew : MonoBehaviour
{
    // Robot variable
    public static Vector3 EE1_pos;
    public static Quaternion EE1_quat;
    public static Vector3 EE2_pos;
    public static Vector3 EE_ECM;
    public static Quaternion EE2_quat;
    public static Matrix<float> calib; // 3x4 projection matrix
    public static Matrix<float> calibRight; // 3x4 projection matrix
    public static Vector2Int tipPositionPSM1;
    public static Vector2Int tipPositionPSM2;
    public static Vector2Int tipPositionPSM1Right;
    public static Vector2Int tipPositionPSM2Right;

    void Start()
    {
        // Calibration (projection) matrix 3x4 from camera calibration procedure
        var matrixDataGstreamLeft = new float[,]
        {
            { 1862.78912f, 0f, 697.99173f, 0f },
            { 0f, 1862.78912f, 490.40184f, 0f },
            { 0f, 0f, 1f, 0f }
        };
        var matrixDataGstreamRight = new float[,]
        {
            { 1862.78912f, 0f, 697.99173f, -10.03737f },
            { 0f, 1862.78912f, 490.40184f, 0f },
            { 0f, 0f, 1f, 0f }
        };
        calib = Matrix<float>.Build.DenseOfArray(matrixDataGstreamLeft);
        calibRight = Matrix<float>.Build.DenseOfArray(matrixDataGstreamRight);
    }

    void Update()
    {
        EE1_pos = UDPComm.EE_pos_PSM1;
        EE1_quat = UDPComm.EE_quat_PSM1;
        EE2_pos = UDPComm.EE_pos_PSM2;
        EE2_quat = UDPComm.EE_quat_PSM2;
        EE_ECM = UDPComm.EE_pos_ECM;

        tipPositionPSM2 = ProjectToPixel(RosToUnityPosition(EE2_pos), calib);
        tipPositionPSM1 = ProjectToPixel(RosToUnityPosition(EE1_pos), calib);
        tipPositionPSM2Right = ProjectToPixel(RosToUnityPosition(EE2_pos), calibRight);
        tipPositionPSM1Right = ProjectToPixel(RosToUnityPosition(EE1_pos), calibRight);
        //print($"Tip PSM1: {tipPositionPSM1}");

        // axis computation
        //axesPSM2 = GetProjectedAxes(EE2_pos, EE2_quat);
        //Debug.Log($"Origin: {axesPSM2[0]}, X: {axesPSM2[1]}, Y: {axesPSM2[2]}, Z: {axesPSM2[3]}");
    }

    // Project a point into image plane
    public static Vector2Int ProjectToPixel(Vector3 pos,  Matrix<float> projectionMatrix)
    {
        // Create the 4D homogeneous vector from the 3D point
        float[] point3D_homogeneous = new float[] { pos.x, pos.y, pos.z, 1.0f };

        // Perform the manual 3x4 matrix by 4x1 vector multiplication
        // The result is a 3x1 vector (in homogeneous coordinates)
        float u = (projectionMatrix[0, 0] * point3D_homogeneous[0]) +
                  (projectionMatrix[0, 1] * point3D_homogeneous[1]) +
                  (projectionMatrix[0, 2] * point3D_homogeneous[2]) +
                  (projectionMatrix[0, 3] * point3D_homogeneous[3]);

        float v = (projectionMatrix[1, 0] * point3D_homogeneous[0]) +
                  (projectionMatrix[1, 1] * point3D_homogeneous[1]) +
                  (projectionMatrix[1, 2] * point3D_homogeneous[2]) +
                  (projectionMatrix[1, 3] * point3D_homogeneous[3]);

        float w = (projectionMatrix[2, 0] * point3D_homogeneous[0]) +
                  (projectionMatrix[2, 1] * point3D_homogeneous[1]) +
                  (projectionMatrix[2, 2] * point3D_homogeneous[2]) +
                  (projectionMatrix[2, 3] * point3D_homogeneous[3]);

        // Normalize the coordinates to get the final pixel position
        if (w == 0) 
        {
            Debug.LogError("The third component of the projected vector is zero. Cannot normalize.");
            return Vector2Int.zero;
        }

        float x_pixel = u / w;
        float y_pixel = v / w;

        return new Vector2Int(Mathf.RoundToInt(x_pixel), Mathf.RoundToInt(y_pixel));
    }

    // Project axis into image
    public static List<Vector2Int> GetProjectedAxes(Vector3 pos, Quaternion quat, float axisLengthPx = 50f)
    {
        List<Vector2Int> axisPixels = new List<Vector2Int>();

        Vector3 origin = RosToUnityPosition(pos);
        Matrix4x4 R = Matrix4x4.Rotate(quat);

        // Axis unit vectors in 3D space
        Vector3[] axes = new Vector3[]
        {
            R.MultiplyVector(Vector3.right),
            R.MultiplyVector(Vector3.up),
            R.MultiplyVector(Vector3.forward)
        };

        // Origin in pixels
        Vector2Int origin_px = ProjectToPixel(origin, calib);
        axisPixels.Add(origin_px);

        foreach (var axis in axes)
        {
            // Project a point on the axis
            Vector3 end3D = origin + axis;
            Vector2Int end_px = ProjectToPixel(end3D, calib);

            Vector2 dirPx = ((Vector2)end_px - (Vector2)origin_px).normalized;

            // Scaled to a fixed length in pixels
            Vector2 fixedEnd = (Vector2)origin_px + dirPx * axisLengthPx;

            axisPixels.Add(Vector2Int.RoundToInt(fixedEnd));
        }

        return axisPixels;
    }

    public static List<Vector2Int> GetProjectedAxesRigth(Vector3 pos, Quaternion quat, float axisLengthPx = 50f)
    {
        List<Vector2Int> axisPixels = new List<Vector2Int>();

        Vector3 origin = RosToUnityPosition(pos);
        Matrix4x4 R = Matrix4x4.Rotate(quat);

        // Axis unit vectors in 3D space
        Vector3[] axes = new Vector3[]
        {
            R.MultiplyVector(Vector3.right),
            R.MultiplyVector(Vector3.up),
            R.MultiplyVector(Vector3.forward)
        };

        // Origin in pixels
        Vector2Int origin_px = ProjectToPixel(origin, calibRight);
        axisPixels.Add(origin_px);

        foreach (var axis in axes)
        {
            // Project a point on the axis
            Vector3 end3D = origin + axis;
            Vector2Int end_px = ProjectToPixel(end3D, calibRight);
            Vector2 dirPx = ((Vector2)end_px - (Vector2)origin_px).normalized;

            // Scaled to a fixed length in pixels
            Vector2 fixedEnd = (Vector2)origin_px + dirPx * axisLengthPx;

            axisPixels.Add(Vector2Int.RoundToInt(fixedEnd));
        }

        return axisPixels;
    }

    // Take into account different reference frame 
    public static Vector3 RosToUnityPosition(Vector3 rosPos)
    {
        // Change Y and Z (--> in UDP.comm they are inverted wrt to ROS). The change of sign is an "offset" to solve projection problem 
        return new Vector3(-rosPos.x, -rosPos.z, rosPos.y);
    }
}
