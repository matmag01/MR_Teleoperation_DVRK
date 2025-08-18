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
    public static Matrix<float> calib; // 3x3 calibration matrix
    public static Vector2Int tipPositionPSM1;
    public static Vector2Int tipPositionPSM2;
    public static float zCoordinatePSM2;
    public static float zCoordinatePSM1;
    float deltaInsertion;
    List<Vector2Int> axesPSM2;
    public float startInsertionPosition;
    public static int width = 1300;
    public static int height = 1024;
    public static int shiftX = 0;
    public static int shiftY = 0;

    void Start()
    {
        // Calibration matrix 3x3
        
        var matrixData1280x1024 = new float[,]
        {
            { 1621.6418f, 0f, 498.3872f },
            { 0f, 1627.7641f, 549.8018f },
            { 0f, 0f, 1f }
        };
        var matrixData1280x1024Proj = new float[,]
        {
            { 1814.948109173233f, 0f,  48.0199761390686f },
            { 0f, 1814.948109173233f, 520.2153778076172f },
            { 0f, 0f, 1f }
        };
        
        var matrixDataStereo = new float[,]
        {
            {1625.634800342367f, 0f, 842.165381812819f},
            {0f, 1631.406282150416f, 565.8816817925815f},
            { 0f, 0f, 1f }
        };
        /*
        var matrixDataNewCalib = new float[,]
        {
            {1925.853728626144f, 0f, 471.7813415527344f},
            {0f, 1925.853728626144f, 570.2743759155273f},
            { 0f, 0f, 1f }
        };
        */
        var matrixDataNewCalib = new float[,]
        {
            {1609.3402719283479f, 0f, 491.4085151655889f},
            {0f, 1608.1829670949828f, 565.8012149050164f},
            { 0f, 0f, 1f }
        };
        
        calib = Matrix<float>.Build.DenseOfArray(matrixData1280x1024);
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

        deltaInsertion = RosToUnityPosition(EE_ECM).z - startInsertionPosition; // Conpensation Insertion vs Extraction

        tipPositionPSM1 = ProjectToPixel(RosToUnityPosition(EE1_pos) - Vector3.forward*deltaInsertion, calib);
        tipPositionPSM2 = ProjectToPixel(RosToUnityPosition(EE2_pos) - Vector3.forward*deltaInsertion, calib);
        //tipPositionPSM2 = ProjectToPixel(RosToUnityPosition(EE2_pos), calib);
        //tipPositionPSM1 = ProjectToPixel(RosToUnityPosition(EE1_pos), calib);
        Debug.Log("Pixel pos: " + tipPositionPSM2);

        // axis computation
        //axesPSM2 = GetProjectedAxes(EE2_pos, EE2_quat);

        //Debug.Log($"Origin: {axesPSM2[0]}, X: {axesPSM2[1]}, Y: {axesPSM2[2]}, Z: {axesPSM2[3]}");
        
    }

    public static Vector2Int ProjectToPixel(Vector3 pos, Matrix<float> calibMatrix)
    {
        Vector3 posRos = pos;
        Vector<float> point3D = Vector<float>.Build.DenseOfArray(new float[] {
            posRos.x,
            posRos.y,
            posRos.z
        });

        Vector<float> proj = calibMatrix * point3D;
        //Debug.Log("proj: " + proj);


        float x_img = -proj[0] / proj[2] + width/2;
        float y_img = height - proj[1] / proj[2] + 42;
        
        /*
        float x_img = -proj[0] / proj[2] + width -150;
        float y_img = height - proj[1] / proj[2] +50;
        */
        return new Vector2Int(Mathf.RoundToInt(x_img), Mathf.RoundToInt(y_img));
    }

    public static List<Vector2Int> GetProjectedAxes(Vector3 pos, Quaternion quat, float axisLength = 0.02f)
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

    public static Vector3 RosToUnityPosition(Vector3 rosPos)
    {
        return new Vector3(rosPos.x, rosPos.z, rosPos.y);  // Scambia Y e Z
    }
}
