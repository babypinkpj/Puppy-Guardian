using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EventInfo : MonoBehaviour
{
    public int timer;

    [Header("Weight Down")]
    public string eventName1;
    public string eventDesc1;
    public float num1_1;
    public Color color1;

    [Header("Conditioning")]
    public string eventName2;
    public string eventDesc2;
    public float num2_1;
    public Color color2;

    [Header("Pigeon Invasion")]
    public string eventName3;
    public string eventDesc3;
    public Transform[] pigeonSpawner;
    public GameObject pigeon;
    public bool isPigeonActive = false;
    public float spawnCooldown;
    public Color color3;

    [Header("Light Out")]
    public string eventName4;
    public string eventDesc4;
    public new GameObject light;
    public Image lightscree;
    public Image flash;
    public Color color4;

    [Header("Justice")]
    public string eventName5;
    public string eventDesc5;
    public float num5_1;
    public Color color5;

    [Header("Robust")]
    public string eventName6;
    public string eventDesc6;
    public int num6_1;
    public Color color6;

    [Header("Dirty Duty")]
    public string eventName7;
    public string eventDesc7;
    public float num7_1;
    public Color color7;

    [Header("Cat Thief")]
    public string eventName8;
    public string eventDesc8;
    public float num8_1;
    public Color color8;

    [Header("Tiredness")]
    public string eventName9;
    public string eventDesc9;
    public float num9_1;
    public Color color9;
}
