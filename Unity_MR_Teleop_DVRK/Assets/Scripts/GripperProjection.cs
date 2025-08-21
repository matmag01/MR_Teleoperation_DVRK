using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Input;
using System;
using MathNet.Numerics.LinearAlgebra;
using System.Linq;
using TMPro;
using System.Globalization;

public class GrippperProjection : MonoBehaviour
{
    Texture2D texStereo;
    Vector2 tipPxPSM1;
    Vector2 tipPxPSM2;
    Vector2 tipPxPSM1Right;
    Vector2 tipPxPSM2Right;
    // Start is called before the first frame update
    void Start()
    {
        texStereo = Img.tex2d_stereo;
    }

    // Update is called once per frame
    void Update()
    {
        // Define tip axis and tip position in pixel
        List<Vector2Int> axesPSM1 = TipVisualNew.GetProjectedAxes(TipVisualNew.EE1_pos, TipVisualNew.EE1_quat, 0.2f);
        List<Vector2Int> axesPSM2 = TipVisualNew.GetProjectedAxes(TipVisualNew.EE2_pos, TipVisualNew.EE2_quat, 0.2f);
        tipPxPSM1 = TipVisualNew.tipPositionPSM1;
        tipPxPSM2 = TipVisualNew.tipPositionPSM2;
        tipPxPSM1Right = TipVisualNew.tipPositionPSM1Right;
        tipPxPSM2Right = TipVisualNew.tipPositionPSM2Right;   
        
    }
}
