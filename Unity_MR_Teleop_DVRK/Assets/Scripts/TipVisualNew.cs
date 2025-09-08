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
    public GameObject UDP;

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
    public static float zCoordinatePSM2;
    public static float zCoordinatePSM1;
    float deltaInsertion;
    List<Vector2Int> axesPSM2;
    public static int width = 1300;
    public static int height = 1024;
    public static int shiftX = 0;
    public static int shiftY = 0;

    void Start()
    {
        // Calibration matrix 3x3
        /*
        var matrixData1280x1024 = new float[,]
        {
            { 1621.6418f, 0f, 498.3872f },
            { 0f, 1627.7641f, 549.8018f },
            { 0f, 0f, 1f }
        };
        var matrixData1280x1024Proj = new float[,]
        {
            { 1814.948109173233f, 0f,  48.0199761390686f, 0f },
            { 0f, 1814.948109173233f, 520.2153778076172f, 0f },
            { 0f, 0f, 1f, 0f }
        };
        var matrixData1280x1024ProjRight = new float[,]
        {
            { 1814.948109173233f, 0f,  48.0199761390686f, -10.66455250459322f },
            { 0f, 1814.948109173233f, 520.2153778076172f, 0f },
            { 0f, 0f, 1f, 0f }
        };
        var matrixData1280x1024ProjNew = new float[,]
        {
            { 1925.85373f, 0f,  471.78134f, 0f },
            { 0f, 1925.85373f, 570.27438f, 0f },
            { 0f, 0f, 1f, 0f }
        };
        var matrixData1280x1024ProjRightNew = new float[,]
        {
            { 1925.85373f, 0f,  471.78134f, -10.2311f },
            { 0f, 1925.85373f, 570.27438f, 0f },
            { 0f, 0f, 1f, 0f }
        };
        
        var matrixDataGstreamLeft = new float[,]
        {
            { 1161.71027f, 0f, 531.93198f, 0f },
            { 0f, 1161.71027f, 341.46125f, 0f },
            { 0f, 0f, 1f, 0f }
        };
        var matrixDataGstreamRight = new float[,]
        {
            { 1161.71027f, 0f, 531.93198f, 6.2986f },
            { 0f, 1161.71027f, 341.46125f, 0f },
            { 0f, 0f, 1f, 0f }
        };
        
        var matrixDataGstreamLeft = new float[,]
        {
            { 1755.62582f, 0f, 836.81531f, 0f },
            { 0f, 1755.62582f, 509.52462f, 0f },
            { 0f, 0f, 1f, 0f }
        };
        var matrixDataGstreamRight = new float[,]
        {
            { 1755.62582f, 0f, 836.81531f, 9.30499f },
            { 0f, 1755.62582f, 509.52462f, 0f },
            { 0f, 0f, 1f, 0f }
        };
        */
        var matrixDataGstreamLeft = new float[,]
        {
            { 1905.46146f, 0f, 668.38528f, 0f },
            { 0f, 1905.46146f, 498.80738f, 0f },
            { 0f, 0f, 1f, 0f }
        };
        var matrixDataGstreamRight = new float[,]
        {
            {1905.46146f, 0f, 668.38528f, 10.55401f },
            { 0f, 1905.46146f, 498.80738f, 0f },
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
        zCoordinatePSM1 = RosToUnityPosition(EE1_pos).z;
        zCoordinatePSM2 = RosToUnityPosition(EE2_pos).z;

        //tipPositionPSM1 = ProjectToPixel(RosToUnityPosition(EE1_pos) - Vector3.forward*deltaInsertion, calib);
        //tipPositionPSM2 = ProjectToPixel(RosToUnityPosition(EE2_pos) - Vector3.forward*deltaInsertion, calib);
        tipPositionPSM2 = ProjectToPixel(RosToUnityPosition(EE2_pos), calib);
        tipPositionPSM1 = ProjectToPixel(RosToUnityPosition(EE1_pos), calib);
        tipPositionPSM2Right = ProjectToPixel(RosToUnityPosition(EE2_pos), calibRight);
        tipPositionPSM1Right = ProjectToPixel(RosToUnityPosition(EE1_pos), calibRight);
        print($"Tip PSM1: {tipPositionPSM1}");
        // axis computation
        //axesPSM2 = GetProjectedAxes(EE2_pos, EE2_quat);

        //Debug.Log($"Origin: {axesPSM2[0]}, X: {axesPSM2[1]}, Y: {axesPSM2[2]}, Z: {axesPSM2[3]}");
    }
    public static Vector2Int ProjectToPixel(Vector3 pos,  Matrix<float> projectionMatrix)
    {
        // 1. Create the 4D homogeneous vector from the 3D point
        float[] point3D_homogeneous = new float[] { pos.x, pos.y, pos.z, 1.0f };

        // 2. Perform the manual 3x4 matrix by 4x1 vector multiplication
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

        // 3. Normalize the coordinates to get the final pixel position
        if (w == 0) 
        {
            Debug.LogError("The third component of the projected vector is zero. Cannot normalize.");
            return Vector2Int.zero;
        }

        float x_pixel = u / w;
        float y_pixel = v / w;

        return new Vector2Int(Mathf.RoundToInt(x_pixel), Mathf.RoundToInt(y_pixel));
    }
    /*
    public static List<Vector2Int> GetProjectedAxes(Vector3 pos, Quaternion quat, float axisLength = 0.1f)
    {
        List<Vector2Int> axisPixels = new List<Vector2Int>();

        Vector3 origin = RosToUnityPosition(pos);
        Matrix4x4 R = Matrix4x4.Rotate(quat);
        Vector3 x = origin + R.MultiplyVector(Vector3.right) * axisLength;
        Vector3 y = origin + R.MultiplyVector(Vector3.up) * axisLength;
        Vector3 z = origin + R.MultiplyVector(Vector3.forward) * axisLength;

        Vector2Int origin_px = ProjectToPixel(origin, calib);
        Vector2Int x_px = ProjectToPixel(x, calib);
        Vector2Int y_px = ProjectToPixel(y, calib);
        Vector2Int z_px = ProjectToPixel(z, calib);

        axisPixels.Add(origin_px);
        axisPixels.Add(x_px);
        axisPixels.Add(y_px);
        axisPixels.Add(z_px);

        return axisPixels;
    }
    
    */
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
    Matrix<float> ResizeMatrix(Matrix<float> original, Vector2Int original_size, Vector2Int new_size)
    {
        // Obtain the intrinsic parameters from the original matrix
        float cx = original[0, 2];
        float cy = original[1, 2];

        // Compute the scaling factors
        float scaleX = (float)new_size.x / original_size.x;
        float scaleY = (float)new_size.y / original_size.y;

        // Apply the scaling to the intrinsic parameters
        float newCx = cx * scaleX;
        float newCy = cy * scaleY;

        // Create a new matrix with the updated intrinsic parameters
        Matrix<float> newMatrix = original.Clone();

        // Replace the intrinsic parameters in the new matrix
        newMatrix[0, 2] = newCx;
        newMatrix[1, 2] = newCy;

        return newMatrix;
    }

    public static Vector3 RosToUnityPosition(Vector3 rosPos)
    {
        // Change Y and Z (--> in UDP.comm they are inverted wrt to ROS). The change of sign is an offset to solve projection problem --> Need to be changed
        return new Vector3(-rosPos.x, -rosPos.z, rosPos.y);
    }
}
