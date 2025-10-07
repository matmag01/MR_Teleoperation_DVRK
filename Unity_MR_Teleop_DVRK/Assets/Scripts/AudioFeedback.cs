using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioFeedback : MonoBehaviour
{
    public AudioClip cameraTeleopClip; // Reference to the teleoperation start voice recording
    public AudioClip enterPSMTeleop;
    public AudioClip exitPSMTeleop;
    public AudioClip right;
    public AudioClip left;
    public AudioClip calib;
    public AudioClip intro;
    public AudioClip badHandTracking;
    public AudioClip cameraRotation;
    public AudioClip cameraTranslation;
    [HideInInspector]
    public bool isOutofView;
    [HideInInspector]
    public bool isCorrosing;
    bool audioOn;
    //public AudioClip teleoperationEndClip; // Reference to the teleoperation end voice recording

    private AudioSource[] audioSources; // Instance of the AudioSource component

    private void Start()
    {
        audioSources = GetComponents<AudioSource>(); // Initialize the AudioSource component
        audioOn = false;
        isOutofView = false;
        isCorrosing = false;
    }
    public void StartAudio()
    {
        audioOn = true;
        StartCoroutine(TeleoperationStarted());
    }

    public void StopAudio()
    {
        audioOn = false;
        audioSources[0].Stop();
    }
    IEnumerator TeleoperationStarted()
    {
        audioSources[0].clip = cameraTeleopClip; // Set the voice recording clip to play
        audioSources[0].volume = 0.35f;
        audioSources[0].Play(); // Start playing the voice recording
        float duration = audioSources[0].clip.length;
        yield return new WaitForSeconds(duration);
    }

    public IEnumerator CameraModality(string act)
    {
        audioSources[0].Stop();
        if (act == "rotate")
        {
            audioSources[0].clip = cameraRotation;
        }
        else if (act == "translate")
        {
            audioSources[0].clip = cameraTranslation;
        }
        audioSources[0].volume = 0.35f;
        audioSources[0].Play(); // Start playing the voice recording
        float duration = audioSources[0].clip.length;
        yield return new WaitForSeconds(duration * 1.1f);
    }

    public IEnumerator LeftRightTooFar(string act)
    {
        audioSources[2].Stop();
        if (act == "right")
        {
            audioSources[2].clip = right;
        }
        else if (act == "left")
        {
            audioSources[2].clip = left;
        }
        audioSources[2].volume = 0.35f;
        audioSources[2].Play(); // Start playing the voice recording
        float duration = audioSources[2].clip.length;
        yield return new WaitForSeconds(duration * 1.1f);
    }
    public IEnumerator IntroOrCalib(string act)
    {
        audioSources[2].Stop();
        if (act == "intro")
        {
            audioSources[2].clip = intro;
        }
        else if (act == "calib")
        {
            audioSources[2].clip = calib;
        }
        audioSources[2].volume = 0.60f;
        audioSources[2].Play(); // Start playing the voice recording
        float duration = audioSources[2].clip.length;
        yield return new WaitForSeconds(duration * 1.1f);
        
    }

    public IEnumerator PSMTeleop(string act)
    {
        audioSources[2].Stop();
        if (act == "enter")
        {
            audioSources[2].clip = enterPSMTeleop;
        }
        else if (act == "exit")
        {
            audioSources[2].clip = exitPSMTeleop;
        }

        audioSources[2].volume = 100;
        audioSources[2].Play(); // Start playing the voice recording
        float duration = audioSources[2].clip.length;
        yield return new WaitForSeconds(duration * 1.1f);
    }
    public IEnumerator BadTracking(string act)
    {
        audioSources[2].Stop();
        if (act == "enter")
        {
            audioSources[2].clip = badHandTracking;
        }
        audioSources[2].volume = 100;
        audioSources[2].Play(); // Start playing the voice recording
        float duration = audioSources[2].clip.length;
        yield return new WaitForSeconds(duration * 1.1f);
    }
}
