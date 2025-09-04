
// script to communicate with dVRK using UDP


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using MathNet.Numerics.LinearAlgebra;

public class UDPComm : MonoBehaviour
{

    // for udp socket connection
    public static byte[] data_ECM;
    public static byte[] data_PSM1;
    public static byte[] data_PSM2;
    public static Socket socket_ECM;
    public static Socket socket_PSM1;
    public static Socket socket_PSM2;
    public static EndPoint remote_ECM;
    public static EndPoint remote_PSM1;
    public static EndPoint remote_PSM2;
    public static byte[] send_msg;
    public static string dVRK_msg_ECM;
    public static string dVRK_msg_PSM1;
    public static string dVRK_msg_PSM2;
    public static int read_msg_count = 0;
    public static Vector<float> ECM_Joints;
    public static Vector<float> PSM1_Joints;
    public static Vector<float> PSM2_Joints;
    public static float jaw_angle_PSM1;
    public static float jaw_angle_PSM2;
    public static Quaternion EE_quat_ECM;
    public static Quaternion EE_quat_PSM1;
    public static Quaternion EE_quat_PSM2;
    public static Vector3 EE_pos_ECM;
    public static Vector3 EE_pos_PSM1;
    public static Vector3 EE_pos_PSM2;

    bool ReaddVRKmsg = false;
    int readcounter = 0;
    int filename = 0;
    public GameObject hololens;

    bool pause = false;
    string file_name_dVRK;
    string file_name_holo;


