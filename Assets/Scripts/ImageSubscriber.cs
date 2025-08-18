using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using System.Threading;
using System.Net;
using System.Net.Sockets;

public class ImageSubscriber : MonoBehaviour
{
    // public variables
    /*< display screen >*/
    public GameObject quad;
    public float contrast = 1.5f;
    // private variables
    /*< 2d texture >*/
    Texture2D tex2d;
    Texture2D tex2d_stereo;
    uint width = 1280 * 2;
    uint height = 1024;
    public int FrameSkip = 1;
    bool firstText = true;
    /*< ROS >*/
    ROSConnection m_ros;
    CompressedImageMsg img_msg_left = null;
    CompressedImageMsg img_msg_right = null;
    float count = 0;

    string type = "dvrk";
    //string video_topic_left = "/lai2/left/decklink/lai2_left/image_arrow/compressed";
    string video_topic_right = "/dvrk_cam/right/image_raw/compressed";
    string video_topic_left = "/dvrk_cam/left/image_raw/compressed";
    //string video_topic_right = "/lai2/right/decklink/lai2_right/image_raw/compressed";
    /*<Socket>*/
    public static byte[] data;
    public static Socket socket;
    public static EndPoint remote;
    private System.Diagnostics.Stopwatch stopwatch;
    int frameCount = 0;
    /*< Start >*/
    void Start()
    {
        // start the ROS connection
        m_ros = ROSConnection.GetOrCreateInstance();//10.162.34.80
        Debug.Log("--> Got ROS connection");
        // subscribe to video stream
        /*<Create new thread to process image subscribing>*/
        m_ros.Subscribe<CompressedImageMsg>(video_topic_left, onNewVideoLeft);
        m_ros.Subscribe<CompressedImageMsg>(video_topic_right, onNewVideoRight);


    }

    /*< Update >*/
    void Update()
    {
        if (frameCount % FrameSkip == 0)
        {
            if (img_msg_left != null && img_msg_right != null)
            {
                // load raw texture
                if (firstText)
                {
                    tex2d = new Texture2D((int)width, (int)height, TextureFormat.RGB24, false);
                    tex2d_stereo = new Texture2D(1280 * 2, 1024, TextureFormat.RGB24, false);
                    firstText = false;
                }
                tex2d.LoadImage(img_msg_left.data);
                Debug.Log("left loaded image data: " + tex2d.GetRawTextureData().Length);
                byte[] left_data = tex2d.GetRawTextureData();
                tex2d.LoadImage(img_msg_right.data);
                //Debug.Log("right loaded image data: " + tex2d.GetRawTextureData().Length);
                byte[] right_data = tex2d.GetRawTextureData();
                byte[] stereo_data = Concatenate(left_data, right_data);
                //Debug.Log("stereo data length: " + stereo_data.Length);
                //tex2d.LoadImage(stereo_data);
                tex2d_stereo.LoadRawTextureData(stereo_data);
                /*<Apply contrast adjustment>*/
                Color[] pixels = tex2d_stereo.GetPixels();
                for (int i = 0; i < pixels.Length; i++)
                {
                    Color pixel = pixels[i];
                    // Apply contrast adjustment to each color channel (R, G, B)
                    pixel.r = (pixel.r - 0.5f) * contrast + 0.5f;
                    pixel.g = (pixel.g - 0.5f) * contrast + 0.5f;
                    pixel.b = (pixel.b - 0.5f) * contrast + 0.5f;
                    pixels[i] = pixel;
                }
                tex2d_stereo.SetPixels(pixels);
                tex2d_stereo.Apply();
                quad.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", tex2d_stereo);

                //Debug.Log("Raw image data: " + tex2d_stereo.GetRawTextureData().Length);
                // // load image
                //tex2d.LoadImage(img_msg_left.data);
                //quad.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", tex2d);
                //Debug.Log("-->Enter image display");
            }
        }
        frameCount++;
        frameCount = (frameCount > 1000) ? 0 : frameCount;

    }


    // ---------------------------
    // ROS Callbacks
    // ---------------------------
    /*< onNewVideoLeft >*/
    public void onNewVideoLeft(CompressedImageMsg img)
    {
        DateTime currentTime = DateTime.Now;
        //Debug.Log("Machine Time: " + currentTime.ToString());
        img_msg_left = img;
        //Debug.Log("data length: " + img.data.Length);
    }

    /*< onNewVideoRight >*/
    public void onNewVideoRight(CompressedImageMsg img)
    {
        img_msg_right = img;

        //Debug.Log("Recieved right video: " + img.data.Length);
    }


    // ---------------------------
    // Utilities
    // ---------------------------
    /*< BgrToRgb >*/
    public void BgrToRgb(byte[] data)
    {
        for (int i = 0; i < data.Length; i += 3)
        {
            byte dummy = data[i];
            data[i] = data[i + 2];
            data[i + 2] = dummy;
        }
    }

    /*< ConcateImage >*/

    /*
    byte[] Concatenate(byte[] left, byte[] right)
    {
        byte[] stereo = new byte[left.Length + right.Length];
        for (int row = 0, stereo_row = 0; row < 1024; row++, stereo_row += 2)
        {

            // row 1
            Buffer.BlockCopy(left, row * 3 * 1280, stereo, stereo_row * 3 * 1280, 3 * 1280);
            Buffer.BlockCopy(right, row * 3 * 1280, stereo, (stereo_row + 1) * 3 * 1280, 3 * 1280);

        }

        return stereo;
    }
    */
    byte[] Concatenate(byte[] left, byte[] right)
{
    int width = 1280;
    int height = 1024;
    int bytesPerPixel = 3;

    byte[] stereo = new byte[width * 2 * height * bytesPerPixel];

    for (int row = 0; row < height; row++)
    {
        // sinistra
        Buffer.BlockCopy(left, row * width * bytesPerPixel,
                         stereo, row * width * 2 * bytesPerPixel,
                         width * bytesPerPixel);

        // destra
        Buffer.BlockCopy(right, row * width * bytesPerPixel,
                         stereo, (row * width * 2 + width) * bytesPerPixel,
                         width * bytesPerPixel);
    }

    return stereo;
}

 
}


