
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Input;
using System;
using MathNet.Numerics.LinearAlgebra;
using System.Linq;
using TMPro;
using System.Globalization;
public class MovecameraLikeConsole : MonoBehaviour
{

    public GameObject mainCamera;
    public GameObject cameraZaxis;
    public GameObject UDP;
    string jointsMessage;
    Vector3 cameraStartPosition;
    //Param for Endoscope
    Vector3 new_EE_pos;
    Quaternion new_EE_rot;
    Vector3 new_EE_pos_send;
    Matrix<float> new_EE_rot_send;
    Vector3 EE_start_pos;
    Quaternion EE_start_rot;
    Quaternion cameraLocalStartRotation;
    Vector3 projected_Vector;
    public float scale = 0.01f;
    public float insertion_step = 0.003f;
    public float radius = 0.12f;
    public float distance = 0.35f;
    //public TextMesh teleop_text;
    public GameObject teleop_on;
    public GameObject teleop_off;
    public GameObject insertion;
    public GameObject extraction;
    public GameObject ECM_status;
    public GameObject rotation;
    public GameObject quad;
    public GameObject moveECM;
    public GameObject txtQuadFixed;
    public GameObject txtCameraFixed;
    bool isFirstData = true;
    [HideInInspector]
    public static bool isOpen = false;
    bool isUpdating = true;
    bool isForward = false;
    bool isBackward = false;
    bool isRotate = false;
    bool isStartRotationSet = false;
    Vector3 lastQuadPosition;
    Quaternion lastQuadRotation;
    Vector3 lastQuadPosition1;
    Vector3 quadStartPosition;
    Quaternion quadStartRotation;
    Quaternion lastQuadRotation1;
    bool canMove = false;
    bool CanRotateCamera = false;
    bool toReset = false;
    float count = 0f;
    private float inactivityTimer = 0f;
    public float inactivityThreshold = 5f;
    private bool isActive = false;
    //public GameObject homeButton;
    MotionFilter motionFilter;
    public GameObject audioFeedback;
    bool isFirstTime;
    
