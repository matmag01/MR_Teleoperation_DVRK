using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Input;

// Pinch gesture calibration
public class CalibrationScript : MonoBehaviour
{
    static int numSample = 40;
    float[] array_pinch = new float[numSample];
    public static bool calib_completed = false;
    public GameObject handTracking;
    public GameObject Calib_txt;
    public static float calibrated_pinch;
    public GameObject intro;
    public GameObject video;
    public GameObject audioFeedback;
    bool firstTime = true;
    public GameObject quad;
    void Start()
    {

    }
    void Update()
    {

    }
    public void Calibration()
    {
        calib_completed = false;
        StartCoroutine(audioFeedback.GetComponent<AudioFeedback>().IntroOrCalib("calib"));
        StartCoroutine(CalibrationRoutine());
    }

    private IEnumerator CalibrationRoutine()
    {
        intro.GetComponent<MeshRenderer>().enabled = false;
        Calib_txt.GetComponent<MeshRenderer>().enabled = true;

        // Wait 3 second before starting calibration
        yield return new WaitForSeconds(3f);

        int counter = 0;
        while (counter < numSample)
        {
            if (HandJointUtils.TryGetJointPose(TrackedHandJoint.Palm, Handedness.Right, out MixedRealityPose poseR) &&
                HandJointUtils.TryGetJointPose(TrackedHandJoint.Palm, Handedness.Left, out MixedRealityPose poseL))
            {
                array_pinch[counter] = HandTracking.pinch_dist;
                Debug.Log("Vector: " + array_pinch[counter]);
                counter++;
            }

            // delay (to avoid freezing)
            yield return null;
        }

        calib_completed = true;
        Calib_txt.GetComponent<MeshRenderer>().enabled = false;

        float sum = 0f;
        for (int i = 0; i < array_pinch.Length; i++)
        {
            sum += array_pinch[i];
        }

        calibrated_pinch = sum / array_pinch.Length;

        Debug.Log("Average Pinch: " + calibrated_pinch);
        //video.SetActive(true);
        quad.GetComponent<CustomPipelinePlayer>().enabled = true;

    }
}