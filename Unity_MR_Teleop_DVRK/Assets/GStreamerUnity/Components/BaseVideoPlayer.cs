using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public abstract class BaseVideoPlayer : DependencyRoot {

	private GstCustomTexture m_Texture = null;
	Material material;
	OffscreenProcessor _Processor;

	public Shader[] PostProcessors;

	OffscreenProcessor[] _postProcessors;

	public Texture VideoTexture;

	public bool ConvertToRGB=true;

	bool _disabledPause;

	public GstCustomTexture InternalTexture
	{
		get{return m_Texture;}
	}

    private void OnDisable()
    {
		if (m_Texture.IsPlaying)
		{
			m_Texture.Pause();
			_disabledPause = true;
        }
        else
        {
			_disabledPause = false;

		}
	}

    private void OnEnable()
    {
        if (_disabledPause)
		{
			m_Texture.Play();
		}
    }

    public delegate void Delg_OnFrameAvailable(BaseVideoPlayer src,Texture tex);
	public event Delg_OnFrameAvailable OnFrameAvailable;

	protected abstract string _GetPipeline ();

	// Use this for initialization
	protected override void Start()
	{

		_disabledPause = false;

		_Processor = new OffscreenProcessor();
		m_Texture = gameObject.GetComponent<GstCustomTexture>();

		material = gameObject.GetComponent<MeshRenderer>().material;
		// Check to make sure we have an instance.
		if (m_Texture == null)
		{
			DestroyImmediate(this);
		}

		m_Texture.Initialize();
		//		pipeline = "filesrc location=\""+VideoPath+"\" ! decodebin ! videoconvert ! video/x-raw,format=I420 ! appsink name=videoSink sync=true";
		//		pipeline = "filesrc location=~/Documents/Projects/BeyondAR/Equirectangular_projection_SW.jpg ! jpegdec ! videoconvert ! imagefreeze ! videoconvert ! imagefreeze ! videoconvert ! video/x-raw,format=I420 ! appsink name=videoSink sync=true";
		//		pipeline = "videotestsrc ! videoconvert ! video/x-raw,width=3280,height=2048,format=I420 ! appsink name=videoSink sync=true";
		m_Texture.SetPipeline(_GetPipeline());
		Debug.Log("Pipeline completa: " + _GetPipeline());
		
		m_Texture.OnFrameGrabbed += OnFrameGrabbed;
		_Processor.ShaderName = "Image/I420ToRGB";

		if (PostProcessors != null)
		{
			_postProcessors = new OffscreenProcessor[PostProcessors.Length];
			for (int i = 0; i < PostProcessors.Length; ++i)
			{
				_postProcessors[i] = new OffscreenProcessor();
				_postProcessors[i].ProcessingShader = PostProcessors[i];
			}
		}
		m_Texture.Play();
		Debug.Log("Starting Base");
		base.Start();
		Debug.Log("Started Base");
	}

	bool _newFrame=false;
	void OnFrameGrabbed(GstBaseTexture src,int index)
	{ 
		_newFrame = true;
	}
	/*
		void _processNewFrame()
		{
			_newFrame = false;
			if (m_Texture.PlayerTexture ().Length == 0)
				return;
			Debug.Log("Processing new frame");
			Texture tex=m_Texture.PlayerTexture () [0];

			if (ConvertToRGB) {
				if (m_Texture.PlayerTexture () [0].format == TextureFormat.Alpha8)
					VideoTexture = _Processor.ProcessTexture (tex);
				else
					VideoTexture = tex;

			} else {
				VideoTexture = tex;
			}

			if (_postProcessors != null) {
				foreach (var p in _postProcessors) {
					VideoTexture = p.ProcessTexture (VideoTexture);
				}
			}
			material.mainTexture = VideoTexture;

			if (OnFrameAvailable != null)
				OnFrameAvailable (this, VideoTexture);

		}
	*/

	public float LastFrameLatencyMs; // Dichiara questa variabile per visualizzare il risultato

void _processNewFrame()
{
    _newFrame = false;
    if (m_Texture.PlayerTexture().Length == 0)
        return;

    Texture tex = m_Texture.PlayerTexture()[0];

    // Cast 'tex' to 'Texture2D' to access the 'format' property
    if (ConvertToRGB && tex is Texture2D)
    {
        Texture2D tex2D = (Texture2D)tex;
        if (tex2D.format == TextureFormat.Alpha8)
        {
            VideoTexture = _Processor.ProcessTexture(tex);
        }
        else
        {
            VideoTexture = tex;
        }
    }
    else
    {
        VideoTexture = tex;
    }

    if (_postProcessors != null)
    {
        foreach (var p in _postProcessors)
        {
            VideoTexture = p.ProcessTexture(VideoTexture);
        }
    }

    material.mainTexture = VideoTexture;
	// === PUNTO DI MISURA B: Rendering completato sul thread principale ===
    // === PUNTO DI MISURA B: Rendering completato sul thread principale ===
    
    // 1. Ottieni il timestamp di fine nel thread principale
    long timeRenderedTicks = DateTime.UtcNow.Ticks; 
    
    // 2. Ottieni il timestamp di inizio dal thread secondario
    long timeCapturedTicks = m_Texture.FrameCapturedTicks;

    // 3. Calcola la differenza in tick
    long elapsedTicks = timeRenderedTicks - timeCapturedTicks;
    
    // 4. Converti i tick in millisecondi: 1 tick = 0.0001 ms (100 nanosecondi)
    double elapsedMilliseconds = elapsedTicks / 10000.0; // 10,000 tick per millisecondo
    
    // Assegna il valore alla variabile pubblica per la visualizzazione
    LastFrameLatencyMs = (float)elapsedMilliseconds;

    // Puoi anche visualizzare il valore nel Debug Log:
    UnityEngine.Debug.Log("Latenza totale (GStreamer -> Texture Unity): " + LastFrameLatencyMs.ToString("F3") + " ms");
    

    if (OnFrameAvailable != null)
			OnFrameAvailable(this, VideoTexture);
}
	// Update is called once per frame
	protected virtual void Update()
	{

		if (_newFrame)
			_processNewFrame();
	}
}
