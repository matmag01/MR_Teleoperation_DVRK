using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine.AI;
using JetBrains.Annotations;

public class StartingScript : MonoBehaviour
{
    //public GameObject homeButton;
    //public GameObject calibButton;
    float first_timer_camera_on = 0f;
    float first_timer_camera_off = 0f;
    public GameObject quad;
    public GameObject headTrack;
    public GameObject axis;
    float first_timer_PSM1_off = 0f;
    float first_timer_PSM1_on = 0f;
    float first_timer_PSM2_on = 0f;
    float first_timer_PSM2_off = 0f;
    HandTracking PSM1;
    HandTracking PSM2;
    public float scale;
    public GameObject UDP;
    public GameObject sphere_marker;
    public GameObject hololens;
    public GameObject ONPSM1;
    public GameObject OFFPSM1;
    public GameObject ONPSM2;
    public GameObject OFFPSM2;
    public GameObject calib_txt;
    public GameObject intro;
    public GameObject video;
    float count;
    bool isCenetered;
    public GameObject audioFeedback;
    static public bool firstTimePSM1 = true;
    static public bool firstTimePSM2 = true;
    public GameObject badHandTracking;
    float badhandTrackingCount = 0;
    bool firstTimeBadHandTracking = false;
    bool firstTimeAudio = true;
    public GameObject txtQuadFixed;
    public GameObject txtCameraFixed;
    public bool showVideo = false;

