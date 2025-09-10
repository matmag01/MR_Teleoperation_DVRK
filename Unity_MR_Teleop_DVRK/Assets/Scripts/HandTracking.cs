using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Input;
using System;
using MathNet.Numerics.LinearAlgebra;
using System.Linq;
using TMPro;
using System.Globalization;

public class HandTracking : MonoBehaviour
{
    // Set hands to PSMs variable
    string PSM_flag;
    string PSM1 = "PSM1";
    string PSM2 = "PSM2";

    // MRKT Variable
    Handedness hand;
    MixedRealityPose pose;

    // Object to identify hands and GameObject 
    public GameObject sphere_marker;
    public GameObject axis;
    public float thresholdClutch = 0.003f;
    private GameObject thumbMarker;
    private GameObject handAxis;
    public GameObject UDP;
    public GameObject quad;
    public GameObject hololens;
    public GameObject teleop_on;
    public GameObject teleop_off;

    // Hand/Finger postion
    Vector3 thumbPos;
    Vector3 indexPos;
    Vector3 palmPos;
    Vector3 wristPos;
    Vector3 middlePos;
    Vector3 thumb_prox_pos;
    Quaternion thumbRot;
    Quaternion indexRot;
    Quaternion palmRot;
    Quaternion wristRot;
    Quaternion middleRot;
    Quaternion thumb_prox_rot;

    // Distances
    public static float pinch_dist;
    public static float pinch_dist_PSM1;
    public static float pinch_dist_PSM2;
    float clutch_distance;
    //Vector3 lastPosition;
    //Vector3 currentPosition;
    //Quaternion currentRotation;
    //Quaternion lastRotation;
    Vector3 startHandPos;
    Quaternion startHandRot;

    // Robot variable
    float jaw_angle;
    private Vector3 EE_pos;
    private Quaternion EE_quat;
    Vector3 new_EE_pos;
    Quaternion new_EE_rot;
    Vector3 startEEPos;
    Quaternion startEERot;
    Vector3 EE_pos_send;
    Matrix<float> EE_rot_send = Matrix<float>.Build.Random(3, 3);

    // Control
    public float scale = 0.02f;
    string pose_message;
    string jaw_message;
    float jawAngleSend;

    // Flags
    public bool teleop;
    bool handWasNotTracked = true;
    bool checkPose = true;
    bool firstTime;
    bool firstTimeLeft = true;
    bool firstTimeRight = true;
    bool firstTimeOutOfView = true;
    static public bool arduinoPSM1 = false;
    static public bool arduinoPSM2 = false;

    // Time
    float T = 0f;
    // Filter
    MotionFilter PSM1MotionFilter;
    MotionFilter PSM2MotionFilter;
    public float rotationScale = 0.45f;
    private QuaternionEMAFilter PSM1RotFilter;
    private QuaternionEMAFilter PSM2RotFilter;

    // Feedback
    public GameObject audioFeedback;

    void Start()
    {
        thumbMarker = Instantiate(sphere_marker, this.transform);
        handAxis = Instantiate(axis, this.transform);
        handAxis.transform.localScale = new Vector3(0.015f, 0.015f, 0.025f);
        PSM1MotionFilter = new MotionFilter();
        PSM1MotionFilter.smoothingFactor = 0.8f;
        PSM2MotionFilter = new MotionFilter();
        PSM2MotionFilter.smoothingFactor = 0.8f;
        PSM1RotFilter = new QuaternionEMAFilter(0.2f);
        PSM2RotFilter = new QuaternionEMAFilter(0.2f);
        firstTime = true;

    }

