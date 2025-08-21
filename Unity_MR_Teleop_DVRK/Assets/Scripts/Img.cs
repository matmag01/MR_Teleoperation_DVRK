using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using System.Net;
using System.Net.Sockets;


public class Img : MonoBehaviour
{
    // public variables
    /*< display screen >*/
    public GameObject quad;
    public float contrast = 1.25f;
    //public AxisOverlayManager axisOverlayManager;
    // private variables
    /*< 2d texture >*/
    Texture2D tex2d_stereo;
    public int width = 1300*2;
    public int height = 1024;
    public bool visualReg = false;

    /*< ROS >*/
    ROSConnection m_ros;
    CompressedImageMsg img_msg = null;
    bool newFrameAvailable = false;
    string video_topic = "/concatenated_image/compressed";

    /* Variable for refresh rate computation*/
    // Variabili per calcolo FPS video
    private int countFPS = 0;
    private double sumFPS = 0f;
    private double lastFrameTime = 0f;
    private double averageFPS;
    private List<float> fpsSamples = new List<float>();
    double lastStamp = 0;
    double instantFPS;


    // Start is called before the first frame update
    void Start()
    {
        m_ros = ROSConnection.GetOrCreateInstance();//10.162.34.80
        m_ros.Subscribe<CompressedImageMsg>(video_topic, VideoCallback);
        tex2d_stereo = new Texture2D(width, height, TextureFormat.RGB24, false);
        fpsSamples.Clear();
    }

    // Update is called once per frame
    void Update()
    {
        if (img_msg != null && newFrameAvailable)
        {
            newFrameAvailable = false;
            tex2d_stereo.LoadImage(img_msg.data);
            //DrawTipMarker(tex2d_stereo, TipReceiver.latestTip);

            if (visualReg)
            {
                DrawTipMarker(tex2d_stereo, TipVisualNew.tipPositionPSM2);
                DrawTipMarker(tex2d_stereo, TipVisualNew.tipPositionPSM1);
                DrawTipMarker(tex2d_stereo, TipVisualNew.tipPositionPSM2Right + new Vector2Int(1300, 0));
                DrawTipMarker(tex2d_stereo, TipVisualNew.tipPositionPSM1Right + new Vector2Int(1300, 0));
            }
            /*
            if (visualReg)
            {
                DrawTipMarker(tex2d_stereo, TipVisualNew.tipPositionPSM2);
                DrawTipMarker(tex2d_stereo, new Vector2Int(700, 400));
                //DrawTipMarker(tex2d_stereo, new Vector2Int(950, 500) + new Vector2Int(1300, 0));
                //DrawTipMarker(tex2d_stereo, new Vector2Int(650, 400) + new Vector2Int(1300, 0));
            }
            */
            //List<Vector2Int> axesPSM1 = TipVisualNew.GetProjectedAxes(TipVisualNew.EE1_pos, TipVisualNew.EE1_quat);
            //List<Vector2Int> axesPSM2 = TipVisualNew.GetProjectedAxes(TipVisualNew.EE2_pos, TipVisualNew.EE2_quat);

            // PSM1 AXIS
            //DrawLine(tex2d_stereo, axesPSM1[0], axesPSM1[1], Color.red);    // X --> Red
            //DrawLine(tex2d_stereo, axesPSM1[0], axesPSM1[2], Color.green);  // Y --> Green
            //DrawLine(tex2d_stereo, axesPSM1[0], axesPSM1[3], Color.blue);   // Z --> blue

            // PSM2 AXIS
            //DrawLine(tex2d_stereo, axesPSM2[0], axesPSM2[1], Color.red);    // X --> Red
            //DrawLine(tex2d_stereo, axesPSM2[0], axesPSM2[2], Color.green);  // Y --> Green
            //DrawLine(tex2d_stereo, axesPSM2[0], axesPSM2[3], Color.blue);   // Z --> blue
            //Debug.Log($"Origin: {axesPSM2[0]}, X: {axesPSM2[1]}, Y: {axesPSM2[2]}, Z: {axesPSM2[3]}");
            //DrawCylinderAsRectangle(tex2d_stereo, axesPSM2[0], axesPSM2[3], 600, 50, Color.magenta);
            
            
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
        }
    }

