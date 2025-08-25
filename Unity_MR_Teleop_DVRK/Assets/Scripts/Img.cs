using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using System.Net;
using System.Net.Sockets;
using UnityEngine.UI;
using Unity.VisualScripting.FullSerializer;
using System.Linq;


public class Img : MonoBehaviour
{
    // public variables
    /*< display screen >*/
    public GameObject quad;
    public float contrast = 1.25f;
    //public AxisOverlayManager axisOverlayManager;
    // private variables
    /*< 2d texture >*/
    static public Texture2D tex2d_stereo;
    public int width = 1300*2;
    public int height = 1024;
    public bool visualReg = false;

    /*< ROS >*/
    ROSConnection m_ros;
    CompressedImageMsg img_msg = null;
    bool newFrameAvailable = false;
    string video_topic = "/concatenated_image/compressed";

    /* Variable for refresh rate computation*/

    private int countFPS = 0;
    private double sumFPS = 0f;
    private double lastFrameTime = 0f;
    private double averageFPS;
    private List<float> fpsSamples = new List<float>();
    double lastStamp = 0;
    double instantFPS;
    // Variables for gripper display
    Vector2Int endPSM1Left;
    Vector2Int endPSM1Right;
    Vector2Int endPSM2Left;
    Vector2Int endPSM2Right;
    private bool isRightImg;
    public float gripperLength;

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

            // Extract PSMs axes
            List<Vector2Int> axesPSM2 = TipVisualNew.GetProjectedAxes(TipVisualNew.EE2_pos, TipVisualNew.EE2_quat, gripperLength);
            List<Vector2Int> axesPSM1 = TipVisualNew.GetProjectedAxes(TipVisualNew.EE1_pos, TipVisualNew.EE1_quat, gripperLength);
            List<Vector2Int> axesPSM2Right = TipVisualNew.GetProjectedAxesRigth(TipVisualNew.EE2_pos, TipVisualNew.EE2_quat, gripperLength);
            List<Vector2Int> axesPSM1Right = TipVisualNew.GetProjectedAxesRigth(TipVisualNew.EE1_pos, TipVisualNew.EE1_quat, gripperLength);

            // Axes final position
            endPSM1Left = axesPSM1[3];
            endPSM2Left = axesPSM2[3];
            endPSM1Right = axesPSM1Right[3] +  new Vector2Int(1300, 0); // Added 1300 pixel to be displayed on the right lens
            endPSM2Right = axesPSM2Right[3] +  new Vector2Int(1300, 0); // Added 1300 pixel to be displayed on the right lens
            //DrawLine(tex2d_stereo, axesPSM1[0], axesPSM1[1], Color.red);    // X --> Red
            //DrawLine(tex2d_stereo, axesPSM1[0], axesPSM1[2], Color.green);  // Y --> Green
            //DrawLine(tex2d_stereo, axesPSM1[0], axesPSM1[3], Color.blue);   // Z --> blue
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
            
            if (visualReg)
            {
                DrawTipMarkerLite(pixels, tex2d_stereo, TipVisualNew.tipPositionPSM2);
                DrawTipMarkerLite(pixels, tex2d_stereo, TipVisualNew.tipPositionPSM1);
                DrawTipMarkerLite(pixels, tex2d_stereo, TipVisualNew.tipPositionPSM2Right + new Vector2Int(1300, 0));
                DrawTipMarkerLite(pixels, tex2d_stereo, TipVisualNew.tipPositionPSM1Right + new Vector2Int(1300, 0));
                //Debug.Log($"Origin: {axesPSM2[0]}, X: {axesPSM2[1]}, Y: {axesPSM2[2]}, Z: {axesPSM2[3]}");
            }
            