    // Update is called once per frame
    void Update()
    {
        // Disactivate object attached to user hand
        thumbMarker.GetComponent<Renderer>().enabled = false;
        handAxis.SetActive(false);

        // Finger, hand and wrist position
        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbTip, hand, out pose))
        {
            thumbPos = pose.Position;
            thumbRot = pose.Rotation;
            thumbMarker.GetComponent<Renderer>().enabled = true;
            thumbMarker.transform.position = pose.Position;
        }
        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, hand, out pose))
        {
            indexPos = pose.Position;
            indexRot = pose.Rotation;
        }
        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.MiddleTip, hand, out pose))
        {
            middlePos = pose.Position;
            middleRot = pose.Rotation;
        }
        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.Palm, hand, out pose))
        {
            palmPos = pose.Position;
            palmRot = pose.Rotation;
            handAxis.SetActive(true);
            handAxis.transform.position = pose.Position;
            handAxis.transform.rotation = pose.Rotation;
        }
        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.Wrist, hand, out pose))
        {
            wristPos = pose.Position;
            wristRot = pose.Rotation;
        }

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbProximalJoint, hand, out pose))
        {
            thumb_prox_pos = pose.Position;
            thumb_prox_rot = pose.Rotation;
            //thumb_prox.GetComponent<Renderer>().enabled = true;
            //thumb_prox.transform.position = pose.Position;

            // rotation axis
        }        

        // Pinch distance computation --> clutch state check
        pinch_dist = Vector3.Magnitude(thumbPos - indexPos);
        // Separate PSM1 and PSM2 and take robot position
        if (PSM_flag == PSM1)
        {
            pinch_dist_PSM1 = pinch_dist;
            jaw_angle = UDPComm.jaw_angle_PSM1;
            EE_pos = UDPComm.EE_pos_PSM1;
            EE_quat = UDPComm.EE_quat_PSM1;
        }
        if (PSM_flag == PSM2)
        {
            pinch_dist_PSM2 = pinch_dist;
            jaw_angle = UDPComm.jaw_angle_PSM2;
            EE_pos = UDPComm.EE_pos_PSM2;
            EE_quat = UDPComm.EE_quat_PSM2;
        }
        if (teleop && CalibrationScript.calib_completed)
        {
            firstTimeOutOfView = true;
            clutch_distance = CalibrationScript.calibrated_pinch;
            if (thumbMarker.GetComponent<Renderer>().enabled && !MovecameraLikeConsole.isOpen)
            {
                if (PSM_flag == PSM1)
                {
                    if (!InstrumentDrawing.smallDistancePSM1)
                    {
                        Debug.Log("STOP: ");
                        if (PSM_flag == PSM1)
                        {
                            quad.transform.Find("QuadBgRight").GetComponent<MeshRenderer>().material.color = Color.gray;
                        }
                        if (firstTimeRight)
                        {
                            firstTimeRight = false;
                            StartCoroutine(audioFeedback.GetComponent<AudioFeedback>().LeftRightTooFar("right"));
                        }
                        checkPose = true;
                        arduinoPSM1 = false;
                        ClutchPSM();
                        return;
                    }
                    else
                    {
                        firstTimeRight = true;
                        arduinoPSM1 = true;
                    }
                }
                if (PSM_flag == PSM2)
                {
                    if (!InstrumentDrawing.smallDistancePSM2)
                    {
                        Debug.Log("STOP: ");
                        if (PSM_flag == PSM2)
                        {
                            quad.transform.Find("QuadBgLeft").GetComponent<MeshRenderer>().material.color = Color.gray;
                        }
                        if (firstTimeLeft)
                        {
                            firstTimeLeft = false;
                            StartCoroutine(audioFeedback.GetComponent<AudioFeedback>().LeftRightTooFar("left"));
                        }
                        checkPose = true;
                        arduinoPSM2 = false;
                        ClutchPSM();
                        return;
                    }
                    else
                    {
                        firstTimeLeft = true;
                        arduinoPSM2 = true;
                    }
                }
                if (handWasNotTracked)
                {
                    //lastPosition = hololens.transform.localPosition + wristPos;
                    //lastRotation = handAxis.transform.rotation;
                    checkPose = true;
                    handWasNotTracked = false;
                }
                // Move or clutch only if camera is fixed
                if (!MovecameraLikeConsole.isOpen)
                {
                    if (pinch_dist < clutch_distance + thresholdClutch)
                    {
                        // Clutch state
                        if (PSM_flag == PSM1)
                        {
                            quad.transform.Find("QuadBgRight").GetComponent<MeshRenderer>().material.color = Color.red;
                        }
                        else if (PSM_flag == PSM2)
                        {
                            quad.transform.Find("QuadBgLeft").GetComponent<MeshRenderer>().material.color = Color.red;
                        }
                        checkPose = true;
                        ClutchPSM();
                        
                    }
                    if (pinch_dist > clutch_distance + thresholdClutch)
                    {
                        //Teleop
                        MovePSM();
                    }
                }
            }
            else
            {
                handWasNotTracked = true;
                checkPose = true;
                T = 0;
                PSM1MotionFilter.ResetEMAValue();
                PSM2MotionFilter.ResetEMAValue();
                if (PSM_flag == PSM1)
                {
                    quad.transform.Find("QuadBgRight").GetComponent<MeshRenderer>().material.color = Color.yellow;

                }
                else if (PSM_flag == PSM2)
                {
                    quad.transform.Find("QuadBgLeft").GetComponent<MeshRenderer>().material.color = Color.yellow;
                }
            }
        }
        else
        {
            //lastPosition = hololens.transform.localPosition + wristPos;
            //lastRotation = handAxis.transform.rotation;
            checkPose = true;
            handWasNotTracked = true;
            T = 0;
            PSM1MotionFilter.ResetEMAValue();
            PSM2MotionFilter.ResetEMAValue();
        }

    }


    // --- functions --- //

    // Clutch
    public void ClutchPSM()
    {

        //lastPosition = hololens.transform.localPosition + wristPos;
        //lastRotation = handAxis.transform.rotation;
        startHandPos = hololens.transform.localPosition + wristPos;
        startHandRot = handAxis.transform.rotation;
        startEEPos = EE_pos;
        startEERot = EE_quat;
        /*
        if (new_EE_pos == Vector3.zero | EE_pos == new_EE_pos)
        {
            startEEPos = EE_pos;
            startEERot = EE_quat;
        }
        else
        {
            startEEPos = new_EE_pos;
            startEERot = new_EE_rot;
        }
        */
        T = 0.0f;
    }

    // Teleop
    public void MovePSM()
    {
        if (PSM_flag == PSM1)
        {
            quad.transform.Find("QuadBgRight").GetComponent<MeshRenderer>().material.color = Color.green ;

        }
        else if (PSM_flag == PSM2)
        {
            quad.transform.Find("QuadBgLeft").GetComponent<MeshRenderer>().material.color = Color.green;
        }
        if (checkPose)
        {
            startHandPos = hololens.transform.localPosition + wristPos;
            startHandRot = handAxis.transform.rotation;
            startEEPos = EE_pos;
            startEERot = EE_quat;
            /*
            if (new_EE_pos == Vector3.zero | EE_pos == new_EE_pos)
            {
                startEEPos = EE_pos;
                startEERot = EE_quat;
            }
            else
            {
                // Hack!
                startEEPos = new_EE_pos;
                startEERot = new_EE_rot;
                Debug.Log("Hack!");
            }
            */
            checkPose = false;
        }

        // Position control
        Vector3 delta = hololens.transform.InverseTransformVector(hololens.transform.position + wristPos - startHandPos);
        // From Hololens to CameraRF
        delta = SwitchFrame(delta);
        // Scaling
        Vector3 scaled_delta = scale * Vector3.Scale(delta, new Vector3(-1.0f, -1.0f, 1.0f));
        // Position of the dVRK to be updated
        new_EE_pos = startEEPos + scaled_delta;
        // Filter
        if (PSM_flag == PSM1)
            new_EE_pos = PSM1MotionFilter.UpdateEMA(new_EE_pos);
        else if (PSM_flag == PSM2)
            new_EE_pos = PSM2MotionFilter.UpdateEMA(new_EE_pos);

        // convert pos to same orientation as dVRK i.e. y and z swapped    
        EE_pos_send[0] = new_EE_pos[0];
        EE_pos_send[1] = new_EE_pos[2];
        EE_pos_send[2] = new_EE_pos[1];
        //Debug.Log("MOVE: PSM flag: " + PSM_flag + " EE pose: " + EE_pos + "EE start pose: " + startEEPos + "new EE pose: " + new_EE_pos);


        // Rotation control

        Quaternion axis_rot = Quaternion.Inverse(startHandRot) * handAxis.transform.rotation;
        //axis_rot = new(axis_rot.x, axis_rot.z, axis_rot.y, axis_rot.w);
        if (PSM_flag == PSM2 || PSM_flag == PSM1)
        {
            Quaternion offset = new(-0.7071f, 0.0f, 0.0f, 0.7071f);
            axis_rot = Quaternion.Inverse(offset) * axis_rot * offset;
        }


        // --- ROTATION SCALING --- //

        // Extract angle and axis
        axis_rot.ToAngleAxis(out float angle, out Vector3 axis);
        axis.Normalize();

        // Scale the angle
        float scaledAngle = angle * rotationScale;

        // Quaternion from axis and scaled angle
        Quaternion scaled_rot = Quaternion.AngleAxis(scaledAngle, axis);

        // Apply rotation to start EE rotation
        new_EE_rot = scaled_rot * startEERot;
        Debug.Log("Angle: " + angle + " Axis: " + axis + " Scaled Angle: " + scaledAngle + " New EE rot: " + new_EE_rot);

        // FINE MODIFICA




        //new_EE_rot = axis_rot * startEERot; // Attention: x axis is shifted (I think)
        
        
        // Apply EMA filter to rotation
        if (PSM_flag == PSM1)
            new_EE_rot = PSM1RotFilter.UpdateEMA(new_EE_rot);
        else if (PSM_flag == PSM2)
            new_EE_rot = PSM2RotFilter.UpdateEMA(new_EE_rot);
        

        //Debug.Log("MOVE : PSM flag: " + PSM_flag + " EE rot: " + EE_quat + "EE start rot: " + startEERot + "new EE pose: " + new_EE_rot + "jaw angle: " + jaw_angle);

        EE_rot_send = Quat2Rot(Quaternion.Normalize(new_EE_rot)); // convert normalised quaternion to rotation matrix
        pose_message = VectorFromMatrix(EE_pos_send, EE_rot_send);// get message to send to dVRK (new EE pose)
        jawAngleSend = MoveJaw();
        jaw_message = "{\"jaw/move_jp\":{\"Goal\":[" + jawAngleSend.ToString("R", CultureInfo.InvariantCulture) + "]}}";
        //Debug.Log(jaw_message);
        if (PSM_flag == "PSM1")
        {
            UDP.GetComponent<UDPComm>().UDPsend(pose_message, jaw_message, "PSM1");
        }
        else if (PSM_flag == "PSM2")
        {
            UDP.GetComponent<UDPComm>().UDPsend(pose_message, jaw_message, "PSM2");
        }
    }
    public float MoveJaw()
    {
        float toSend = jaw_angle;
        bool stopJawAngle = false;
        float desAngle = JawAngle(thumbPos, middlePos);
        if (Math.Abs(jaw_angle - desAngle) < 0.02)
        {
            // Stop moving
            stopJawAngle = true;
            //Debug.Log("Angle diff < 2");
            T = 0;
        }
        if (stopJawAngle)
        {
            toSend = desAngle;
        }
        if (!stopJawAngle)
        {
            toSend = ((desAngle - jaw_angle) / 2.0f) * T + jaw_angle;
            T += Time.deltaTime;
        }
        return toSend;
    }
    
    // This function sets PSM1 to right hand and PSM2 to left hand
    public void SetPSM(String PSM)
    {
        PSM_flag = PSM;
        //Debug.Log("Initialize HandTrack the PSM is: "+PSM);
        if (PSM_flag == PSM1)
        {
            hand = Handedness.Right;
        }
        else if (PSM_flag == PSM2)
        {
            hand = Handedness.Left;
        }
        else
        {
            hand = Handedness.Right;
            Debug.LogError("The manipulator is assigned to PSM1 by default");
        }
    }
    float JawAngle(Vector3 first_pos, Vector3 second_pos)
    {
        float dist = Vector3.Distance(first_pos, second_pos);
        float threshold_1 = 0.04f;
        float threshold_2 = 0.12f;
        float angle = 0f;
        //Debug.Log("Dist: " + dist);
        if (dist <= threshold_1)
        {
            angle = -20;
        }
        else if (threshold_1 < dist && dist < threshold_2)
        {
            angle = 80 * (dist - threshold_1) / (threshold_2 - threshold_1);
        }
        else if (dist >= threshold_2)
        {
            angle = 80;
        }
        //Debug.Log("thumb-middle distance: " + dist);
        //Debug.Log("thumb-middle distance: " + dist+"\nJaw angle: " + angle);
        return angle * Mathf.Deg2Rad;
    }

    Vector3 SwitchFrame(Vector3 trans)
    {
        float y = trans.y;
        float z = trans.z;
        trans.y = -z;
        trans.z = y;
        return trans;
    }
    // get quaternion from homogeneous matrix
    public static Quaternion QuaternionFromMatrix(Matrix4x4 m)
    {
        return Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));
    }

    // position vector and 3x3 rotation matrix to json string
    public static string VectorFromMatrix(Vector3 pos, Matrix<float> rot)
    {
        jsonFloat jsonfloat = new jsonFloat();

        // translation
        float[] translation = new float[3];
        for (int i = 0; i < 3; i++) // only get the first 3 rows of Transformation matrix
        {
            translation[i] = pos[i];
        }

        // rotation
        string rotation = string.Empty;
        rotation += "[";
        for (int i = 0; i < 3; i++)
        {
            if (i == 0)
            {
                rotation += "[";
            }
            else
            {
                rotation += ",[";
            }
            for (int j = 0; j < 3; j++)
            {
                rotation += rot[i, j].ToString("R", CultureInfo.InvariantCulture).ToLower();    // dVRK seems to only read lower case e for scientific notation (i.e. e-07 and not E-07)
                if (j == 2)
                {
                    rotation += "]";
                }
                else
                {
                    rotation += ",";
                }
            }
        }

        rotation += "]";
        jsonfloat.Rotation = rotation;
        jsonfloat.Translation = translation;
        string mockString = JsonUtility.ToJson(jsonfloat);
        //begin funny string operation because jsonUtility doesn't support 2d array serialization
        string[] mockArray = mockString.Split('[');
        string first_part = mockArray[0].Substring(0, mockArray[0].Length - 1);
        string second_part = mockString.Substring(mockArray[0].Length, mockString.Length - mockArray[0].Length);
        string[] mockArray_2 = second_part.Split('\"');
        string second_part_2 = mockArray_2[0];
        string third_part = second_part.Substring(second_part_2.Length + 1, second_part.Length - (second_part_2.Length + 1));
        string final_string = first_part + second_part_2 + third_part;
        //final_string = "{\"servo_cp\": {\"Goal\": " + final_string + "}}";
        final_string = "{\"move_cp\": {\"Goal\": " + final_string + "}}";
        return final_string;
    }

    // convert quaternion to rotation matrix
    public static Matrix<float> Quat2Rot(Quaternion q)
    {
        Matrix<float> m = Matrix<float>.Build.Random(3, 3);

        m[0, 0] = 1 - 2 * Mathf.Pow(q.y, 2) - 2 * Mathf.Pow(q.z, 2);
        m[0, 1] = 2 * q.x * q.y - 2 * q.w * q.z;
        m[0, 2] = 2 * q.x * q.z + 2 * q.w * q.y;
        m[1, 0] = 2 * q.x * q.y + 2 * q.w * q.z;
        m[1, 1] = 1 - 2 * Mathf.Pow(q.x, 2) - 2 * Mathf.Pow(q.z, 2);
        m[1, 2] = 2 * q.y * q.z - 2 * q.w * q.x;
        m[2, 0] = 2 * q.x * q.z - 2 * q.w * q.y;
        m[2, 1] = 2 * q.y * q.z + 2 * q.w * q.x;
        m[2, 2] = 1 - 2 * Mathf.Pow(q.x, 2) - 2 * Mathf.Pow(q.y, 2);

        return m;
    }

    // Quaternion EMA filter class
    public class QuaternionEMAFilter
    {
        private Quaternion emaValue;
        private float smoothingFactor;
        private bool initialized = false;
        public QuaternionEMAFilter(float smoothing)
        {
            smoothingFactor = smoothing;
        }
        public Quaternion UpdateEMA(Quaternion newValue)
        {
            if (!initialized)
            {
                emaValue = newValue;
                initialized = true;
                return emaValue;
            }
            emaValue = Quaternion.Slerp(emaValue, newValue, 1 - smoothingFactor);
            return emaValue;
        }
        public void ResetEMAValue()
        {
            initialized = false;
        }
    }
}