    // Start is called before the first frame update
    void Start()
    {
        StartHandTrack();
        firstTimeAudio = true;
        calib_txt.GetComponent<MeshRenderer>().enabled = false;
        txtQuadFixed.GetComponent<MeshRenderer>().enabled = true;
        txtCameraFixed.GetComponent<MeshRenderer>().enabled = false;
        badHandTracking.GetComponent<MeshRenderer>().enabled = false;
        //intro.GetComponent<MeshRenderer>().enabled = true;
        ONPSM2.GetComponent<MeshRenderer>().enabled = false;
        OFFPSM2.GetComponent<MeshRenderer>().enabled = true;
        ONPSM1.GetComponent<MeshRenderer>().enabled = false;
        OFFPSM1.GetComponent<MeshRenderer>().enabled = true;
        //homeButton.SetActive(false);
        //calibButton.SetActive(false);
        if (showVideo)
        {
            //video.SetActive(false);
            intro.GetComponent<MeshRenderer>().enabled = false;
        }
        else
        {
            video.SetActive(false);
            intro.GetComponent<MeshRenderer>().enabled = true;            
        }
    
        isCenetered = true;
        BoxCollider[] colliders = quad.GetComponents<BoxCollider>();
        foreach (BoxCollider col in colliders)
        {
            col.enabled = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (firstTimeAudio)
        {
            StartCoroutine(audioFeedback.GetComponent<AudioFeedback>().IntroOrCalib("intro"));
            firstTimeAudio = false;
        }
        if (isCenetered)
        {
            //count = 0;
            quad.gameObject.GetComponent<FolloCamera>().enabled = true;
            if (count < 1.0f)
            {
                count += Time.deltaTime;
            }
            else
            {
                isCenetered = false;

                quad.gameObject.GetComponent<FolloCamera>().enabled = false;
            }
        }
        if (Fist(Handedness.Right))
        {
            first_timer_PSM1_off += Time.deltaTime;
        }
        if (first_timer_PSM1_off > 3f)
        {
            PSM1.teleop = false;
            StartCoroutine(audioFeedback.GetComponent<AudioFeedback>().PSMTeleop("exit"));
            first_timer_PSM1_off = 0f;
            ONPSM1.GetComponent<MeshRenderer>().enabled = false;
            OFFPSM1.GetComponent<MeshRenderer>().enabled = true;
        }
        if (Fist(Handedness.Left))
        {
            first_timer_PSM2_off += Time.deltaTime;
        }
        if (first_timer_PSM2_off > 3f)
        {
            PSM2.teleop = false;
            StartCoroutine(audioFeedback.GetComponent<AudioFeedback>().PSMTeleop("exit"));
            first_timer_PSM2_off = 0f;
            ONPSM2.GetComponent<MeshRenderer>().enabled = false;
            OFFPSM2.GetComponent<MeshRenderer>().enabled = true;
        }
        if (CalibrationScript.calib_completed && HandTracking.pinch_dist_PSM1 < CalibrationScript.calibrated_pinch + 0.01)
        {
            first_timer_PSM1_on += Time.deltaTime;
        }
        if (first_timer_PSM1_on > 3f)
        {
            PSM1.teleop = true;
            first_timer_PSM1_on = 0f;
            ONPSM1.GetComponent<MeshRenderer>().enabled = true;
            OFFPSM1.GetComponent<MeshRenderer>().enabled = false;
            if (firstTimePSM1)
            {
                firstTimePSM1 = false;
                StartCoroutine(audioFeedback.GetComponent<AudioFeedback>().PSMTeleop("enter"));
            }
        }

        if (CalibrationScript.calib_completed && HandTracking.pinch_dist_PSM2 < CalibrationScript.calibrated_pinch + 0.01)
        {
            first_timer_PSM2_on += Time.deltaTime;
        }
        if (first_timer_PSM2_on > 3f)
        {
            PSM2.teleop = true;
            first_timer_PSM2_on = 0f;
            ONPSM2.GetComponent<MeshRenderer>().enabled = true;
            OFFPSM2.GetComponent<MeshRenderer>().enabled = false;
            if (firstTimePSM2)
            {
                firstTimePSM2 = false;
                StartCoroutine(audioFeedback.GetComponent<AudioFeedback>().PSMTeleop("enter"));
            }
        }
        if (MovecameraLikeConsole.isOpen)
        {
            PSM1.teleop = false;
            PSM2.teleop = false;
            ONPSM2.GetComponent<MeshRenderer>().enabled = false;
            OFFPSM2.GetComponent<MeshRenderer>().enabled = true;
            ONPSM1.GetComponent<MeshRenderer>().enabled = false;
            OFFPSM1.GetComponent<MeshRenderer>().enabled = true;
            first_timer_PSM2_on = 0;
            first_timer_PSM1_on = 0;
        }
        if (!CalibrationScript.calib_completed)
        {
            first_timer_PSM2_on = 0;
            first_timer_PSM1_on = 0;
        }
        if (PrecisionHandTracking.notPreciseHandtracking && CalibrationScript.calib_completed && firstTimeBadHandTracking)
        {
            if (firstTimeBadHandTracking)
            {
                firstTimeBadHandTracking = false;
                StartCoroutine(audioFeedback.GetComponent<AudioFeedback>().BadTracking("enter"));
            }
            quad.transform.Find("QuadBgLeft").GetComponent<MeshRenderer>().material.color = Color.magenta;
            quad.transform.Find("QuadBgRight").GetComponent<MeshRenderer>().material.color = Color.magenta;
            PSM1.teleop = false;
            PSM2.teleop = false;
            badHandTracking.GetComponent<MeshRenderer>().enabled = true;
            ONPSM2.GetComponent<MeshRenderer>().enabled = false;
            OFFPSM2.GetComponent<MeshRenderer>().enabled = true;
            ONPSM1.GetComponent<MeshRenderer>().enabled = false;
            OFFPSM1.GetComponent<MeshRenderer>().enabled = true;
            badhandTrackingCount+=Time.deltaTime;
            if (badhandTrackingCount > 9.0f)
            {
                firstTimeBadHandTracking = true;
                badhandTrackingCount = 0;
                PSM1.teleop = true;
                PSM2.teleop = true;
                ONPSM2.GetComponent<MeshRenderer>().enabled = true;
                OFFPSM2.GetComponent<MeshRenderer>().enabled = false;
                ONPSM1.GetComponent<MeshRenderer>().enabled = true;
                OFFPSM1.GetComponent<MeshRenderer>().enabled = false;
                badHandTracking.GetComponent<MeshRenderer>().enabled = false;
                PrecisionHandTracking.notPreciseHandtracking = false;
            }
        }
    }
    private bool Fist(Handedness hand)
    {
        /*Grab gesture*/
        bool pinky_curl = HandPoseUtils.PinkyFingerCurl(hand) > 0.7;
        bool ring_curl = HandPoseUtils.RingFingerCurl(hand) > 0.7;
        bool middle_curl = HandPoseUtils.MiddleFingerCurl(hand) > 0.7;
        bool index_curl = HandPoseUtils.IndexFingerCurl(hand) > 0.5;

        return pinky_curl && ring_curl && middle_curl && index_curl;//&&thumb_curl;
    }
    private bool HandOpen(Handedness hand)
    {
        // when hand not in view Curl return 0 -> have min threshold (as well as the max threshold)
        bool pinky_curl = HandPoseUtils.PinkyFingerCurl(hand) < 0.07 && HandPoseUtils.PinkyFingerCurl(hand) > 0.0005;
        bool ring_curl = HandPoseUtils.RingFingerCurl(hand) < 0.07 && HandPoseUtils.RingFingerCurl(hand) > 0.0005;
        bool middle_curl = HandPoseUtils.MiddleFingerCurl(hand) < 0.07 && HandPoseUtils.MiddleFingerCurl(hand) > 0.0005;
        bool index_curl = HandPoseUtils.IndexFingerCurl(hand) < 0.07 && HandPoseUtils.IndexFingerCurl(hand) > 0.0005;
        return pinky_curl && ring_curl && middle_curl && index_curl/*&&thumb_curl*/;
    }
    private bool StartHandTrackingTeleop(Handedness hand)
    {
        {

            Vector3 thumbTip = HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbTip, Handedness.Right, out var thumbPose) ? thumbPose.Position : Vector3.zero;
            Vector3 indexTip = HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, Handedness.Right, out var indexPose) ? indexPose.Position : Vector3.zero;
            Vector3 middleTip = HandJointUtils.TryGetJointPose(TrackedHandJoint.MiddleTip, Handedness.Right, out var middlePose) ? middlePose.Position : Vector3.zero;

            float thumbIndexDist = Vector3.Distance(thumbTip, indexTip);
            float thumbMiddleDist = Vector3.Distance(thumbTip, middleTip);
            float pinchThreshold = 0.02f;

            // check for pinch
            bool isPinching;
            isPinching = (thumbIndexDist < pinchThreshold || thumbMiddleDist < pinchThreshold);
            return isPinching;
        }
    }
    public void StartHandTrack()
    {
        /*PSM1*/
        GameObject PSM1_HandTrack = new GameObject("PSM1 HandTrack");
        PSM1_HandTrack.transform.parent = this.transform;
        PSM1 = PSM1_HandTrack.AddComponent<HandTracking>();
        PSM1.teleop = false;
        PSM1.sphere_marker = sphere_marker;
        PSM1.axis = axis;
        PSM1.scale = scale;
        PSM1.UDP = UDP;
        PSM1.hololens = hololens;
        PSM1.teleop_on = ONPSM1;
        PSM1.teleop_off = OFFPSM1;
        PSM1.quad = quad;
        PSM1.audioFeedback = audioFeedback;
        PSM1.SetPSM("PSM1");
        /*PSM2*/
        GameObject PSM2_HandTrack = new GameObject("PSM2 HandTrack");
        PSM2_HandTrack.transform.parent = this.transform;
        PSM2 = PSM2_HandTrack.AddComponent<HandTracking>();
        PSM2.teleop = false;
        PSM2.sphere_marker = sphere_marker;
        PSM2.axis = axis;
        PSM2.scale = scale;
        PSM2.UDP = UDP;
        PSM2.hololens = hololens;
        PSM2.teleop_on = ONPSM2;
        PSM2.teleop_off = OFFPSM2;
        PSM2.quad = quad;
        PSM2.audioFeedback = audioFeedback;
        PSM2.SetPSM("PSM2");
    }
    public void blockFollowCamera()
    {
        //calibButton.SetActive(true);
        quad.gameObject.GetComponent<FolloCamera>().enabled = false;
    }
    public void ResetFollowCamera()
    {
        //calibButton.SetActive(false);
        quad.gameObject.GetComponent<FolloCamera>().enabled = true;
    }
    public void centerCamera()
    {
        isCenetered = true;
        count = 0;
    }
}
