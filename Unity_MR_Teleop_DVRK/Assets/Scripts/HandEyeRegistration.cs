using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Input;
using System;
using MathNet.Numerics.LinearAlgebra;
using System.Linq;
using TMPro;
using System.Globalization;

public class HandEyeRegistration : MonoBehaviour
{
    public GameObject UDP;

    // Robot variable
    public static Vector3 EE1_pos;
    public static Quaternion EE1_quat;
    public static Vector3 EE2_pos;
    public static Vector3 EE_ECM;
    public static Quaternion ECM_quat;
    public static Quaternion EE2_quat;
    public static Matrix<float> calib; // 3x3 calibration matrix
    public static Matrix<float> calibRight; // 3x3 calibration matrix
    public static Matrix<float> R; // 3x3 calibration matrix
    Vector<float> t;
    public static Vector2Int tipPositionPSM1;
    public static Vector2Int tipPositionPSM2;
    public static Vector2Int tipPositionPSM1Right;
    public static Vector2Int tipPositionPSM2Right;
    public static float zCoordinatePSM2;
    public static float zCoordinatePSM1;
    float deltaInsertion;
    List<Vector2Int> axesPSM2;
    public float startInsertionPosition;
    public static int width = 1300;
    public static int height = 1024;
    public static int shiftX = 0;
    public static int shiftY = 0;
    public static float baseline;

    void Start()
    {
        // Calibration matrix 3x3

        var matrixData1280x1024 = new float[,]
        {
            { 1621.6418f, 0f, 498.3872f },
            { 0f, 1627.7641f, 549.8018f },
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

        tipPositionPSM2 = ProjectToPixel(RosToUnityPosition(EE2_pos), calib);
        tipPositionPSM1 = ProjectToPixel(RosToUnityPosition(EE1_pos), calib);
        Debug.Log("Pixel pos left: " + tipPositionPSM2);
        // axis computation
        //axesPSM2 = GetProjectedAxes(EE2_pos, EE2_quat);

        //Debug.Log($"Origin: {axesPSM2[0]}, X: {axesPSM2[1]}, Y: {axesPSM2[2]}, Z: {axesPSM2[3]}");

        EE1_pos = UDPComm.EE_pos_PSM1;
        EE1_quat = UDPComm.EE_quat_PSM1;
        EE2_pos = UDPComm.EE_pos_PSM2;
        EE2_quat = UDPComm.EE_quat_PSM2;
        EE_ECM = UDPComm.EE_pos_ECM;
        ECM_quat = UDPComm.EE_quat_ECM;

        // ECM wrt base
        Matrix4x4 T_ecm_base_to_ecm_gripper = Matrix4x4.TRS(RosToUnityPosition(EE_ECM), ECM_quat, Vector3.one); 
        
        // PSM1 in camera
        Vector3 pos_psm1_in_camera_frame = T_ecm_base_to_ecm_gripper.MultiplyPoint(RosToUnityPosition(EE1_pos));

        // Proietta il punto del PSM1
        tipPositionPSM1 = ProjectToPixel(pos_psm1_in_camera_frame, calib);
        
        // Ripeti il calcolo per il PSM2
        Vector3 pos_psm2_in_camera_frame = T_ecm_base_to_ecm_gripper.MultiplyPoint(RosToUnityPosition(EE2_pos));
        tipPositionPSM2 = ProjectToPixel(pos_psm2_in_camera_frame, calib);
    
        
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
        float y_img = height - proj[1] / proj[2] +45;
        
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