    // Start is called before the first frame update
    void Start()
    {
        /*udp using socket --> Initialization*/

        /*ECM port*/
        Debug.Log("START UDP!");
        data_ECM = new byte[1024];
        IPEndPoint ip_ECM = new IPEndPoint(IPAddress.Any, 48053); // Always remember to check the port!!
        socket_ECM = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        socket_ECM.Bind(ip_ECM);
        IPEndPoint sender_ECM = new IPEndPoint(IPAddress.Any, 2);
        Debug.Log("UDP ECM CONFIGURED!");
        remote_ECM = (EndPoint)(sender_ECM);
        socket_ECM.BeginReceiveFrom(data_ECM, 0, data_ECM.Length, SocketFlags.None, ref remote_ECM, new AsyncCallback(ReceiveCallbackECM), socket_ECM);

        /* PSM1 port*/
        data_PSM1 = new byte[1024];
        IPEndPoint ip_PSM1 = new IPEndPoint(IPAddress.Any, 48051); // Always remember to check the port!!
        socket_PSM1 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket_PSM1.Bind(ip_PSM1);
        IPEndPoint sender_PSM1 = new IPEndPoint(IPAddress.Any, 1);
        remote_PSM1 = (EndPoint)(sender_PSM1);
        socket_PSM1.BeginReceiveFrom(data_PSM1, 0, data_PSM1.Length, SocketFlags.None, ref remote_PSM1, new AsyncCallback(ReceiveCallbackPSM1), socket_PSM1);


        
        data_PSM2 = new byte[1024];
        IPEndPoint ip_PSM2 = new IPEndPoint(IPAddress.Any, 48052); // Always remember to check the port!!
        socket_PSM2 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket_PSM2.Bind(ip_PSM2);
        IPEndPoint sender_PSM2 = new IPEndPoint(IPAddress.Any, 3);
        remote_PSM2 = (EndPoint)(sender_PSM2);
        socket_PSM2.BeginReceiveFrom(data_PSM2, 0, data_PSM2.Length, SocketFlags.None, ref remote_PSM2, new AsyncCallback(ReceiveCallbackPSM2), socket_PSM2);
        //Debug.Log("FINISH START!");
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    /*Callback function for the ECM port*/
    void ReceiveCallbackECM(IAsyncResult ar)
    {
        //Debug.Log("[ECM] Callback in!");
        // Get the socket and remote endpoint from the async result
        Socket socket_ECM = (Socket)ar.AsyncState;
        int bytesRead = socket_ECM.EndReceiveFrom(ar, ref remote_ECM);
        // Process the received data here
        dVRK_msg_ECM = Encoding.UTF8.GetString(data_ECM);
        //Debug.Log("ECM message: " + dVRK_msg_ECM);
        if (parser.StringMatch(dVRK_msg_ECM, "\"setpoint_cp\":"))
        {
            // extract rot and pos
            EE_quat_ECM = QuaternionFromMatrix(parser.GetMatrix4X4(dVRK_msg_ECM));
            Matrix4x4 temp = parser.GetMatrix4X4(dVRK_msg_ECM);
            //Debug.Log("dVRK rot: " + temp.rotation);
            EE_pos_ECM = parser.GetPos(dVRK_msg_ECM);
            //Debug.Log("EE_pos_ECM: " + EE_pos_ECM);
        }
        else if (parser.StringMatch(dVRK_msg_ECM, "\"setpoint_js\":"))
        {

            ECM_Joints = parser.GetECMJointPositions(dVRK_msg_ECM);
            Debug.Log("ECM_Joint positions are: " + ECM_Joints);
        }
        // Start receiving messages again
        socket_ECM.BeginReceiveFrom(data_ECM, 0, data_ECM.Length, SocketFlags.None, ref remote_ECM, new AsyncCallback(ReceiveCallbackECM), socket_ECM);
    }

    void ReceiveCallbackPSM1(IAsyncResult ar)
    {
        // Get the socket and remote endpoint from the async result
        Socket socket = (Socket)ar.AsyncState;
        //Debug.Log("IN");
        int bytesRead = socket.EndReceiveFrom(ar, ref remote_PSM1);
        /*Process the received data here*/
        dVRK_msg_PSM1 = Encoding.UTF8.GetString(data_PSM1);
        //Debug.Log("PSM1 message: " + dVRK_msg_PSM1);

        /*
        if (parser.StringMatch(dVRK_msg_PSM1, "\"Valid\":false")) //probably restarted "Valid":false
        {
            Debug.Log("Restart PSM1");
            HandTrack.new_EE_pos_PSM1 = Vector3.zero;
        }
        */

        // If jaw angle (joint)
        if (parser.StringMatch(dVRK_msg_PSM1, "\"jaw/setpoint_js\":"))
        {
            // extract jaw angle
            jaw_angle_PSM1 = parser.GetJawAngle(dVRK_msg_PSM1);
            //Debug.Log("jaw angle psm1: " + jaw_angle_PSM1);
        }
        // if pose message
        else if (parser.StringMatch(dVRK_msg_PSM1, "\"setpoint_cp\":"))
        {
            // extract rot and pos
            EE_quat_PSM1 = QuaternionFromMatrix(parser.GetMatrix4X4(dVRK_msg_PSM1));
            Matrix4x4 temp = parser.GetMatrix4X4(dVRK_msg_PSM1);
            //Debug.Log("dVRK rot: " + temp.rotation);
            EE_pos_PSM1 = parser.GetPos(dVRK_msg_PSM1);
            Debug.Log("EE_pos_PSM1: " + EE_pos_PSM1);
        }
        else if (parser.StringMatch(dVRK_msg_PSM1, "\"setpoint_js\":"))
        {
            PSM1_Joints = parser.GetECMJointPositions(dVRK_msg_PSM1);
            //Debug.Log("ECM_Joint positions are: " + ECM_Joints);
        }
        // Start receiving messages again
        socket.BeginReceiveFrom(data_PSM1, 0, data_PSM1.Length, SocketFlags.None, ref remote_PSM1, new AsyncCallback(ReceiveCallbackPSM1), socket);
    }

    void ReceiveCallbackPSM2(IAsyncResult ar)
    {
        // Get the socket and remote endpoint from the async result
        //Debug.Log("IN");
        Socket socket = (Socket)ar.AsyncState;
        int bytesRead = socket.EndReceiveFrom(ar, ref remote_PSM2);
        /*Process the received data here*/
        dVRK_msg_PSM2 = Encoding.UTF8.GetString(data_PSM2);
        //Debug.Log("PSM2 message: " + dVRK_msg_PSM2);

        /*
        if (parser.StringMatch(dVRK_msg_PSM2, "\"Valid\":false")) //probably restarted "Valid":false
        {
            Debug.Log("Restart PSM2");
            HandTrack.new_EE_pos_PSM2 = Vector3.zero;

        }
        */

        // If Jaw angle
        if (parser.StringMatch(dVRK_msg_PSM2, "\"jaw/setpoint_js\":"))
        {
            // extract jaw angle
            jaw_angle_PSM2 = parser.GetJawAngle(dVRK_msg_PSM2);
            //Debug.Log("dVRK jawangle: " + jaw_angle_PSM2);
        }
        // if pose message
        else if (parser.StringMatch(dVRK_msg_PSM2, "\"setpoint_cp\":"))
        {
            // extract rot and pos
            EE_quat_PSM2 = QuaternionFromMatrix(parser.GetMatrix4X4(dVRK_msg_PSM2));
            Matrix4x4 temp = parser.GetMatrix4X4(dVRK_msg_PSM2);
            //Debug.Log("dVRK rot: " + temp.rotation);
            EE_pos_PSM2 = parser.GetPos(dVRK_msg_PSM2);
            Debug.Log("psm2 POS: " + EE_pos_PSM2);
        }
        else if (parser.StringMatch(dVRK_msg_PSM2, "\"setpoint_js\":"))
        {
            PSM2_Joints = parser.GetECMJointPositions(dVRK_msg_PSM2);
            //Debug.Log("ECM_Joint positions are: " + ECM_Joints);
        }
        // Start receiving messages again
        socket.BeginReceiveFrom(data_PSM2, 0, data_PSM2.Length, SocketFlags.None, ref remote_PSM2, new AsyncCallback(ReceiveCallbackPSM2), socket);
    }

    public void UDPsendECM(string pose_message)
    {
        //byte[] send_msg;
        // send json strings to dVRK //
        send_msg = Encoding.UTF8.GetBytes(pose_message);
        //socket_ECM.SendTo(send_msg, remote_ECM);
        socket_ECM.BeginSendTo(send_msg, 0, send_msg.Length, SocketFlags.None, remote_ECM, new AsyncCallback(SendCallbackECM), socket_ECM);

    }

    public void UDPsend(string pose_message, string jaw_message, String PSM)
    {
        //byte[] send_msg;

        // send json strings to dVRK 
        if (PSM == "PSM1")
        {
            // Pose PSM1
            send_msg = Encoding.UTF8.GetBytes(pose_message);
            socket_PSM1.BeginSendTo(send_msg, 0, send_msg.Length, SocketFlags.None, remote_PSM1, new AsyncCallback(SendCallbackPSM1), socket_PSM1);
            // Jaw angle (PSM1)
            send_msg = Encoding.UTF8.GetBytes(jaw_message);
            socket_PSM1.BeginSendTo(send_msg, 0, send_msg.Length, SocketFlags.None, remote_PSM1, new AsyncCallback(SendCallbackPSM1), socket_PSM1);
        }
        else if (PSM == "PSM2")
        {
            // Pose PSM2
            send_msg = Encoding.UTF8.GetBytes(pose_message);
            socket_PSM2.BeginSendTo(send_msg, 0, send_msg.Length, SocketFlags.None, remote_PSM2, new AsyncCallback(SendCallbackPSM2), socket_PSM2);
            // Jaw Angle PSM2
            send_msg = Encoding.UTF8.GetBytes(jaw_message);
            socket_PSM2.BeginSendTo(send_msg, 0, send_msg.Length, SocketFlags.None, remote_PSM2, new AsyncCallback(SendCallbackPSM2), socket_PSM2);
        }

    }


    void SendCallbackECM(IAsyncResult ar)
    {

        try
        {
            // Retrieve the Socket object from the IAsyncResult
            Socket socket = (Socket)ar.AsyncState;

            // End the asynchronous send operation
            int bytesSent = socket.EndSendTo(ar);

            // Check if the send operation was successful
            if (bytesSent > 0)
            {
                Debug.Log("ECM message was sent");
            }
            else
            {
                Debug.LogWarning("Message was not sent");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error sending message: " + e.Message);
        }
    }

    void SendCallbackPSM1(IAsyncResult ar)
    {
        try
        {
            // Retrieve the Socket object from the IAsyncResult
            Socket socket = (Socket)ar.AsyncState;

            // End the asynchronous send operation
            int bytesSent = socket.EndSendTo(ar);

            // Check if the send operation was successful
            if (bytesSent > 0)
            {
                Debug.Log("PSM1 message was sent");
            }
            else
            {
                Debug.LogWarning("Message was not sent");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error sending message: " + e.Message);
        }

    }

    void SendCallbackPSM2(IAsyncResult ar)
    {
        try
        {
            // Retrieve the Socket object from the IAsyncResult
            Socket socket = (Socket)ar.AsyncState;

            // End the asynchronous send operation
            int bytesSent = socket.EndSendTo(ar);

            // Check if the send operation was successful
            if (bytesSent > 0)
            {
                Debug.Log("PSM2 message was sent");
            }
            else
            {
                Debug.LogWarning("Message was not sent");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error sending message: " + e.Message);
        }

    }

    // get quaternion from homogeneous matrix
    public static Quaternion QuaternionFromMatrix(Matrix4x4 m)
    {
        return Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));
    }

    public void ReaddVRK()
    {
        readcounter++;
        if (readcounter > 1)
        {
            ReaddVRKmsg = false;
            Debug.Log("stop Reading dVRK");
            readcounter = 0;

            //read.text = "Recording complete";

            //read.GetComponent<Renderer>().enabled = true;

        }
        else
        {

            ReaddVRKmsg = true;
            Debug.Log("Reading dVRK");
            readcounter++;
            filename++;
            //read.text = "Recording";

            //read.GetComponent<Renderer>().enabled = true;
        }
    }

    // sends pose and jaw messages to dVRK over UDP connection


    public void PauseRecord()
    {
        pause = !pause;
    }



}
