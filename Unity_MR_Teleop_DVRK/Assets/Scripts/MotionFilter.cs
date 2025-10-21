// Code taken from Ai, et Al., 2024 --> EMA Filter

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MotionFilter// : MonoBehaviour
{
    
    // The smoothing factor (alpha) for the EMA filter
    public float smoothingFactor = 0.85f;

    // The current EMA value
    public Vector3 emaValue;

    // Update the EMA filter with new input vector
    public Vector3 UpdateEMA(Vector3 inputVector)

    {
        // If it's the first input, set the EMA value to the input vector
        if (emaValue == Vector3.zero)
        {
            emaValue = inputVector;
        }
        else
        {
            // Calculate the new EMA value based on the input vector and smoothing factor
            emaValue = smoothingFactor * inputVector + (1 - smoothingFactor) * emaValue;
        }

        return emaValue;
    }

    public void ResetEMAValue()
    {
        emaValue = Vector3.zero;
    }

    

}
