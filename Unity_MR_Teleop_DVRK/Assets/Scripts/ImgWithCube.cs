using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;


public class ImgWithCube : MonoBehaviour
{
    // public variables
    /*< display screen >*/
    public GameObject quad;
    public float contrast = 1.6f;

    // private variables
    /*< 2d texture >*/
    Texture2D tex2d_stereo;
    public int width = 1300*2;
    public int height = 1024;
    public int FrameSkip = 1;
    public bool visualReg = false;

    /*< ROS >*/
    ROSConnection m_ros;
    CompressedImageMsg img_msg = null;
    bool newFrameAvailable = false;
    string video_topic = "/concatenated_image/square_compressed";

    // Start is called before the first frame update
    void Start()
    {
        m_ros = ROSConnection.GetOrCreateInstance();//10.162.34.80
        m_ros.Subscribe<CompressedImageMsg>(video_topic, VideoCallback);
        tex2d_stereo = new Texture2D(width, height, TextureFormat.RGB24, false);
    }

    // Update is called once per frame
    void Update()
    {
        if (img_msg != null && newFrameAvailable)
        {
            newFrameAvailable = false;
            tex2d_stereo.LoadImage(img_msg.data);
            
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
    }
}
