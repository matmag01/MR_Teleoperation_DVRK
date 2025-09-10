using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;

[RequireComponent(typeof(GstCustomTexture))]
public class CustomPipelinePlayer : BaseVideoPlayer
{

	// Use this for initialization
	protected override string _GetPipeline()
	{
		//string P = pipeline + " ! video/x-raw,format=I420 ! videoconvert ! appsink name=videoSink emit-signals=true max-buffers=1 drop=true";
		//FUNZIONA :

		//string P = "udpsrc port=7000 ! application/x-rtp ! rtpjitterbuffer ! rtph264depay ! decodebin ! videoconvert ! video/x-raw,format=I420 ! appsink name=videoSink";

		// ER TOP:
		//string P = "udpsrc port=7000 ! application/x-rtp ! rtph264depay ! avdec_h264 ! deinterlace ! videoconvert ! video/x-raw,format=I420 ! appsink name=videoSink emit-signals=true max-buffers=1 drop=true"; 
		string P = "udpsrc port=7000 ! application/x-rtp ! rtph264depay ! avdec_h264 ! deinterlace ! videoflip method=vertical-flip ! videoconvert ! video/x-raw,format=RGB ! appsink name=videoSink emit-signals=true max-buffers=1 drop=true";

		return P;
	}

}