using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{
    public GameObject quad;
    // Start is called before the first frame update
    void Start()
{
    Bounds bounds = quad.GetComponent<Renderer>().bounds;
    Vector3 center = bounds.center;
    Vector3 position = quad.transform.position;

    Debug.Log($"Quad position: {position}, Renderer bounds center: {center}");
    Debug.Log($"Offset: {center - position}");
}

    // Update is called once per frame
    void Update()
    {
        
    }
}