    void VideoCallback(CompressedImageMsg img)
    {
        //Debug.Log("Time stamp: " + DateTime.Now);
        img_msg = img;
        newFrameAvailable = true;

        /*
        double now = img.header.stamp.sec + img.header.stamp.nanosec*1e-9;
        Debug.Log("Timestamps " + now);
        if (lastFrameTime > 0f && now != lastFrameTime)
        {
            countFPS++;
            double delta = now - lastFrameTime;
            instantFPS = 1f / delta;
            sumFPS = sumFPS + instantFPS;
            Debug.Log($"FPS: {instantFPS:F2}");
        }
        if (countFPS == 100)
        {
            averageFPS = sumFPS / countFPS;
            sumFPS = 0;
            countFPS = 0;
            Debug.Log($"FPS Average: {averageFPS:F2}");
        }
        lastFrameTime = now;
        */
    }
    /*
    void VideoCallback(CompressedImageMsg img)
    {
        img_msg = img;
        newFrameAvailable = true;

        float now = Time.realtimeSinceStartup;
        if (lastFrameTime > 0f)
        {
            float delta = now - lastFrameTime;
            float instantFPS = 1f / delta;
            fpsSamples.Add(instantFPS);
            
            sumFPS += instantFPS;

            Debug.Log($"FPS: {instantFPS:F2}");
            countFPS++;

            if (countFPS == 100)
            {
                // Media
                averageFPS = sumFPS / countFPS;

                // Deviazione standard
                float sumSquaredDiffs = 0f;
                foreach (var f in fpsSamples)
                    sumSquaredDiffs += (f - averageFPS) * (f - averageFPS);

                float stdDev = Mathf.Sqrt(sumSquaredDiffs / countFPS);

                Debug.Log($"FPS Average: {averageFPS:F2}");
                Debug.Log($"FPS Std Dev: {stdDev:F2}");

                // Reset per il prossimo ciclo
                fpsSamples.Clear();
                sumFPS = 0f;
                countFPS = 0;
            }
        }
        lastFrameTime = now;
    }
    */
    void DrawTipMarker(Texture2D tex, Vector2Int pixel)
    {
        //if (pixel.x < 0 || pixel.y < 0 || pixel.x >= tex.width / 2 || pixel.y >= tex.height) return;

        int radius = 20;
        Color color = Color.red;

        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                int x = pixel.x + dx;
                int y = pixel.y + dy;

                if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
                {
                    if (dx * dx + dy * dy <= radius * radius)
                    {
                        tex.SetPixel(x, -y, color); // y-flip to match Unity texture orientation                        
                    }
                }
            }
        }
    }
    /*
    void DrawCylinder(Texture2D tex, Vector2Int p1, Vector2Int p2, Color col)
    {
        int dx = Mathf.Abs(p2.x - p1.x);
        int dy = Mathf.Abs(p2.y - p1.y);
        int sx = (p1.x < p2.x) ? 1 : -1;
        int sy = (p1.y < p2.y) ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            tex.SetPixel(p1.x, tex.height - p1.y, col); // Flip y-axis

            if (p1.x == p2.x && p1.y == p2.y) break;

            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; p1.x += sx; }
            if (e2 < dx) { err += dx; p1.y += sy; }
        }
    }
    void DrawCylinderAsRectangle(Texture2D tex, Vector2Int origin, Vector2Int zDir, float length, float width, Color col)
    {
        Vector2 zNorm = -((Vector2)zDir).normalized;
        Vector2 perp = new Vector2(-zNorm.y, zNorm.x); // Perpendicolare

        Vector2 p1 = origin + (perp * (width / 2f));
        Vector2 p2 = origin - (perp * (width / 2f));
        Vector2 p3 = p2 + (zNorm * length);
        Vector2 p4 = p1 + (zNorm * length);

        // Convert to int
        Vector2Int ip1 = Vector2Int.RoundToInt(p1);
        Vector2Int ip2 = Vector2Int.RoundToInt(p2);
        Vector2Int ip3 = Vector2Int.RoundToInt(p3);
        Vector2Int ip4 = Vector2Int.RoundToInt(p4);

        // Draw edges
        DrawCylinder(tex, ip1, ip2, col);
        DrawCylinder(tex, ip2, ip3, col);
        DrawCylinder(tex, ip3, ip4, col);
        DrawCylinder(tex, ip4, ip1, col);
    }

    void DrawLine(Texture2D tex, Vector2Int p1, Vector2Int p2, Color color)
    {
        int x0 = p1.x;
        int y0 = p1.y;
        int x1 = p2.x;
        int y1 = p2.y;

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);

        int sx = (x0 < x1) ? 1 : -1;
        int sy = (y0 < y1) ? 1 : -1;

        int err = dx - dy;

        while (true)
        {
            // Inverti y per adattarsi all'orientamento della texture di Unity
            if (x0 >= 0 && x0 < tex.width && y0 >= 0 && y0 < tex.height)
            {
                tex.SetPixel(x0, tex.height - 1 - y0, color);
            }

            if (x0 == x1 && y0 == y1) break;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }
    */
}
