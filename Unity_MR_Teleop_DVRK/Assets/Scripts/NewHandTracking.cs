using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Input;
using System;
using MathNet.Numerics.LinearAlgebra;
using System.Linq;
using TMPro;
using System.Globalization;

public class NewHandTracking : MonoBehaviour
{
    // declare variables //

    // for hand tracking
    public GameObject sphere_marker;
    public float scale = 0.2f;    // translational scaling factor
    public GameObject UDP;    // udp comm 
    public TextMesh teleop_text;
    public TextMesh plan_text;
    public GameObject axis;
    public GameObject teleop_on;
    public GameObject teleop_off;
    public bool teleop;
    public GameObject hololens;    // hololens
    public GameObject quad;
    public  static float pinch_dist;
    public static Vector3 new_EE_pos_PSM1;
    public static Vector3 new_EE_pos_PSM2;
    [HideInInspector]
    public  Vector3 new_EE_pos;
    public bool first_pinch = true;
    private GameObject index;
    private GameObject thumb;
    private GameObject thumb_prox;
    private GameObject middle;
    Vector3 index_pos;
    Vector3 thumb_pos;
    Vector3 thumb_prox_pos;
    Vector3 middle_pos;
    Vector3 wrist_pos;
    Quaternion wrist_rot;
    MixedRealityPose pose;
    Quaternion thumb_rot;


    Vector3 hand_start_pos;
    Quaternion hand_start_rot;
    GameObject EE_virt_pos;
    bool line_follow_start = false;
    Vector3 EE_virt_pos_start;
    Vector3 EE_line_start_pos;
    bool line_follow = false;
    Quaternion EE_line_start_rot;
    
    // for jaw angle control
    float angle_des;     // desired jaw angle from hand tracking
    float jaw_angle;     // read in jaw angle from dVRK
    float send_angle;    // jaw angle to send 
    float T = 0.0f;
    bool angle_adj_stop = false;

    // for pose control
    private Vector3 EE_pos;        // EE pos from dVRK
    private Quaternion EE_quat;    // EE rot from dVRK
    private Quaternion new_EE_rot;
    private Vector3 new_EE_pos_send;
    private Vector3 EE_start_pos;
    private Quaternion EE_start_rot;
    
    Quaternion thumb_prox_rot;
    Quaternion scaled_new_EE_rot;
    Matrix<float> new_EE_rot_send = Matrix<float>.Build.Random(3, 3);
    Quaternion holo_2_endo = new Quaternion(0, 1, 0, 0);    // transform from holo coord to dVRK endo coord
    Quaternion hand_rot_change;    // change in hand orientation

    // for udp message sending
    string pose_message;
    string jaw_message;


    float dist2dVRK;

    Quaternion index_rot;
    //public GameObject qr_tracker;

    private Queue<float> pinch_dist_q = new Queue<float>();
    /*Visual Marker*/
   
    GameObject hand_axis;
    GameObject PSM_tip_axis;
    Quaternion start_axis;
    Quaternion temp_axis_start;
    [HideInInspector]
    public bool teleop_lock = false;

    GameObject tempAxis;
    /*Motion filter*/
    MotionFilter PSM1PoseFilter;
    MotionFilter PSM2PoseFilter;
    /*PSM indentification*/
    string PSM_flag;
    string PSM1 = "PSM1";
    string PSM2 = "PSM2";
    Handedness hand;
    bool first_EnterTeleop = true;
    bool first_ExitTeleop = false;
    public void SetPSM(String PSM)
    {
        PSM_flag = PSM;
        //Debug.Log("Initialize HandTrack the PSM is: "+PSM);
        if (PSM_flag == PSM1)
        {
            hand = Handedness.Right;
        }else if(PSM_flag == PSM2)
        {
            hand = Handedness.Left;
        }
        else
        {
            //throw new System.Exception("Please assign the manipulator: PSM1 or PSM2");
            hand = Handedness.Right;
            Debug.LogError("The manipulator is assigned to PSM1 by default");
        }
        
    }
    void Start()
    {
        /* create spheres object and coordinate axis to attach to fingers*/
        thumb =Instantiate(sphere_marker,this.transform);
        hand_axis = Instantiate(axis,this.transform);
        hand_axis.transform.localScale = new Vector3(0.025f, 0.025f, 0.025f);
        PSM1PoseFilter = new MotionFilter();
        PSM1PoseFilter.smoothingFactor = 1.0f;
        PSM2PoseFilter = new MotionFilter();
        PSM2PoseFilter.smoothingFactor = 1.0f;
    }

