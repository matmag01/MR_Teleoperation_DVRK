using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Input;
using System;
using MathNet.Numerics.LinearAlgebra;
using System.Linq;
using TMPro;
using System.Globalization;

public class TipVisual : MonoBehaviour
{
    public GameObject UDP;

    // Robot variable
    public static Vector3 EE1_pos;
    public static Quaternion EE1_quat;
    public static Vector3 EE2_pos;
    public static Quaternion EE2_quat;
    //public static Matrix4x4 calib = new Matrix4x4();
    public static readonly Matrix<float> calib = Matrix<float>.Build.DenseOfArray(new float[,]
    {
        { 1621.6418f, 0f, 498.3872f },
        { 0f, 1627.7641f, 549.8018f },
        { 0f, 0f, 1f }
    });
    Matrix4x4 T_cam_robot = new Matrix4x4();
    Matrix4x4 T_ECM_PSM = new Matrix4x4();
    public static Vector2Int tipPositionPSM1;
    public static Vector2Int tipPositionPSM2;

    public static TipVisual Instance;
    void Awake()
    {
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        // Calibration matrix
        // calib = K [R|t] proiezione camera 3x4
        // Init matrix
        /*
        calib.SetRow(0, new Vector4(1621.6418f, 0f, 498.3872f, 0f));
        calib.SetRow(1, new Vector4(0f, 1627.7641f, 549.8018f, 0f));
        calib.SetRow(2, new Vector4(0f, 0f, 1f, 0f));
        calib.SetRow(3, new Vector4(0f, 0f, 0f, 1f));
        */
        // Matrice da frame robot a frame camera (hand-eye)
        T_cam_robot.SetRow(0, new Vector4(0.589745f, -0.627691f, 0.508137f, 0.198506f));
        T_cam_robot.SetRow(1, new Vector4(0.807461f, 0.447073f, -0.384881f, 0.195905f));
        T_cam_robot.SetRow(2, new Vector4(0.014412f, 0.637283f, 0.770495f, -0.042342f));
        T_cam_robot.SetRow(3, new Vector4(0f, 0f, 0f, 1f));
        T_ECM_PSM.SetRow(0, new Vector4(
            -0.3698945f,  0.4264364f, -0.8254272f, -0.12297405f));

        T_ECM_PSM.SetRow(1, new Vector4(
            0.5930221f,  0.7922838f,  0.14356576f,  0.111135654f));

        T_ECM_PSM.SetRow(2, new Vector4(
            0.7151943f, -0.43639237f, -0.5459477f,  0.16568446f));

        T_ECM_PSM.SetRow(3, new Vector4(
            0f, 0f, 0f, 1f));
    }

    // Update is called once per frame
    void Update()
    {
        // Extract position:
        EE1_pos = UDPComm.EE_pos_PSM1;
        EE1_quat = UDPComm.EE_quat_PSM1;
        EE2_pos = UDPComm.EE_pos_PSM2;
        EE2_quat = UDPComm.EE_quat_PSM2;
        Debug.Log("quat position: " + EE2_pos);


        // Tip position
        tipPositionPSM1 = ProjectToPixel((EE1_pos));
        tipPositionPSM2 = ProjectToPixel(RosToUnityPosition(EE2_pos));
        Debug.Log("Pixel pos: " + tipPositionPSM2);
        List<Vector2Int> axes = GetProjectedAxes(EE2_pos, EE2_quat);

        Debug.Log($"Origin: {axes[0]}, X: {axes[1]}, Y: {axes[2]}, Z: {axes[3]}");
    }
    Vector2Int ProjectToPixel(Vector3 pos)
    {
        Vector3 posRos = (pos);
        Debug.Log("Punti ros: " + posRos);
        Vector4 point3D = new Vector4(posRos.x, posRos.y, -posRos.z, 1f);

        // Projection
        Vector3 proj = new Vector3();

        for (int i = 0; i < 3; i++)
        {
            float val = 0f;
            for (int j = 0; j < 4; j++)
            {
                val += calib[i, j] * point3D[j];
            }
            if (i == 0) proj.x = val;
            if (i == 1) proj.y = val;
            if (i == 2) proj.z = val;
        }

        // Pixel position
        float x_img = proj.x / proj.z;
        float y_img = proj.y / proj.z;

        //Debug.Log("PixelPosition: ( " + x_img + "; " + y_img + " )");

        return new Vector2Int(Mathf.RoundToInt(x_img), Mathf.RoundToInt(y_img));
    }
    
    Vector2Int ProjectToPixelRos(Vector3 pos)
    {
        Vector4 pointRobot = new Vector4(pos.x, pos.y, pos.z, 1f);

        // Trasformazione da robot a camera
        Vector4 pointCam = T_ECM_PSM * pointRobot;

        // Proiezione intrinseca
        Vector3 proj = new Vector3();

        for (int i = 0; i < 3; i++)
        {
            float val = 0f;
            for (int j = 0; j < 4; j++)
            {
                val += calib[i, j] * pointCam[j];
            }
            if (i == 0) proj.x = val;
            if (i == 1) proj.y = val;
            if (i == 2) proj.z = val;
        }

        // Controllo sicurezza
        if (Mathf.Abs(proj.z) < 1e-5f)
        {
            Debug.LogWarning("Punto proiettato dietro la camera o troppo vicino.");
            return new Vector2Int(-1, -1);
        }

        // Conversione a pixel
        float x_img = proj.x / proj.z;
        float y_img = - proj.y / proj.z;

        return new Vector2Int(Mathf.RoundToInt(x_img), Mathf.RoundToInt(y_img));
    }


    public static List<Vector2Int> GetProjectedAxes(Vector3 pos, Quaternion quat, float axisLength = 0.01f)
    {
        List<Vector2Int> axisPixels = new List<Vector2Int>();

        Vector3 origin = RosToUnityPosition(pos); // solo per posizione
        Matrix4x4 R = Matrix4x4.Rotate(quat);     // usa direttamente il quaternion ricevuto

        Vector3 x = origin + (Vector3)(R.MultiplyVector(Vector3.right) * axisLength);
        Vector3 y = origin + (Vector3)(R.MultiplyVector(Vector3.up) * axisLength);
        Vector3 z = origin + (Vector3)(R.MultiplyVector(Vector3.forward) * axisLength);

        Vector2Int origin_px = Instance.ProjectToPixel(origin);
        Vector2Int x_px = Instance.ProjectToPixel(x);
        Vector2Int y_px = Instance.ProjectToPixel(y);
        Vector2Int z_px = Instance.ProjectToPixel(z);

        axisPixels.Add(origin_px); // origin
        axisPixels.Add(x_px);      // x
        axisPixels.Add(y_px);      // y
        axisPixels.Add(z_px);      // z

        return axisPixels;
    }

    public static Vector3 RosToUnityPosition(Vector3 rosPos)
    {
        return new Vector3(rosPos.x, rosPos.z, rosPos.y);  // Scambia Y e Z
    }
    Matrix4x4 QuaternionToMatrix(Quaternion q)
    {
    return Matrix4x4.Rotate(q);
    }
}