            // PSM1 --> left img
            DrawGripper(pixels, tex2d_stereo, TipVisualNew.tipPositionPSM1, endPSM1Left, Color.blue, isRightImg = false, 0.7f);
            // PSM1 --> right img
            DrawGripper(pixels, tex2d_stereo, TipVisualNew.tipPositionPSM1Right + new Vector2Int(1300, 0), endPSM1Right, Color.blue, isRightImg = true, 0.7f);
            // PSM2 --> left img
            DrawGripper(pixels, tex2d_stereo, endPSM2Left, TipVisualNew.tipPositionPSM2, Color.blue, isRightImg = false, 0.7f);
            // PSM2 --> right img
            DrawGripper(pixels, tex2d_stereo, endPSM2Right, TipVisualNew.tipPositionPSM2Right + new Vector2Int(1300, 0), Color.blue, isRightImg = true, 0.7f);
            
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
    void DrawTipMarkerLite(Color[] pixels, Texture2D tex, Vector2Int pixel, int radius = 20)
    {
        Color color = Color.red;
        int texWidth = tex.width;
        int texHeight = tex.height;

        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                if (dx * dx + dy * dy <= radius * radius) // check se siamo dentro il cerchio
                {
                    int x = pixel.x + dx;
                    int y = pixel.y + dy;

                    if (x >= 0 && x < texWidth && y >= 0 && y < texHeight)
                    {
                        int yy = texHeight - 1 - y; // flip verticale
                        int idx = yy * texWidth + x;

                        // blend oppure colore pieno
                        pixels[idx] = color;
                    }
                }
            }
        }
    }

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

    void DrawGripper(Color[] pixels, Texture2D tex, Vector2Int p1, Vector2Int p2, Color color, bool isRight, float alpha = 0.5f)
    {
        int texHeight = tex.height;
        int texWidth = tex.width;
        int halfLength = 25; // 50px total thickness

        // Tip Z-axis
        Vector2 dir = (p2 - p1);
        dir.Normalize();

        // Orthogonal direction
        Vector2 perp = new Vector2(-dir.y, dir.x) * halfLength;

        // Quad verteces
        Vector2 a = p1 + perp;
        Vector2 b = p1 - perp;
        Vector2 c = p2 - perp;
        Vector2 d = p2 + perp;

        Vector2Int[] verts = { Vector2Int.RoundToInt(a), Vector2Int.RoundToInt(b),
                            Vector2Int.RoundToInt(c), Vector2Int.RoundToInt(d) };

        // For each pixel --> Check if inside the BB
        int minX = Mathf.Max(0, verts.Min(v => v.x));
        int maxX = Mathf.Min(texWidth - 1, verts.Max(v => v.x));
        int minY = Mathf.Max(0, verts.Min(v => v.y));
        int maxY = Mathf.Min(texHeight - 1, verts.Max(v => v.y));

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                if (PointInQuad(new Vector2Int(x, y), verts))
                {
                    float pixelAlpha = alpha;

                    // If Right img and quad goes in left image
                    if (isRight && x < 1300)
                        pixelAlpha = 0f;
                    if (!isRight && x > 1300)
                        pixelAlpha = 0f;
                    int yy = texHeight - 1 - y; // Vertical flip to match unity texture
                    int idx = yy * texWidth + x;
                    pixels[idx] = Blend(pixels[idx], color, pixelAlpha);
                }
            }
        }
    }

    // Check if a point is inside a quad
    bool PointInQuad(Vector2Int p, Vector2Int[] verts)
    {
        int sign = 0;
        for (int i = 0; i < 4; i++)
        {
            Vector2Int v0 = verts[i];
            Vector2Int v1 = verts[(i + 1) % 4];
            int cross = (v1.x - v0.x) * (p.y - v0.y) - (v1.y - v0.y) * (p.x - v0.x);
            if (cross != 0)
            {
                if (sign == 0) sign = Math.Sign(cross);
                else if (Math.Sign(cross) != sign) return false;
            }
        }
        return true;
    }

    Color Blend(Color baseColor, Color overlay, float alpha)
    {
        return new Color(
            baseColor.r * (1 - alpha) + overlay.r * alpha,
            baseColor.g * (1 - alpha) + overlay.g * alpha,
            baseColor.b * (1 - alpha) + overlay.b * alpha,
            1f
        );
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
            // Change y axis (unity texture convention)
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
    
}
