using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Input;
using System;
using MathNet.Numerics.LinearAlgebra;
using System.Linq;
using TMPro;
using System.Globalization;

public class InstrumentDrawingWithMovement : MonoBehaviour
{
    int width = 1280;
    int height = 1024;
    Vector2 imageSizeP;
    Vector2 imageSizeM;
    public GameObject quad;
    Vector3 worldPos;
    private GameObject cylinder;
    Vector3 leftHandPosition;
    float pixelDistanceX;
    float pixelDistanceY;
    float meterDistanceX;
    float meterDistanceY;
    public float pixelOffset = -100f; // per esempio, 50 pixel avanti lungo Z
    Vector2 z_px;
    List<Vector2Int> axesPSM2;


    // Start is called before the first frame update
    void Start()
    {
        imageSizeP = new Vector2(width, height);
        Bounds bounds = quad.GetComponent<Renderer>().bounds;
        imageSizeM = new Vector2(bounds.size.x, bounds.size.y); // quad dimension in meters
    }

    // Update is called once per frame
    void Update()
    {
        List<Vector2Int> axesPSM2 = TipVisualNew.GetProjectedAxes(TipVisualNew.EE2_pos, TipVisualNew.EE2_quat);
        z_px = axesPSM2[3];
        Vector2 directionZ = z_px - TipVisualNew.tipPositionPSM2;
        Vector2 dirZ_normalized = directionZ.normalized;
        Vector2 target_px = TipVisualNew.tipPositionPSM2 + dirZ_normalized * pixelOffset;
        // Real-world size and center of the quad
        Bounds bounds = quad.GetComponent<Renderer>().bounds;

        // world-space position of the bottom-left corner of the quad:
        Vector3 bottomLeft = bounds.center 
                            - quad.transform.right * (bounds.size.x / 2f) 
                            - quad.transform.up * (bounds.size.y / 2f);

        // Tip’s pixel coordinates into UV coordinates (range 0–1). 
        // uv.x and uv.y now represent percentages of how far across the image the point is
        Vector2 uv = new Vector2(
            target_px.x / imageSizeP.x,
            target_px.y / imageSizeP.y
        );

        // world-space position of the tip: 
        // - Start at bottom left
        // - Move rightward and upwards ( = how far across and down the image the tip pixel is)
        Vector3 worldPos =
            bottomLeft
            + quad.transform.right * (bounds.size.x * uv.x)
            + quad.transform.up * (bounds.size.y * (1 - uv.y));

        float z = 0.035f;
        Vector3 tipOnQuadPlane = bottomLeft + quad.transform.right * (bounds.size.x * uv.x) + quad.transform.up * (bounds.size.y * (1 - uv.y));
        worldPos = tipOnQuadPlane + quad.transform.forward * z;

        
        if (!HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbTip, Handedness.Left, out MixedRealityPose handPose))
        {
            if (cylinder != null)
            {
                Destroy(cylinder);
            }
            return;
        }
            
        leftHandPosition = handPose.Position;
        
        /*
        if (!CalibrationScript.calib_completed || MovecameraLikeConsole.isOpen)
        {
            Destroy(cylinder);
            return;
        }
        */
        
        // Create instrument grip
        if (cylinder == null)
        {
            cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Material transparentMaterial = new Material(Shader.Find("Standard"));
            transparentMaterial.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); // alpha 0.5

            transparentMaterial.SetFloat("_Mode", 3); // 3 = Transparent
            transparentMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            transparentMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            transparentMaterial.SetInt("_ZWrite", 0);
            transparentMaterial.DisableKeyword("_ALPHATEST_ON");
            transparentMaterial.EnableKeyword("_ALPHABLEND_ON");
            transparentMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            transparentMaterial.renderQueue = 3000;
            cylinder.GetComponent<Renderer>().material = transparentMaterial;
            Destroy(cylinder.GetComponent<Collider>()); // avoid eventually collision with other object 
        }

        // Instrument grip from hand to the instrument 
        Vector3 direction = worldPos - leftHandPosition;
        float distance = direction.magnitude;
        Vector3 midpoint = leftHandPosition + direction / 2f;
        cylinder.transform.position = midpoint;
        cylinder.transform.up = direction.normalized;
        cylinder.transform.localScale = new Vector3(0.005f, distance / 2f, 0.005f); 
    }
}