    // Update is called once per frame
    void Update()
    {
        /*get hand tracking positions
         spheres only visible if hand is visible
        */
        
        thumb.GetComponent<Renderer>().enabled = false;

        hand_axis.SetActive(false);

        // get index pos
        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, hand, out pose))
        {
            index_pos = pose.Position;
            index_rot = pose.Rotation;
        }
        // get thumb pos
        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbTip, hand, out pose))
        {
            thumb_pos = pose.Position;
            thumb_rot = pose.Rotation;
            thumb.GetComponent<Renderer>().enabled = true;
            thumb.transform.position = pose.Position;
            
        }
       
        /*get thumb prox pos*/
        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbProximalJoint, hand, out pose))
        {
            thumb_prox_pos = pose.Position;
            thumb_prox_rot = pose.Rotation;
        }
        /*get hand pos*/
        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.Palm, hand, out pose))
        { 
            // rotation axis
            hand_axis.SetActive(true);
            hand_axis.transform.position = pose.Position;
            hand_axis.transform.rotation = pose.Rotation;
        }
        /*get middle pos*/
        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.MiddleTip, hand, out pose))
        {
            middle_pos = pose.Position;
        }
        /*get wrist pos*/
        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.Wrist, hand, out pose))
        {
            wrist_pos = pose.Position;
            wrist_rot = pose.Rotation;
        }


        /*Get dVRK pose and jaw from UDP*/
        if (PSM_flag == PSM1)
        {
            jaw_angle = UDPComm.jaw_angle_PSM1;
            EE_pos = UDPComm.EE_pos_PSM1;
            EE_quat = UDPComm.EE_quat_PSM1;
        }
        else if (PSM_flag == PSM2)
        {
            jaw_angle = UDPComm.jaw_angle_PSM2;
            EE_pos = UDPComm.EE_pos_PSM2;
            EE_quat = UDPComm.EE_quat_PSM2;
        }

        /*Visual text feedback*/
        teleop_text.GetComponent<Renderer>().enabled = true;
        //teleop_off.SetActive(true);
        //teleop_on.SetActive(false);
        
        if (teleop & CalibrationScript.calib_completed)
        {
            if (first_EnterTeleop)
            {
                //StartCoroutine(audio.GetComponent<AudioFeedback>().PSMTeleop("enter"));
                first_EnterTeleop = false;
                first_ExitTeleop = true;
            }

            bool cameraisMoving = MovecameraLikeConsole.isOpen;
            if (thumb.GetComponent<Renderer>().enabled)
            {
                pinch_dist = Vector3.Magnitude(thumb_pos - middle_pos);
                float clutch_dist = CalibrationScript.calibrated_pinch;
                if (pinch_dist < clutch_dist && !cameraisMoving /*Calib.calibrated_pinch*/)
                {
                    first_pinch = !teleop_lock;
                    teleop_lock = true;
                    //ChangeQuadBgColor("clutch");
                }
                else if (pinch_dist < clutch_dist && cameraisMoving)
                {
                    teleop_lock = false;
                    /*
                    if (!audio.GetComponent<AudioFeedback>().isWarnning)
                    {
                        StartCoroutine(audio.GetComponent<AudioFeedback>().Conflictwarnning());
                    }
                    */
                    //ChangeQuadBgColor("release");
                }
                else if (pinch_dist > clutch_dist || cameraisMoving/* (Calib.calibrated_pinch + 0.011)*/)    // only stop teleop if pinch dist greater than certain threshold
                {
                    teleop_lock = false;
                    //ChangeQuadBgColor("release");
                }


                if (teleop_lock)
                {

                    //teleop_off.SetActive(false);
                    //teleop_on.SetActive(true);

                    if (first_pinch)
                    {
                        first_pinch = false;

                        // get starting hand position
                        hand_start_pos = hololens.transform.position + wrist_pos;

                        // save start position of EE
                        if ((PSM_flag == PSM1 & new_EE_pos_PSM1 == Vector3.zero) | (PSM_flag == PSM2 & new_EE_pos_PSM2 == Vector3.zero) | EE_pos == new_EE_pos)
                        {
                            EE_start_pos = EE_pos;
                            EE_start_rot = EE_quat;
                        }
                        else
                        {
                            // this is a hack, NEEDS to be changed!!
                            EE_start_pos = new_EE_pos;
                            EE_start_rot = new_EE_rot;
                            Debug.LogWarning("Enter hack");
                        }

                        start_axis = hand_axis.transform.rotation;

                    }

                    /* Write pose message
                     * 1. Compute relative translation of how much hand has moved
                     * 2. Convert it to local coordinate transformation
                     * 3. Scale and mirror it
                     * 4. Get new EE position 
                     */

                    Vector3 relativeTrans = hololens.transform.InverseTransformVector((hololens.transform.position + wrist_pos) - hand_start_pos);
                    relativeTrans = SwitchFrame(relativeTrans);//Convert from HoloLens frame to camera frame
                    new_EE_pos = EE_start_pos + scale * Vector3.Scale(relativeTrans, new Vector3(-1.0f, -1.0f, 1.0f));
                    if (PSM_flag == PSM1)
                    {
                        new_EE_pos = PSM1PoseFilter.UpdateEMA(new_EE_pos); /*Motion filter*/
                    }
                    else if (PSM_flag == PSM2)
                    {
                        new_EE_pos = PSM2PoseFilter.UpdateEMA(new_EE_pos); /*Motion filter*/
                    }

                    //Debug.Log("relative translation: " + relativeTrans);
                    // convert pos to same orientation as dVRK i.e. y and z swapped
                    new_EE_pos_send[0] = new_EE_pos[0];
                    new_EE_pos_send[1] = new_EE_pos[2];
                    new_EE_pos_send[2] = new_EE_pos[1];

                    //Debug.Log("PSM flag: "+PSM_flag+" EE pose: "+EE_pos+"EE start pose: " + EE_start_pos + "new EE pose: " + new_EE_pos);
                    /* Use relative rotation 
                     * Euler angle representation
                     * Right order multiplication
                     * Still view in world coordinate  R_tempAxis = R_tempAxisStart*axis_rot, R_EE_new = R_EE_start*axis_rot
                     */

                    //Quaternion axis_rot = Quaternion.Inverse(temp_axis_start) * tempAxis.transform.rotation;
                    Quaternion axis_rot = (Quaternion.Inverse(start_axis) * hand_axis.transform.rotation);
                    /*This rotation works when the fourth joint -90 degrees*/
                    axis_rot.eulerAngles = new Vector3(-axis_rot.eulerAngles.x, axis_rot.eulerAngles.y, -axis_rot.eulerAngles.z);
                    /*This rotation works when the fourth joint 90 degrees*/
                    //axis_rot.eulerAngles = new Vector3(axis_rot.eulerAngles.x, -axis_rot.eulerAngles.y, -axis_rot.eulerAngles.z);
                    new_EE_rot = EE_start_rot * axis_rot;

                    /*Use absolute rotation*/

                    if (PSM_flag == PSM1)
                    {
                        Quaternion hand_in_HoloLens = Quaternion.Inverse(hololens.transform.rotation) * (hand_axis.transform.rotation * Quaternion.Euler(60f, -45f, 0f));
                        new_EE_rot = new Quaternion(hand_in_HoloLens.x, -hand_in_HoloLens.y, -hand_in_HoloLens.z, hand_in_HoloLens.w);
                    }
                    else if (PSM_flag == PSM2)
                    {
                        Quaternion hand_in_HoloLens = Quaternion.Inverse(hololens.transform.rotation) * (hand_axis.transform.rotation * Quaternion.Euler(60f, 0f, -180f));
                        new_EE_rot = new Quaternion(hand_in_HoloLens.x, -hand_in_HoloLens.y, -hand_in_HoloLens.z, hand_in_HoloLens.w);
                    }


                    /*Use Unity axis to help determine rotation mapping*/
                    //PSM_tip_axis.transform.rotation = new_EE_rot;
                    //new_EE_rot = new Quaternion(PSM_tip_axis.transform.rotation.x, -PSM_tip_axis.transform.rotation.y, -PSM_tip_axis.transform.rotation.z, PSM_tip_axis.transform.rotation.w);
                    //new_EE_rot = new Quaternion(tempAxis.transform.rotation.x, -tempAxis.transform.rotation.y, -tempAxis.transform.rotation.z, tempAxis.transform.rotation.w);

                    new_EE_rot_send = Quat2Rot(Quaternion.Normalize(new_EE_rot));// convert normalised quaternion to rotation matrix
                    pose_message = VectorFromMatrix(new_EE_pos_send, new_EE_rot_send);// get message to send to dVRK (new EE pose)

                    //Debug.Log("new EE rot: " + new_EE_rot);
                    //Debug.Log("pose message: " + pose_message);
                    /* Get jaw angle message
                     adjust jaw angle until jaw angle == finger angle
                    */
                    angle_des = JawAngle(index_pos, middle_pos);

                    if (Math.Abs(jaw_angle - angle_des) < 2)
                    {
                        angle_adj_stop = true;
                    }
                    if (angle_adj_stop)                     // if jaw angle caught up to finger angle, send desired angle
                    {
                        send_angle = (float)angle_des;
                    }
                    else
                    {
                        send_angle = ((angle_des - jaw_angle) / 10.0f) * T + jaw_angle;// if not, move jaws towards desired angle
                                                                                       // current rate: jaw angle reach desired angle in 10s
                        T += Time.deltaTime;
                    }

                    /*override send_angle for general operation*/
                    //send_angle = jaw_angle;

                    /*message to send to dVRK*/
                    jaw_message = "{\"jaw/move_jp\":{\"Goal\":[" + send_angle.ToString("R", CultureInfo.InvariantCulture) + "]}}";
                    if (PSM_flag == "PSM1")
                    {
                        UDP.GetComponent<UDPComm>().UDPsend(pose_message, jaw_message, "PSM1");
                    }
                    else if (PSM_flag == "PSM2")
                    {
                        UDP.GetComponent<UDPComm>().UDPsend(pose_message, jaw_message, "PSM2");

                    }

                }

                // not teleoperating
                else
                {
                    angle_adj_stop = false;
                    T = 0.0f;
                }
            }
            else if (!thumb.GetComponent<Renderer>().enabled)
            {
                first_pinch = true;
                //Reset the filter initial value
                PSM1PoseFilter.ResetEMAValue();
                PSM2PoseFilter.ResetEMAValue();
                /*<Audio warnning>*/
                /*
                if (!audio.GetComponent<AudioFeedback>().isOutofView && !cameraisMoving)
                {
                    StartCoroutine(audio.GetComponent<AudioFeedback>().HandisOutofView());
                }
                */
            }
        }
        else
        {

            //Reset the filter initial value
            PSM1PoseFilter.ResetEMAValue();
            PSM2PoseFilter.ResetEMAValue();
            /*<Reset quad bg color>*/
            //ChangeQuadBgColor("outTeleoperation");

            if (first_ExitTeleop)
            {
                //StartCoroutine(audio.GetComponent<AudioFeedback>().PSMTeleop("exit"));
                first_ExitTeleop = false;
                first_EnterTeleop = true;
            }

        }
     
        // if not in teleop mode
        //else
        //{
        //    // remove visual feedback
        //    teleop_text.GetComponent<Renderer>().enabled = false;
        //}
    }




    // --- functions --- //

    /* get desired jaw angle from middle and thumb positions
        dVRK jaw -> [-20; 80] degrees, fingers -> [10; 50] degrees
    */
    public float DesAngle(Vector3 thumb_pos, Vector3 middle_pos, Vector3 thumb_prox_pos)
    {
        // get length of thumb, index and thumb-index
        float thumb_len = (thumb_pos - thumb_prox_pos).sqrMagnitude;
        float middle_len = (middle_pos - thumb_prox_pos).sqrMagnitude;
        float thumb_index_len = (middle_pos - thumb_pos).sqrMagnitude;
        // angle formed by index finger and thumb
        double finger_angle = Mathf.Acos((thumb_len + middle_len - thumb_index_len) / (2 * Mathf.Sqrt(thumb_len) * Mathf.Sqrt(middle_len)));
        // desired jaw angle in radians (mapping: -20 jaw == 30 fingers, 80 jaw == 50 fingers)
        double angle_des = (5 * (Mathf.Rad2Deg * finger_angle) - 170);
        //    -20 <= angle_des <= 80
        if (angle_des < -20.0)
        {
            angle_des = -20.0;
        }
        else if (angle_des > 80.0)
        {
            angle_des = 80.0;
        }
        // return desired jaw angle
        return (float)angle_des * Mathf.Deg2Rad;
    }
    float JawAngle(Vector3 first_pos, Vector3 second_pos)
    {
        /* Lineat mapping between distance to jaw angle
         * Grab gesture: thumb-middle distance: 0.05278337
         * Flat gesture : thumb-middle distance: 0.1180264   
         */
        float dist = Vector3.Distance(first_pos, second_pos);
        float threshold_1 = 0.04f;
        float threshold_2 = 0.12f;
        float angle = 0f;
        //Debug.Log("Dist: " + dist);
        if (dist <= threshold_1)
        {
            angle= -20;
        }else if (threshold_1 < dist && dist < threshold_2)
        {
            angle = 80 * (dist - threshold_1) / (threshold_2 - threshold_1);
        }else if (dist >= threshold_2)
        {
            angle = 80;
        }
        //Debug.Log("thumb-middle distance: " + dist);
        //Debug.Log("thumb-middle distance: " + dist+"\nJaw angle: " + angle);
        return angle*Mathf.Deg2Rad;
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


    // moving average
    private float MovingAveragePos(Queue<float> pinch_dist_q)
    {
        float sum = 0.0f;


        for (int i = 0; i < pinch_dist_q.Count; i++)
        {
            sum += pinch_dist_q.ElementAt(i);
        }

        return sum / pinch_dist_q.Count;

    }

}