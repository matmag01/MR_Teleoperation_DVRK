using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class Video: MonoBehaviour
{
    // public variables
    public GameObject quad;
    public float contrast = 1.25f;
    public bool visualReg = false;
    public float gripperLength;
    public bool drawGrippers = true;

    // private variables
    static public Texture2D tex2d_stereo;
    public int width = 1300 * 2;
    public int height = 1024;

    // TCP variables
    private TcpClient client;
    private NetworkStream stream;
    private Thread receiveThread;
    private byte[] receivedData;
    private bool newDataReady = false;
    public string serverIP = "10.162.34.82";
    public int serverPort = 5000;

    // Variables for gripper display
    Vector2Int endPSM1Left;
    Vector2Int endPSM1Right;
    Vector2Int endPSM2Left;
    Vector2Int endPSM2Right;
    private bool isRightImg;

    void Start()
    {
        tex2d_stereo = new Texture2D(width, height, TextureFormat.RGB24, false);

        // TCP connection to Python server
        receiveThread = new Thread(new ThreadStart(ReceiveVideo));
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    void OnDestroy()
    {
        if (client != null)
            client.Close();
        if (receiveThread != null && receiveThread.IsAlive)
            receiveThread.Abort();
    }

    private void ReceiveVideo()
    {
        try
        {
            client = new TcpClient(serverIP, serverPort);
            stream = client.GetStream();
            Debug.Log("Connected to Python server.");

            while (true)
            {
                // Read the size of the incoming image (16 bytes)
                byte[] sizeBytes = new byte[16];
                int bytesRead = 0;
                while (bytesRead < 16)
                {
                    int count = stream.Read(sizeBytes, bytesRead, 16 - bytesRead);
                    if (count == 0) throw new Exception("Connection lost.");
                    bytesRead += count;
                }

                string sizeString = Encoding.UTF8.GetString(sizeBytes).Trim();
                int size = int.Parse(sizeString);

                // Image data
                byte[] frameData = new byte[size];
                bytesRead = 0;
                while (bytesRead < size)
                {
                    // Check connection
                    int count = stream.Read(frameData, bytesRead, size - bytesRead);
                    if (count == 0) throw new Exception("Connection lost.");
                    bytesRead += count;
                }

                lock (this)
                {
                    receivedData = frameData;
                    newDataReady = true;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error in connection: " + e.Message);
        }
    }

    void Update()
    {
        if (newDataReady)
        {
            lock (this)
            {
                float startTime = Time.realtimeSinceStartup;
                // update texture
                tex2d_stereo.LoadImage(receivedData);
                Debug.Log($"Frame updated in {Time.realtimeSinceStartup - startTime} s.");
                // Axes projection and tip position
                List<Vector2Int> axesPSM2 = TipVisualNew.GetProjectedAxes(TipVisualNew.EE2_pos, TipVisualNew.EE2_quat, gripperLength);
                List<Vector2Int> axesPSM1 = TipVisualNew.GetProjectedAxes(TipVisualNew.EE1_pos, TipVisualNew.EE1_quat, gripperLength);
                List<Vector2Int> axesPSM2Right = TipVisualNew.GetProjectedAxesRigth(TipVisualNew.EE2_pos, TipVisualNew.EE2_quat, gripperLength);
                List<Vector2Int> axesPSM1Right = TipVisualNew.GetProjectedAxesRigth(TipVisualNew.EE1_pos, TipVisualNew.EE1_quat, gripperLength);

                endPSM1Left = axesPSM1[3];
                endPSM2Left = axesPSM2[3];
                endPSM1Right = axesPSM1Right[3] + new Vector2Int(width/2, 0);
                endPSM2Right = axesPSM2Right[3] + new Vector2Int(width/2, 0);

                // Extract pixel colors for manipulation (contrast, markers, grippers)
                Color[] pixels = tex2d_stereo.GetPixels();
                for (int i = 0; i < pixels.Length; i++)
                {
                    Color pixel = pixels[i];
                    pixel.r = (pixel.r - 0.5f) * contrast + 0.5f;
                    pixel.g = (pixel.g - 0.5f) * contrast + 0.5f;
                    pixel.b = (pixel.b - 0.5f) * contrast + 0.5f;
                    pixels[i] = pixel;
                }

                // Marker projection
                if (visualReg)
                {
                    DrawTipMarkerLite(pixels, tex2d_stereo, TipVisualNew.tipPositionPSM2);
                    DrawTipMarkerLite(pixels, tex2d_stereo, TipVisualNew.tipPositionPSM1);
                    DrawTipMarkerLite(pixels, tex2d_stereo, TipVisualNew.tipPositionPSM2Right + new Vector2Int(width/2, 0));
                    DrawTipMarkerLite(pixels, tex2d_stereo, TipVisualNew.tipPositionPSM1Right + new Vector2Int(width/2, 0));
                }
                
                if (drawGrippers)
                {
                    // Gripper projection
                    DrawGripper(pixels, tex2d_stereo, TipVisualNew.tipPositionPSM1, endPSM1Left, Color.blue, isRightImg = false, 0.7f);
                    DrawGripper(pixels, tex2d_stereo, TipVisualNew.tipPositionPSM1Right + new Vector2Int(width / 2, 0), endPSM1Right, Color.blue, isRightImg = true, 0.7f);
                    DrawGripper(pixels, tex2d_stereo, endPSM2Left, TipVisualNew.tipPositionPSM2, Color.blue, isRightImg = false, 0.7f);
                    DrawGripper(pixels, tex2d_stereo, endPSM2Right, TipVisualNew.tipPositionPSM2Right + new Vector2Int(width / 2, 0), Color.blue, isRightImg = true, 0.7f);
                }

                tex2d_stereo.SetPixels(pixels);
                tex2d_stereo.Apply();
                quad.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", tex2d_stereo);
                newDataReady = false;
            }
        }
    }
    void DrawTipMarkerLite(Color[] pixels, Texture2D tex, Vector2Int pixel, int radius = 20)
    {
        Color color = Color.red;
        int texWidth = tex.width;
        int texHeight = tex.height;

        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                if (dx * dx + dy * dy <= radius * radius) // CHeck within circle
                {
                    int x = pixel.x + dx;
                    int y = pixel.y + dy;

                    if (x >= 0 && x < texWidth && y >= 0 && y < texHeight)
                    {
                        int yy = texHeight - 1 - y; // Vertical flip to match unity texture
                        int idx = yy * texWidth + x;

                        // Blend with existing pixel color
                        pixels[idx] = color;
                    }
                }
            }
        }
    }


    void DrawGripper(Color[] pixels, Texture2D tex, Vector2Int p1, Vector2Int p2, Color color, bool isRight, float alpha = 0.5f)
    {
        int texHeight = tex.height;
        int texWidth = tex.width;
        int halfThickness = 25; // 50px total thickness
        int semiRadius = halfThickness;

        // Direction from p1 to p2
        Vector2 dir = p2 - p1;
        dir.Normalize();

        // Orthogonal direction
        Vector2 perp = new Vector2(-dir.y, dir.x) * halfThickness;

        // Rectangle vertices
        Vector2 a = p1 + perp;
        Vector2 b = p1 - perp;
        Vector2 c = p2 - perp;
        Vector2 d = p2 + perp;

        Vector2Int[] verts = { Vector2Int.RoundToInt(a), Vector2Int.RoundToInt(b),
                            Vector2Int.RoundToInt(c), Vector2Int.RoundToInt(d) };

        // Draw rectangle part
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
                    if (isRight && x < texWidth/2) pixelAlpha = 0f;
                    if (!isRight && x > texWidth/2) pixelAlpha = 0f;
                    int yy = texHeight - 1 - y;
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