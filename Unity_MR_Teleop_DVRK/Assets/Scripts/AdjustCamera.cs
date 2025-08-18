using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Input;
using System;
using MathNet.Numerics.LinearAlgebra;
using System.Linq;
using TMPro;
using System.Globalization;
using UnityEngine.AI;

public class AdjustCamera : MonoBehaviour
{

    private Vector3 lastQuadPosition;
    private bool isOneHanded = true; // oppure impostato dinamicamente da un tuo controller
    public GameObject mainCamera;
    public GameObject cameraZaxis;
    public GameObject UDP;
    public GameObject quad;
    string jointsMessage;
    private Quaternion lastQuadRotation;
    Vector3 cameraStartPosition;
    Quaternion quadLocalStartRotation;
    Vector3 quadStartPosition;
    //Param for Endoscope
    Vector3 EE_pos;        // EE pos from dVRK
    Quaternion EE_quat;    // EE rot from dVRK
    Vector3 new_EE_pos;
    Quaternion new_EE_rot;

    Quaternion quadStartRotation;
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
    private Vector3 originalQuadScale;
    bool isFirstData = true;
    [HideInInspector]
    public bool isOpen = false;
    bool isUpdating = true;
    bool isForward = false;
    bool isBackward = false;
    bool isRotate = false;
    bool isStartRotationSet = false;
    bool canRotate = false;
    bool startRotation = false;
    private float timer = 0f;
    private MotionFilter motionFilter;

    
    // Start is called before the first frame update
    void Start()
    {
        //cameraStartPosition = mainCamera.transform.position;
        //cameraLocalStartRotation = mainCamera.transform.localRotation;
        /*<Visual indicator>*/
        teleop_on.GetComponent<Renderer>().enabled = false;
        teleop_off.GetComponent<Renderer>().enabled = true;
        ECM_status.GetComponent<MeshRenderer>().enabled = true;
        extraction.GetComponent<MeshRenderer>().enabled = false;
        insertion.GetComponent<MeshRenderer>().enabled = false;
        rotation.GetComponent<MeshRenderer>().enabled = false;
        originalQuadScale = quad.transform.localScale;
        motionFilter = new MotionFilter();
        motionFilter.smoothingFactor = 0.85f;  
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
            
            if (isUpdating)
            {
                updatePose();
            }
            if (!canRotate)
            {
                CheckWhich();
                
                if (isForward || isBackward)
                {
                    Debug.Log("IN");
                    insertORextractCamera();
                    quadStartPosition = quad.transform.position;
                    quadLocalStartRotation = quad.transform.localRotation;

                }
            }
            if (canRotate)
            {
                if (!startRotation)
                {
                    timer += Time.deltaTime;
                    if (timer > 1.5f)
                    {
                        timer = 0;
                        startRotation = true;
                    }
                }
                else
                {
                    CameraRotation();
                }
            }         
        }
    }

    /*<Select wich one should be controled: tip or insertion axis>*/
    void CheckWhich()
    {
        Vector3 trans = mainCamera.transform.position - cameraStartPosition;
        trans = mainCamera.transform.InverseTransformVector(trans);
        float rot = constrainAngle((mainCamera.transform.localEulerAngles.z - cameraLocalStartRotation.eulerAngles.z) * Mathf.Deg2Rad);
        if (trans.z > distance)
        {
            /*
            if (!audioFeedback.GetComponent<AudioFeedback>().isCorrosing)
            {
                StartCoroutine(audioFeedback.GetComponent<AudioFeedback>().CorrssingBound());
            }
            */
            HideText(ECM_status);
            ShowText(insertion);
            forwardCamera();
        }
        else if (trans.z < -distance)
        {
            /*
            if (!audioFeedback.GetComponent<AudioFeedback>().isCorrosing)
            {
                StartCoroutine(audioFeedback.GetComponent<AudioFeedback>().CorrssingBound());
            }
            */
            HideText(ECM_status);
            ShowText(extraction);
            backwardCamera();
        }
        else
        {
            stopInsertion();
        }
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
            jointsMessage = "{\"move_jp\":{\"Goal\":[" +
                yaw.ToString(CultureInfo.InvariantCulture) + "," +
                pitch.ToString(CultureInfo.InvariantCulture) + "," +
                insertion.ToString(CultureInfo.InvariantCulture) + "," +
                roll.ToString(CultureInfo.InvariantCulture) +
                "]}}";
            Debug.Log("Message sent: " + jointsMessage);
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
            Debug.Log("Message sent: " + jointsMessage);
            UDP.GetComponent<UDPComm>().UDPsendECM(jointsMessage);
        }
    }

    private void CameraRotation()
    {

        //If the movement of head is out of a specified range then continuously move the camera only in x direction and y direction
        Quaternion R_start = quadStartRotation;
        Quaternion R_new = quad.transform.localRotation;
        Quaternion R_relative = Quaternion.Euler(R_new.eulerAngles - R_start.eulerAngles);
        Vector3 referenceZaxis = quad.transform.forward; 
        Vector3 rotatedLocalZaxis = R_relative * referenceZaxis;
        Vector3 translation_hand = rotatedLocalZaxis - referenceZaxis;
        projected_Vector = new Vector3(translation_hand.x, translation_hand.y, 0f);
        if (projected_Vector.magnitude > radius)
        {
            float trans_x = Vector3.Normalize(translation_hand).x;
            float trans_y = Vector3.Normalize(translation_hand).y;
            Vector3 translation_ECM = new Vector3(trans_x, 0f, trans_y);
            translation_ECM = Quaternion.Euler(0f, UDPComm.ECM_Joints[3] * Mathf.Rad2Deg, 0f) * translation_ECM;//Compensate for roll angle
            if (projected_Vector.magnitude > 0.3)
            {
                new_EE_pos = UDPComm.EE_pos_ECM + scale * Vector3.Scale(translation_ECM, new Vector3(-1.0f, 1.0f, -1.0f)); // change from left hand convention to right
            }

            /*
            //Show direction
            directionArrow.gameObject.SetActive(true);
            directionArrow.transform.localPosition = middle_point.transform.localPosition + new Vector3(translation_hand.x, translation_hand.y, 0f);
            Quaternion directionArrow_rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(translation_hand.y, translation_hand.x) * Mathf.Rad2Deg);
            directionArrow.transform.localRotation = directionArrow_rotation;
            */
            HideText(ECM_status);
            ShowText(rotation);

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
            //directionArrow.gameObject.SetActive(false);
            HideText(rotation);
            ShowText(ECM_status);
        }
    }
