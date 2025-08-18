using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

public class StartHandTracking : MonoBehaviour
{
    HandTracking PSM1;
    HandTracking PSM2;
    public float scale;
    public GameObject UDP;
    public GameObject sphere_marker;
    public GameObject hololens;
    public GameObject ONPSM1;
    public GameObject OFFPSM1;
    public GameObject ONPSM2;
    public GameObject OFFPSM2;
    public GameObject quad;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    public void StartHandTrack()
    {
        /*PSM1*/
        GameObject PSM1_HandTrack = new GameObject("PSM1 HandTrack");
        PSM1_HandTrack.transform.parent = this.transform;
        PSM1 = PSM1_HandTrack.AddComponent<HandTracking>();
        PSM1.teleop = false;
        PSM1.sphere_marker = sphere_marker;
        PSM1.scale = scale;
        PSM1.UDP = UDP;
        PSM1.hololens = hololens;
        PSM1.teleop_on = ONPSM1;
        PSM1.teleop_off = OFFPSM1;
        PSM1.quad = quad;
        PSM1.SetPSM("PSM1");
        /*PSM2*/
        GameObject PSM2_HandTrack = new GameObject("PSM2 HandTrack");
        PSM2_HandTrack.transform.parent = this.transform;
        PSM2 = PSM2_HandTrack.AddComponent<HandTracking>();
        PSM2.teleop = false;
        PSM2.sphere_marker = sphere_marker;
        PSM2.scale = scale;
        PSM2.UDP = UDP;
        PSM2.hololens = hololens;
        PSM2.teleop_on = ONPSM2;
        PSM2.teleop_off = OFFPSM2;
        PSM2.quad = quad;
        PSM2.SetPSM("PSM2");
        Debug.Log("FATTO");
    }
}