    // Start is called before the first frame update
    void Start()
    {
        teleop_on.GetComponent<Renderer>().enabled = false;
        teleop_off.GetComponent<Renderer>().enabled = true;
        ECM_status.GetComponent<MeshRenderer>().enabled = true;
        extraction.GetComponent<MeshRenderer>().enabled = false;
        insertion.GetComponent<MeshRenderer>().enabled = false;
        rotation.GetComponent<MeshRenderer>().enabled = false;
        moveECM.GetComponent<MeshRenderer>().enabled = false;
        isFirstTime = true;
        if (isFirstTime)
        {
            isFirstTime = false;
            Vector<float> joints = UDPComm.ECM_Joints;
            float yaw = joints[0];
            float pitch = joints[1];
            float insertionjoint = joints[2];
            float roll = 0f;
            jointsMessage = "{\"move_jp\":{\"Goal\":[" +
                yaw.ToString(CultureInfo.InvariantCulture) + "," +
                pitch.ToString(CultureInfo.InvariantCulture) + "," +
                insertionjoint.ToString(CultureInfo.InvariantCulture) + "," +
                roll.ToString(CultureInfo.InvariantCulture) +
                "]}}";
            Debug.Log("Message sent: " + jointsMessage);
            UDP.GetComponent<UDPComm>().UDPsendECM(jointsMessage);    
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!isStartRotationSet)
            {
                return;
            }
        if (UDPComm.EE_pos_ECM != new Vector3(0.0f, 0.0f, 0.0f) && isFirstData)
        {
            EE_start_pos = UDPComm.EE_pos_ECM;
            EE_start_rot = UDPComm.EE_quat_ECM;
            isFirstData = false;
        }

        if (!isFirstData && isOpen)
        {
            Debug.Log("IN");
            if (isUpdating)
            {
                updatePose();
                isUpdating = false;
            }
            if (canMove && !toReset)
            {
                CheckWhich();
                if (!isForward && !isBackward && !isRotate && !CanRotateCamera)
                {
                    MoveCP();
                }
                else
                {
                    insertORextractCamera();

                }
            }
            if (canMove && toReset)
            {

                if (count < 0.3f)
                {
                    count += Time.deltaTime;
                }
                else
                {
                    quad.gameObject.GetComponent<FolloCamera>().enabled = false;
                    count = 0;
                    lastQuadPosition = quad.transform.localPosition;
                    lastQuadPosition1 = quad.transform.localPosition;
                    lastQuadRotation1 = quad.transform.localRotation;
                    canMoveCamera();
                    toReset = false;
                }
            }
            if (!isActive)
            {
                inactivityTimer += Time.deltaTime;
                if (inactivityTimer >= inactivityThreshold)
                {
                    ResetQuad();
                    inactivityTimer = 0f; 
                }
            }
            else
            {
                inactivityTimer = 0f;
                isActive = false; 
            }
        }
    }
    /*<Select wich one should be controled: tip or insertion axis>*/
    void CheckWhich()
    {
        Vector3 trans = quad.transform.localPosition - lastQuadPosition1;
        trans = quad.transform.InverseTransformVector(trans);
        //float rot = constrainAngle((quad.transform.localEulerAngles.z -quadStartRotation.eulerAngles.z) * Mathf.Deg2Rad);
        float rot = constrainAngle((mainCamera.transform.localEulerAngles.z - cameraLocalStartRotation.eulerAngles.z) * Mathf.Deg2Rad);
        //directionArrow.gameObject.SetActive(false);
        if (trans.z > distance && !CanRotateCamera)
        {
            /*
            if (!audioFeedback.GetComponent<AudioFeedback>().isCorrosing)
            {
                StartCoroutine(audioFeedback.GetComponent<AudioFeedback>().CorrssingBound());
            }
            forwardArrow.gameObject.SetActive(true);
            */
            isActive = true;
            HideText(ECM_status);
            ShowText(insertion);
            forwardCamera();
        }
        else if (trans.z < -distance && !CanRotateCamera)
        {
            /*
            if (!audioFeedback.GetComponent<AudioFeedback>().isCorrosing)
            {
                StartCoroutine(audioFeedback.GetComponent<AudioFeedback>().CorrssingBound());
            }
            backwardArrow.gameObject.SetActive(true);
            */
            isActive = true;
            HideText(ECM_status);
            ShowText(extraction);
            backwardCamera();
        }
        else if (Mathf.Abs(rot) > 10f * Mathf.Deg2Rad && projected_Vector.magnitude < radius && CanRotateCamera)
        {
            Debug.Log("rot: " + rot * Mathf.Rad2Deg);
            /*
            CWArrow.gameObject.SetActive(((Mathf.Sign(rot) < 0) ? true : false));
            CCWArrow.gameObject.SetActive(((Mathf.Sign(rot) > 0) ? true : false));
            cameraAngle.gameObject.SetActive(true);
            */
            isActive = true;
            HideText(ECM_status);
            ShowText(rotation);
            rotateCamera();
        }
        else
        {
            stopInsertion();
        }
            lastQuadPosition1 = quad.transform.localPosition;
            lastQuadRotation1 = quad.transform.localRotation;

    }
    private void insertORextractCamera()
    {

        Vector<float> joints = UDPComm.ECM_Joints;
        float yaw = joints[0];
        float pitch = joints[1];
        float insertion = joints[2];
        float roll = joints[3];
        if (isForward)
        {
            insertion += insertion_step;
            //jointsMessage = "{\"move_jp\":{\"Goal\":[" + yaw.ToString("R") + "," + pitch.ToString("R") + "," + insertion.ToString("R") + "," + roll.ToString("R") + "]}}";
            jointsMessage = "{\"move_jp\":{\"Goal\":[" +
                yaw.ToString(CultureInfo.InvariantCulture) + "," +
                pitch.ToString(CultureInfo.InvariantCulture) + "," +
                insertion.ToString(CultureInfo.InvariantCulture) + "," +
                roll.ToString(CultureInfo.InvariantCulture) +
                "]}}";
            UDP.GetComponent<UDPComm>().UDPsendECM(jointsMessage);
        }
        else if (isBackward)
        {
            insertion -= insertion_step;

            jointsMessage = "{\"move_jp\":{\"Goal\":[" +
                yaw.ToString(CultureInfo.InvariantCulture) + "," +
                pitch.ToString(CultureInfo.InvariantCulture) + "," +
                insertion.ToString(CultureInfo.InvariantCulture) + "," +
                roll.ToString(CultureInfo.InvariantCulture) +
                "]}}";
            //jointsMessage = "{\"move_jp\":{\"Goal\":[" + yaw.ToString("R") + "," + pitch.ToString("R") + "," + insertion.ToString("R") + "," + roll.ToString("R") + "]}}";
            UDP.GetComponent<UDPComm>().UDPsendECM(jointsMessage);
        }
        else if (isRotate)
        {
            //float rot = constrainAngle((quad.transform.localEulerAngles.z - quadStartRotation.eulerAngles.z) * Mathf.Deg2Rad);
            float rot = constrainAngle((mainCamera.transform.localEulerAngles.z - cameraLocalStartRotation.eulerAngles.z) * Mathf.Deg2Rad);
            if (Mathf.Abs(rot) < 15f * Mathf.Deg2Rad)
            {
                roll -= Mathf.Sign(rot) * 0.015f;
                Debug.Log("Roll: " + roll);
            }
            else
            {
                roll -= Mathf.Sign(rot) * 0.030f;
                Debug.Log("Roll: " + roll);
            }

            //cameraAngle.text = (roll * Mathf.Rad2Deg).ToString("F2") + "�";
            //jointsMessage = "{\"move_jp\":{\"Goal\":[" + yaw.ToString("R") + "," + pitch.ToString("R") + "," + insertion.ToString("R") + "," + roll.ToString("R") + "]}}";
            jointsMessage = "{\"move_jp\":{\"Goal\":[" +
                yaw.ToString(CultureInfo.InvariantCulture) + "," +
                pitch.ToString(CultureInfo.InvariantCulture) + "," +
                insertion.ToString(CultureInfo.InvariantCulture) + "," +
                roll.ToString(CultureInfo.InvariantCulture) +
                "]}}";
            Debug.Log("Message sent: " + jointsMessage);
            UDP.GetComponent<UDPComm>().UDPsendECM(jointsMessage);
        }
    }

