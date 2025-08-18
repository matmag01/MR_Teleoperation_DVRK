using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine.UIElements;

public class ArduinoConnection : MonoBehaviour
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
        
        if (HandTracking.arduinoPSM1)
        {
            serial.Write("1");
        }
        else
        {
            serial.Write("0");
        }
    }
    void OnApplicationQuit()
    {
        if (serial != null && serial.IsOpen)
        {
            serial.Write("STOP\n");
            serial.Close();
        }
    }
}
