using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Input;
using System;
using MathNet.Numerics.LinearAlgebra;
using System.Globalization;
using System.IO;
using System.Text;


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
    public float thresholdClutch = 0.011f;
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
    string jointsMessage;


    // Flags
    public bool teleop;
    bool handWasNotTracked = true;
    bool checkPose = true;
    bool firstTime;
    bool firstTimeLeft = true;
    bool firstTimeRight = true;
    bool firstTimeOutOfView = true;
    static public bool isReset = false;
    bool closeRight = false;
    bool closeLeft = false;


    // Time
    float T = 0f;
    float timerRight = 0f;
    float timerLeft = 0f;
    // Filter
    MotionFilter PSM1MotionFilter;
    MotionFilter PSM2MotionFilter;
    private QuaternionEMAFilter PSM1RotFilter;
    private QuaternionEMAFilter PSM2RotFilter;


    // Feedback
    public GameObject audioFeedback;


    // .csv file

    private StreamWriter csvWriterPSM1;
    private StreamWriter csvWriterPSM2;
    private string csvFilePathPSM1;
    private string csvFilePathPSM2;
    private bool csvInitialized = false;
    public string logFilePrefix;



    void Start()
    {
        thumbMarker = Instantiate(sphere_marker, this.transform);
        handAxis = Instantiate(axis, this.transform);
        handAxis.transform.localScale = new Vector3(0.015f, 0.015f, 0.025f);
        PSM1MotionFilter = new MotionFilter();
        PSM1MotionFilter.smoothingFactor = 0.95f;
        PSM2MotionFilter = new MotionFilter();
        PSM2MotionFilter.smoothingFactor = 0.95f;
        PSM1RotFilter = new QuaternionEMAFilter(0.2f);
        PSM2RotFilter = new QuaternionEMAFilter(0.2f);
        firstTime = true;


        // Write .csv file
        
        string logFolder = Path.Combine(Application.dataPath, "Log");
        if (!Directory.Exists(logFolder))
        {
            Directory.CreateDirectory(logFolder);
        }


        if (PSM_flag == PSM1)
        {
            csvFilePathPSM1 = Path.Combine(logFolder, $"{logFilePrefix}_HandAxisRotation_PSM1.csv");
            csvWriterPSM1 = new StreamWriter(csvFilePathPSM1, false);
            csvWriterPSM1.WriteLine("Time_stamps, X_hand,Y_hand,Z_hand,W_hand,X_gripper,Y_gripper,Z_gripper,W_gripper");
        }
        else
        {
            csvFilePathPSM2 = Path.Combine(logFolder, $"{logFilePrefix}_HandAxisRotation_PSM2.csv");
            csvWriterPSM2 = new StreamWriter(csvFilePathPSM2, false);
            csvWriterPSM2.WriteLine("Time_stamps, X_hand,Y_hand,Z_hand,W_hand,X_gripper,Y_gripper,Z_gripper,W_gripper");
        }
        csvInitialized = true;
        Debug.Log($"CSV logging initialized in {logFolder} with prefix: {logFilePrefix}");
        
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
            handAxis.SetActive(true);
            handAxis.transform.position = pose.Position;
            handAxis.transform.rotation = pose.Rotation;
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
            if (thumbMarker.GetComponent<Renderer>().enabled && !MovecameraLikeConsole.isOpen && !isReset)
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
                        closeRight = true;
                        ClutchPSM();
                        return;
                    }
                    else
                    {
                        firstTimeRight = true;
                        if (closeRight)
                        {
                            quad.transform.Find("QuadBgRight").GetComponent<MeshRenderer>().material.color = Color.red;
                            timerRight += Time.deltaTime;
                            ClutchPSM();
                            if (timerRight >= 0.75f)
                            {
                                closeRight = false;
                                timerRight = 0f;
                            }
                            else
                            {
                                return;
                            }
                        }
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
                        closeLeft = true;
                        ClutchPSM();
                        return;
                    }
                    else
                    {
                        firstTimeLeft = true;
                        if (closeLeft)
                        {
                            quad.transform.Find("QuadBgLeft").GetComponent<MeshRenderer>().material.color = Color.red;
                            timerLeft += Time.deltaTime;
                            ClutchPSM();
                            if (timerLeft >= 0.75f)
                            {
                                closeLeft = false;
                                timerLeft = 0f;
                            }
                            else
                            {
                                return;
                            }
                        }


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
                if (!MovecameraLikeConsole.isOpen && !isReset)
                {
                    if (pinch_dist <= clutch_distance + thresholdClutch)
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
            quad.transform.Find("QuadBgRight").GetComponent<MeshRenderer>().material.color = Color.green;


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


        new_EE_rot = axis_rot * startEERot;




        // Apply EMA filter to rotation
        /*
        if (PSM_flag == PSM1)
            new_EE_rot = PSM1RotFilter.UpdateEMA(new_EE_rot);
        else if (PSM_flag == PSM2)
            new_EE_rot = PSM2RotFilter.UpdateEMA(new_EE_rot);
       
        */
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


        if (PSM_flag == "PSM2")
        {
            UDP.GetComponent<UDPComm>().UDPsend(pose_message, jaw_message, "PSM2");
        }
        
        if (csvInitialized)
        {
            if (PSM_flag == "PSM1" && csvWriterPSM1 != null)
            {
                Quaternion q = handAxis.transform.rotation;
                string line = string.Format(
                    "{0},{1},{2},{3},{4},{5},{6},{7},{8}",
                    Time.time.ToString("F4", CultureInfo.InvariantCulture),
                    q.x.ToString("R", CultureInfo.InvariantCulture),
                    q.y.ToString("R", CultureInfo.InvariantCulture),
                    q.z.ToString("R", CultureInfo.InvariantCulture),
                    q.w.ToString("R", CultureInfo.InvariantCulture),
                    InstrumentDrawing.PSM1_cylinderQuat.x.ToString("R", CultureInfo.InvariantCulture),
                    InstrumentDrawing.PSM1_cylinderQuat.y.ToString("R", CultureInfo.InvariantCulture),
                    InstrumentDrawing.PSM1_cylinderQuat.z.ToString("R", CultureInfo.InvariantCulture),
                    InstrumentDrawing.PSM1_cylinderQuat.w.ToString("R", CultureInfo.InvariantCulture)
                );
                csvWriterPSM1.WriteLine(line);


            }


            else if (PSM_flag == "PSM2" && csvWriterPSM2 != null)
            {
                Quaternion q = handAxis.transform.rotation;
                string line = string.Format(
                    "{0},{1},{2},{3},{4},{5},{6},{7},{8}",
                    Time.time.ToString("F4", CultureInfo.InvariantCulture),
                    q.x.ToString("R", CultureInfo.InvariantCulture),
                    q.y.ToString("R", CultureInfo.InvariantCulture),
                    q.z.ToString("R", CultureInfo.InvariantCulture),
                    q.w.ToString("R", CultureInfo.InvariantCulture),
                    InstrumentDrawing.PSM2_cylinderQuat.x.ToString("R", CultureInfo.InvariantCulture),
                    InstrumentDrawing.PSM2_cylinderQuat.y.ToString("R", CultureInfo.InvariantCulture),
                    InstrumentDrawing.PSM2_cylinderQuat.z.ToString("R", CultureInfo.InvariantCulture),
                    InstrumentDrawing.PSM2_cylinderQuat.w.ToString("R", CultureInfo.InvariantCulture)
                );
                csvWriterPSM2.WriteLine(line);
            }
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
            toSend = ((desAngle - jaw_angle) / 1.1f) * T + jaw_angle;
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
    // Send robot wrist to home position
    public void ReturnHomePosition()
    {
        quad.transform.Find("QuadBgLeft").GetComponent<MeshRenderer>().material.color = Color.gray;
        quad.transform.Find("QuadBgRight").GetComponent<MeshRenderer>().material.color = Color.gray;
        isReset = true;
        StartingScript.firstTimePSM1 = true;
        StartingScript.firstTimePSM2 = true;


        Debug.Log("Return to home position");


        Debug.Log("Return to home position PSM1");
        // joint values from UDP
        Vector<float> joints = UDPComm.PSM1_Joints;
        float yaw = joints[0];
        float pitch = joints[1];
        float insertion = joints[2];
        float roll = joints[3];
        float wrist_pitch = joints[4];
        float wrist_yaw = joints[5];
        float jaw_angle1 = UDPComm.jaw_angle_PSM1;


        // desired joint values
        roll = 0f;
        wrist_pitch = 0f;
        wrist_yaw = 0f;


        // send message
        jointsMessage = "{\"move_jp\":{\"Goal\":[" +
            yaw.ToString(CultureInfo.InvariantCulture) + "," +
            pitch.ToString(CultureInfo.InvariantCulture) + "," +
            insertion.ToString(CultureInfo.InvariantCulture) + "," +
            roll.ToString(CultureInfo.InvariantCulture) + "," +
            wrist_pitch.ToString(CultureInfo.InvariantCulture) + "," +
            wrist_yaw.ToString(CultureInfo.InvariantCulture) +
            "]}}";
        jaw_message = "{\"jaw/move_jp\":{\"Goal\":[" + jaw_angle1.ToString("R", CultureInfo.InvariantCulture) + "]}}";
        UDP.GetComponent<UDPComm>().UDPsend(jointsMessage, jaw_message, "PSM1");


        Debug.Log("Return to home position PSM2");


        // joint values from UDP
        joints = UDPComm.PSM2_Joints;
        yaw = joints[0];
        pitch = joints[1];
        insertion = joints[2];
        roll = joints[3];
        wrist_pitch = joints[4];
        wrist_yaw = joints[5];
        float jaw_angle2 = UDPComm.jaw_angle_PSM2;


        // desired joint values
        roll = 0f;
        wrist_pitch = 0f;
        wrist_yaw = 0f;


        // send message
        jointsMessage = "{\"move_jp\":{\"Goal\":[" +
            yaw.ToString(CultureInfo.InvariantCulture) + "," +
            pitch.ToString(CultureInfo.InvariantCulture) + "," +
            insertion.ToString(CultureInfo.InvariantCulture) + "," +
            roll.ToString(CultureInfo.InvariantCulture) + "," +
            wrist_pitch.ToString(CultureInfo.InvariantCulture) + "," +
            wrist_yaw.ToString(CultureInfo.InvariantCulture) +
            "]}}";
        jaw_message = "{\"jaw/move_jp\":{\"Goal\":[" + jaw_angle2.ToString("R", CultureInfo.InvariantCulture) + "]}}";
        UDP.GetComponent<UDPComm>().UDPsend(jointsMessage, jaw_message, "PSM2");
    }
    public void ClockWiseRotationPSM1()
    {
        Vector<float> joints = UDPComm.PSM1_Joints;
        float yaw = joints[0];
        float pitch = joints[1];
        float insertion = joints[2];
        float roll = joints[3];
        float wrist_pitch = joints[4];
        float wrist_yaw = joints[5];
        float jaw_angle1 = UDPComm.jaw_angle_PSM1;


        // desired joint values
        if (roll <= 3)
        {
            roll += 0.55f;
            ClutchPSM();
            // send message
            jointsMessage = "{\"move_jp\":{\"Goal\":[" +
                yaw.ToString(CultureInfo.InvariantCulture) + "," +
                pitch.ToString(CultureInfo.InvariantCulture) + "," +
                insertion.ToString(CultureInfo.InvariantCulture) + "," +
                roll.ToString(CultureInfo.InvariantCulture) + "," +
                wrist_pitch.ToString(CultureInfo.InvariantCulture) + "," +
                wrist_yaw.ToString(CultureInfo.InvariantCulture) +
                "]}}";
            jaw_message = "{\"jaw/move_jp\":{\"Goal\":[" + jaw_angle1.ToString("R", CultureInfo.InvariantCulture) + "]}}";
            UDP.GetComponent<UDPComm>().UDPsend(jointsMessage, jaw_message, "PSM1");
            ClutchPSM();
        }
    }
    public void ClockWiseRotationPSM2()
    {
        Vector<float> joints = UDPComm.PSM2_Joints;
        float yaw = joints[0];
        float pitch = joints[1];
        float insertion = joints[2];
        float roll = joints[3];
        float wrist_pitch = joints[4];
        float wrist_yaw = joints[5];
        float jaw_angle1 = UDPComm.jaw_angle_PSM1;


        if (roll <= 3)
        {
            roll += 0.55f;
            ClutchPSM();
            // send message
            jointsMessage = "{\"move_jp\":{\"Goal\":[" +
                yaw.ToString(CultureInfo.InvariantCulture) + "," +
                pitch.ToString(CultureInfo.InvariantCulture) + "," +
                insertion.ToString(CultureInfo.InvariantCulture) + "," +
                roll.ToString(CultureInfo.InvariantCulture) + "," +
                wrist_pitch.ToString(CultureInfo.InvariantCulture) + "," +
                wrist_yaw.ToString(CultureInfo.InvariantCulture) +
                "]}}";
            jaw_message = "{\"jaw/move_jp\":{\"Goal\":[" + jaw_angle1.ToString("R", CultureInfo.InvariantCulture) + "]}}";
            UDP.GetComponent<UDPComm>().UDPsend(jointsMessage, jaw_message, "PSM2");
            ClutchPSM();
        }
    }
    public void CounterClockWiseRotationPSM1()
    {
        Vector<float> joints = UDPComm.PSM1_Joints;
        float yaw = joints[0];
        float pitch = joints[1];
        float insertion = joints[2];
        float roll = joints[3];
        float wrist_pitch = joints[4];
        float wrist_yaw = joints[5];
        float jaw_angle1 = UDPComm.jaw_angle_PSM1;


        // desired joint values
        if (roll >= -3)
        {
            roll -= 0.55f;
            ClutchPSM();
            // send message
            jointsMessage = "{\"move_jp\":{\"Goal\":[" +
                yaw.ToString(CultureInfo.InvariantCulture) + "," +
                pitch.ToString(CultureInfo.InvariantCulture) + "," +
                insertion.ToString(CultureInfo.InvariantCulture) + "," +
                roll.ToString(CultureInfo.InvariantCulture) + "," +
                wrist_pitch.ToString(CultureInfo.InvariantCulture) + "," +
                wrist_yaw.ToString(CultureInfo.InvariantCulture) +
                "]}}";
            jaw_message = "{\"jaw/move_jp\":{\"Goal\":[" + jaw_angle1.ToString("R", CultureInfo.InvariantCulture) + "]}}";
            UDP.GetComponent<UDPComm>().UDPsend(jointsMessage, jaw_message, "PSM1");
            ClutchPSM();
        }
    }
    public void CounterClockWiseRotationPSM2()
    {
        Vector<float> joints = UDPComm.PSM2_Joints;
        float yaw = joints[0];
        float pitch = joints[1];
        float insertion = joints[2];
        float roll = joints[3];
        float wrist_pitch = joints[4];
        float wrist_yaw = joints[5];
        float jaw_angle1 = UDPComm.jaw_angle_PSM1;


        // desired joint values
        if (roll >= -3)
        {
            roll -= 0.55f;
            ClutchPSM();
            // send message
            jointsMessage = "{\"move_jp\":{\"Goal\":[" +
                yaw.ToString(CultureInfo.InvariantCulture) + "," +
                pitch.ToString(CultureInfo.InvariantCulture) + "," +
                insertion.ToString(CultureInfo.InvariantCulture) + "," +
                roll.ToString(CultureInfo.InvariantCulture) + "," +
                wrist_pitch.ToString(CultureInfo.InvariantCulture) + "," +
                wrist_yaw.ToString(CultureInfo.InvariantCulture) +
                "]}}";
            jaw_message = "{\"jaw/move_jp\":{\"Goal\":[" + jaw_angle1.ToString("R", CultureInfo.InvariantCulture) + "]}}";
            UDP.GetComponent<UDPComm>().UDPsend(jointsMessage, jaw_message, "PSM2");
            ClutchPSM();
        }
    }
    
    void OnApplicationQuit()
    {
        if (csvWriterPSM1 != null)
        {
            csvWriterPSM1.Flush();
            csvWriterPSM1.Close();
            Debug.Log("File CSV saved: " + csvFilePathPSM1);
        }


        if (csvWriterPSM2 != null)
        {
            csvWriterPSM2.Flush();
            csvWriterPSM2.Close();
            Debug.Log("File CSV saved: " + csvFilePathPSM2);
        }
    }
    
}

