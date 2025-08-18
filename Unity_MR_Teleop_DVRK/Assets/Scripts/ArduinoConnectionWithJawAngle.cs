using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine.UIElements;

public class ArduinoConnectionWithJawAngle : MonoBehaviour
{
    SerialPort serial;
    private float angle;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Arduino");
        serial = new SerialPort("COM4", 9600);
        serial.Open();
        serial.DtrEnable = true;
    }

    // Update is called once per frame
    void Update()
    {
        angle = DistanceComputation();
        /*
        if (DistanceComputation() < 0.2f)
        {
            string message = $"{angle}";
            serial.Write(message);
        }
        else
        {
            serial.Write("0");
        }
        */
        string message = $"{angle}\n";
        serial.Write(message);
        Debug.Log("MESSAGE ARDUINO: " + message);
        
    }
    void OnApplicationQuit()
    {
        if (serial != null && serial.IsOpen)
        {
            serial.Write("STOP\n");
            serial.Close();
        }
    }
    float DistanceComputation()
    {
        float desAngle = 80.0f* Mathf.Deg2Rad;
        if (!HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbTip, Handedness.Right, out MixedRealityPose thumb))
        {
            desAngle = 80.0f* Mathf.Deg2Rad;
        }
        if (!HandJointUtils.TryGetJointPose(TrackedHandJoint.MiddleTip, Handedness.Right, out MixedRealityPose middle))
        {
            desAngle = 80.0f* Mathf.Deg2Rad;
        }
        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbTip, Handedness.Right, out thumb) &&
        HandJointUtils.TryGetJointPose(TrackedHandJoint.MiddleTip, Handedness.Right, out middle))
        {
            Vector3 thumbPos = thumb.Position;
            Vector3 middlePos = middle.Position;
            desAngle = JawAngle(thumbPos, middlePos);
        }
        return desAngle;
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
}