        private void MoveCP()
        {
            Vector3 currentPosition = quad.transform.localPosition;
            Vector3 translation_hand = currentPosition - lastQuadPosition;
            translation_hand = quad.transform.InverseTransformVector(translation_hand); // Transform the movement from world to local space
            projected_Vector = new Vector3(translation_hand.x, translation_hand.y, 0f);
            if (Mathf.Abs(translation_hand.x) > 0.0005f || Mathf.Abs(translation_hand.y) > 0.0005f)
            {
                float trans_x = Vector3.Normalize(translation_hand).x;
                float trans_y = Vector3.Normalize(translation_hand).y;
                Vector3 translation_ECM = new Vector3(trans_x, 0f, trans_y);
                //Vector3 translation_ECM = new Vector3(translation_hand.x, 0f, translation_hand.y);
                translation_ECM = Quaternion.Euler(0f, UDPComm.ECM_Joints[3] * Mathf.Rad2Deg, 0f) * translation_ECM;//Compensate for roll angle
                new_EE_pos = UDPComm.EE_pos_ECM + scale * Vector3.Scale(translation_ECM, new Vector3(-1.0f, 1.0f, -1.0f)); // change from left hand convention to right
                new_EE_pos = motionFilter.UpdateEMA(new_EE_pos);

                isActive = true;
                HideText(ECM_status);
                ShowText(moveECM);

                //Convert pos to same orientation as dVRK i.e. y and z swapped
                new_EE_pos_send[0] = new_EE_pos[0];
                new_EE_pos_send[1] = new_EE_pos[2];
                new_EE_pos_send[2] = new_EE_pos[1];
                //get new rotation
                new_EE_rot = UDPComm.EE_quat_ECM;//
                new_EE_rot_send = Quat2Rot(Quaternion.Normalize(new_EE_rot));

                //Send message to dVRK (new EE pose)
                string pose_message = VectorFromMatrix(new_EE_pos_send, new_EE_rot_send);
                UDP.GetComponent<UDPComm>().UDPsendECM(pose_message);
            }
            else
            {
                HideText(moveECM);
                ShowText(ECM_status);
            }
            lastQuadPosition = currentPosition;
        }

    
    public void ResetQuad()
    {
        toReset = true;
        quad.gameObject.GetComponent<FolloCamera>().enabled = true;
        canMove = true;
        isUpdating = true;
        count = 0f;
    }

