using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FolloCamera : MonoBehaviour
{
    public GameObject Marker;
    public Vector3 relativeOffset = new Vector3(0f, -0.07f, 0.7f);
    Vector3 relativePosition;
    Quaternion relativeRotation;
    MotionFilter quardFilter;
    MotionFilter quardFilterRotation;
    // Start is called before the first frame update
    void Start()
    {
        /*
        this.transform.position = Marker.transform.position;
        this.transform.rotation = Marker.transform.rotation;
        quardFilter = new MotionFilter();
        quardFilter.smoothingFactor = 0.5f;
        quardFilterRotation = new MotionFilter();
        quardFilterRotation.smoothingFactor = 1f;
        */
        quardFilter = new MotionFilter();
        quardFilter.smoothingFactor = 0.5f;
        quardFilterRotation = new MotionFilter();
        quardFilterRotation.smoothingFactor = 1f;
        UpdatePositionAndRotation();
    }

    // Update is called once per frame
    public void FollowingCamera()
    {
        //this.transform.position = quardFilter.UpdateEMA(Marker.transform.position);
        //this.transform.rotation = Marker.transform.rotation;
        /*
        if (MovecameraLikeConsole.isOpen)
        {
            this.transform.position = quardFilter.UpdateEMA(Marker.transform.position) - new Vector3(0f, 0f, 0.4f);
            this.transform.rotation = Marker.transform.rotation;
        }
        else
        {
            this.transform.position = quardFilter.UpdateEMA(Marker.transform.position);
            this.transform.rotation = Marker.transform.rotation;
        }
        */
        UpdatePositionAndRotation();
    }


    void Update()
    {
        /*
        this.transform.position = quardFilter.UpdateEMA(Marker.transform.position);
        this.transform.rotation = Marker.transform.rotation;
        */
        UpdatePositionAndRotation();
    }
    private void UpdatePositionAndRotation()
    {
        Transform cameraTransform = Camera.main.transform;

        // Calcola la posizione in world space partendo da un offset locale rispetto alla camera
        Vector3 worldTargetPosition = cameraTransform.TransformPoint(relativeOffset);

        if (MovecameraLikeConsole.isOpen)
        {
            //worldTargetPosition -= cameraTransform.forward * 0.4f;
        }

        // Smoothing
        this.transform.position = quardFilter.UpdateEMA(worldTargetPosition);

        // Rotation
        this.transform.rotation = cameraTransform.rotation;
    }
}