/*    
            private void CameraRotation()
            {
                Quaternion currentRotation = quad.transform.localRotation;
                Quaternion deltaRotation = currentRotation * Quaternion.Inverse(lastQuadRotation);

                // quad movement computation
                Vector3 deltaEuler = deltaRotation.eulerAngles;

                float deltaX = Mathf.DeltaAngle(0, deltaEuler.y); // Yaw
                float deltaY = Mathf.DeltaAngle(0, deltaEuler.x); // Pitch

                Vector3 translation_ECM = new Vector3(deltaX, 0f, deltaY).normalized;

                // Roll angle compensation
                //translation_ECM = Quaternion.Euler(0f, UDPComm.ECM_Joints[3] * Mathf.Rad2Deg, 0f) * translation_ECM;

                float movementMagnitude = new Vector2(deltaX, deltaY).magnitude;

                // Remove jitter
                if (movementMagnitude > 0.3f)
                {
                    //new_EE_pos = UDPComm.EE_pos_ECM + scale * Vector3.Scale(translation_ECM, new Vector3(-1.0f, 1.0f, -1.0f));
                    Vector3 filteredTranslation = motionFilter.UpdateEMA(translation_ECM);
                    new_EE_pos = UDPComm.EE_pos_ECM + scale * Vector3.Scale(filteredTranslation, new Vector3(-1.0f, 1.0f, -1.0f));


                    //Convert pos to same orientation as dVRK i.e. y and z swapped
                    new_EE_pos_send[0] = new_EE_pos[0];
                    new_EE_pos_send[1] = new_EE_pos[2];
                    new_EE_pos_send[2] = new_EE_pos[1];
                    //get new rotation
                    new_EE_rot = UDPComm.EE_quat_ECM;
                    new_EE_rot_send = Quat2Rot(Quaternion.Normalize(new_EE_rot));

                    string pose_message = VectorFromMatrix(new_EE_pos_send, new_EE_rot_send);
                    UDP.GetComponent<UDPComm>().UDPsendECM(pose_message);

                    HideText(ECM_status);
                    ShowText(rotation);
                }
                else
                {
                    HideText(rotation);
                    ShowText(ECM_status);

                }

                lastQuadRotation = currentRotation;
            }


        private void CameraRotation()
    {
        Quaternion currentRotation;

        if (isOneHanded)
        {
            // Blocca la posizione del quadrante
            quad.transform.position = lastQuadPosition;

            // Ruota solo attorno al suo asse Z (forward)
            Vector3 forward = quad.transform.forward;
            currentRotation = Quaternion.LookRotation(forward) * Quaternion.AngleAxis(quad.transform.localEulerAngles.z, Vector3.forward);
        }
        else
        {
            // Due mani: lascia libera la rotazione completa
            currentRotation = quad.transform.localRotation;
        }

        Quaternion deltaRotation = Quaternion.Inverse(lastQuadRotation) * currentRotation;
        deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);
        angle = Mathf.DeltaAngle(0, angle); // Correzione per eccessi oltre 180°

        // Conversione e mapping su base ECM
        Vector3 ecmForward = quad.transform.forward;
        Vector3 ecmUp = quad.transform.up;
        Vector3 ecmRight = quad.transform.right;

        Vector3 translation_ECM = Vector3.zero;
        Quaternion newRotation = UDPComm.EE_quat_ECM;

        if (isOneHanded)
        {
            // Mappa solo rotazione attorno all'asse Z (roll)
            float roll = Vector3.Dot(axis.normalized, ecmForward) * angle;
            Quaternion rollRotation = Quaternion.AngleAxis(roll, Vector3.forward);
            newRotation = rollRotation * newRotation;
        }
        else
        {
            // Mappa rotazione attorno a X (pitch) e Y (yaw)
            float pitch = Vector3.Dot(axis.normalized, ecmRight) * angle;
            float yaw = Vector3.Dot(axis.normalized, ecmUp) * angle;

            Quaternion pitchRotation = Quaternion.AngleAxis(pitch, Vector3.right);
            Quaternion yawRotation = Quaternion.AngleAxis(yaw, Vector3.up);

            newRotation = yawRotation * pitchRotation * newRotation;

            // Movimento traslazionale opzionale (ECM movement)
            translation_ECM = new Vector3(yaw, 0f, pitch).normalized;
            translation_ECM = Quaternion.Euler(0f, UDPComm.ECM_Joints[3] * Mathf.Rad2Deg, 0f) * translation_ECM;
        }

        float movementMagnitude = new Vector2(translation_ECM.x, translation_ECM.z).magnitude;

        if (movementMagnitude > 0.3f || isOneHanded)
        {
            Vector3 filteredTranslation = motionFilter.UpdateEMA(translation_ECM);
            new_EE_pos = UDPComm.EE_pos_ECM + scale * Vector3.Scale(filteredTranslation, new Vector3(-1.0f, 1.0f, -1.0f));

            // Y-Z swap per compatibilità dVRK
            new_EE_pos_send[0] = new_EE_pos[0];
            new_EE_pos_send[1] = new_EE_pos[2];
            new_EE_pos_send[2] = new_EE_pos[1];

            new_EE_rot_send = Quat2Rot(Quaternion.Normalize(newRotation));
            string pose_message = VectorFromMatrix(new_EE_pos_send, new_EE_rot_send);
            UDP.GetComponent<UDPComm>().UDPsendECM(pose_message);

            HideText(ECM_status);
            ShowText(rotation);
        }
        else
        {
            HideText(rotation);
            ShowText(ECM_status);
        }

        lastQuadRotation = currentRotation;
        lastQuadPosition = quad.transform.position; // salva la posizione originale per una mano
    }
    */


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
        quad.gameObject.GetComponent<FolloCamera>().enabled = true;
        //audioFeedback.GetComponent<AudioFeedback>().StartAudio();
        isOpen = true;
        SetCameraStartRotation();
        Debug.Log("camera is open");
        isUpdating = false;
        canRotate = false;
        quad.transform.localScale = originalQuadScale;
    }
    public void closeCamera()
    {
        quad.gameObject.GetComponent<FolloCamera>().enabled = false;
        teleop_on.GetComponent<Renderer>().enabled = false;
        teleop_off.GetComponent<Renderer>().enabled = true;
        stopInsertion();
        isOpen = false;
        isUpdating = true;
        HideText(rotation);
        HideText(insertion);
        HideText(extraction);
        ShowText(ECM_status);
        //audioFeedback.GetComponent<AudioFeedback>().StopAudio();
    }

    public void canRotateCamera()
    {
        quad.gameObject.GetComponent<FolloCamera>().enabled = false;
        stopInsertion();
        teleop_on.GetComponent<Renderer>().enabled = true;
        teleop_off.GetComponent<Renderer>().enabled = false;
        stopInsertion();
        isUpdating = true;
        HideText(rotation);
        HideText(insertion);
        HideText(extraction);
        ShowText(ECM_status);
        canRotate = true;
        startRotation = false;
        quad.transform.localScale = originalQuadScale *1f;
        quadStartRotation = quad.transform.localRotation;
        motionFilter.ResetEMAValue();

    }

    void updatePose()
    {
        cameraStartPosition = mainCamera.transform.position;
        cameraLocalStartRotation = mainCamera.transform.localRotation;
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