    public void canMoveCamera()
    {
        HideText(txtQuadFixed);
        ShowText(txtCameraFixed);
        quad.gameObject.GetComponent<FolloCamera>().enabled = false;
        stopInsertion();
        teleop_on.GetComponent<Renderer>().enabled = true;
        teleop_off.GetComponent<Renderer>().enabled = false;
        isUpdating = true;
        HideText(rotation);
        HideText(insertion);
        HideText(extraction);
        HideText(moveECM);
        ShowText(ECM_status);
        canMove = true;
        toReset = false;
        audioFeedback.GetComponent<AudioFeedback>().StartAudio();
        SetCameraStartRotation();
        lastQuadPosition = quad.transform.localPosition;
        lastQuadPosition1 = quad.transform.localPosition;
        lastQuadRotation1 = quad.transform.localRotation;
        isOpen = true;
        //homeButton.SetActive(true);
        motionFilter = new MotionFilter();
        motionFilter.smoothingFactor = 0.33f;
        quad.transform.Find("QuadBgLeft").GetComponent<MeshRenderer>().material.color = Color.cyan;
        quad.transform.Find("QuadBgRight").GetComponent<MeshRenderer>().material.color = Color.cyan;  
        BoxCollider[] colliders = quad.GetComponents<BoxCollider>();
        foreach (BoxCollider col in colliders)
        {
            col.enabled = true;
        }
    }
    float constrainAngle(float angle)
    {
        if (angle > Mathf.PI)
            angle = angle - 2 * Mathf.PI;
        else if (angle < -Mathf.PI)
        {
            angle = angle + 2 * Mathf.PI;
        }
        return angle;
    }

    public void openCamera()
    {
        teleop_on.GetComponent<Renderer>().enabled = true;
        teleop_off.GetComponent<Renderer>().enabled = false;
        isOpen = true;
        SetCameraStartRotation();
        quad.gameObject.GetComponent<FolloCamera>().enabled = true;
        Debug.Log("camera is open");
        canMove = false;
    }
    public void closeCamera()
    {
        ShowText(txtQuadFixed);
        HideText(txtCameraFixed);
        teleop_on.GetComponent<Renderer>().enabled = false;
        teleop_off.GetComponent<Renderer>().enabled = true;
        StartingScript.firstTimePSM1 = true;
        StartingScript.firstTimePSM2 = true;
        //quad.gameObject.GetComponent<FolloCamera>().enabled = true;
        stopInsertion();
        isOpen = false;
        canMove = false;
        isUpdating = true;
        HideText(rotation);
        HideText(insertion);
        HideText(extraction);
        ShowText(ECM_status);
        //homeButton.SetActive(false);
        motionFilter.ResetEMAValue();
        audioFeedback.GetComponent<AudioFeedback>().StopAudio();
        quad.transform.Find("QuadBgLeft").GetComponent<MeshRenderer>().material.color = Color.gray;
        quad.transform.Find("QuadBgRight").GetComponent<MeshRenderer>().material.color = Color.gray;
        BoxCollider[] colliders = quad.GetComponents<BoxCollider>();
        foreach (BoxCollider col in colliders)
        {
            col.enabled = false;
        }
        //directionArrow.gameObject.SetActive(false);
        //audioFeedback.GetComponent<AudioFeedback>().StopAudio();
    }

    void updatePose()
    {
        cameraStartPosition = mainCamera.transform.position;
        cameraLocalStartRotation = mainCamera.transform.localRotation;
        quadStartPosition = quad.transform.position;
        quadStartRotation = quad.transform.localRotation;
    }

    public void forwardCamera()
    {
        isForward = true;
        isBackward = false;
        isRotate = false;

    }

    public void backwardCamera()
    {
        isBackward = true;
        isForward = false;
        isRotate = false;
    }

    public void rotateCamera()
    {
        isRotate = true;
        isForward = false;
        isBackward = false;
    }
    public void stopInsertion()
    {
        isForward = false;
        isBackward = false;
        isRotate = false;
        /*
        forwardArrow.gameObject.SetActive(false);
        backwardArrow.gameObject.SetActive(false);
        CWArrow.gameObject.SetActive(false);
        CCWArrow.gameObject.SetActive(false);
        cameraAngle.gameObject.SetActive(false);
        */
        ShowText(ECM_status);
        HideText(rotation);
        HideText(insertion);
        HideText(extraction);
    }



    public void GoBackHome()
    {
        closeCamera();
        float yaw = 0;
        float pitch = 0;
        float insertion = 0;
        float roll = 0;

        //jointsMessage = "{\"move_jp\":{\"Goal\":[" + yaw.ToString("R") + "," + pitch.ToString("R") + "," + insertion.ToString("R") + "," + roll.ToString("R") + "]}}";
        jointsMessage = "{\"move_jp\":{\"Goal\":[" +
            yaw.ToString(CultureInfo.InvariantCulture) + "," +
            pitch.ToString(CultureInfo.InvariantCulture) + "," +
            insertion.ToString(CultureInfo.InvariantCulture) + "," +
            roll.ToString(CultureInfo.InvariantCulture) +
            "]}}";
        Debug.Log("Message sent: " + jointsMessage);
        UDP.GetComponent<UDPComm>().UDPsendECM(jointsMessage);
        SetCameraStartRotation();
    }
    public void CanRotate()
    {
        CanRotateCamera = true;
    }
    public void CannotRotate()
    {
        CanRotateCamera = false;
    }
    // convert quaternion to rotation matrix
    public static Matrix<float> Quat2Rot(Quaternion q)
    {
        Matrix<float> m = Matrix<float>.Build.Random(3, 3);

        m[0, 0] = 1 - 2 * Mathf.Pow(q.y, 2) - 2 * Mathf.Pow(q.z, 2);
        m[0, 1] = 2 * q.x * q.y - 2 * q.w * q.z;
        m[0, 2] = 2 * q.x * q.z + 2 * q.w * q.y;
        m[1, 0] = 2 * q.x * q.y + 2 * q.w * q.z;
        m[1, 1] = 1 - 2 * Mathf.Pow(q.x, 2) - 2 * Mathf.Pow(q.z, 2);
        m[1, 2] = 2 * q.y * q.z - 2 * q.w * q.x;
        m[2, 0] = 2 * q.x * q.z - 2 * q.w * q.y;
        m[2, 1] = 2 * q.y * q.z + 2 * q.w * q.x;
        m[2, 2] = 1 - 2 * Mathf.Pow(q.x, 2) - 2 * Mathf.Pow(q.y, 2);

        return m;
    }
    public static string VectorFromMatrix(Vector3 pos, Matrix<float> rot)
    {
        jsonFloat jsonfloat = new jsonFloat();

        // translation
        float[] translation = new float[3];
        for (int i = 0; i < 3; i++) // only get the first 3 rows of Transformation matrix
        {
            translation[i] = pos[i];
        }

        // rotation
        string rotation = string.Empty;
        rotation += "[";
        for (int i = 0; i < 3; i++)
        {
            if (i == 0)
            {
                rotation += "[";
            }
            else
            {
                rotation += ",[";
            }
            for (int j = 0; j < 3; j++)
            {
                //rotation += rot[i, j].ToString("R").ToLower();    // dVRK seems to only read lower case e for scientific notation (i.e. e-07 and not E-07)
                rotation += rot[i, j].ToString("R", CultureInfo.InvariantCulture).ToLower();
                if (j == 2)
                {
                    rotation += "]";
                }
                else
                {
                    rotation += ",";
                }
            }
        }

        rotation += "]";
        jsonfloat.Rotation = rotation;
        jsonfloat.Translation = translation;
        string mockString = JsonUtility.ToJson(jsonfloat);
        //begin funny string operation because jsonUtility doesn't support 2d array serialization
        string[] mockArray = mockString.Split('[');
        string first_part = mockArray[0].Substring(0, mockArray[0].Length - 1);
        string second_part = mockString.Substring(mockArray[0].Length, mockString.Length - mockArray[0].Length);
        string[] mockArray_2 = second_part.Split('\"');
        string second_part_2 = mockArray_2[0];
        string third_part = second_part.Substring(second_part_2.Length + 1, second_part.Length - (second_part_2.Length + 1));
        string final_string = first_part + second_part_2 + third_part;
        //final_string = "{\"servo_cp\": {\"Goal\": " + final_string + "}}";
        final_string = "{\"move_cp\": {\"Goal\": " + final_string + "}}";
        return final_string;
    }
    public void SetCameraStartRotation()
    {
        cameraLocalStartRotation = mainCamera.transform.localRotation;
        cameraStartPosition = mainCamera.transform.position;
        Debug.Log("Initial position of the camera: " + cameraLocalStartRotation.eulerAngles);
        isStartRotationSet = true;
        
    }
    public void ShowText(GameObject text)
    {
        text.GetComponent<MeshRenderer>().enabled = true;
    }
    public void HideText(GameObject text)
    {
        text.GetComponent<MeshRenderer>().enabled = false;
    }

}


