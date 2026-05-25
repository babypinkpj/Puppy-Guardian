using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ChaosMode : MonoBehaviour
{
    public TextMeshProUGUI eventname;
    public TextMeshProUGUI description;
    public GameObject eventnameobject;
    public GameObject descriptionobject;
    public PlayerMovement playerMovement;
    public GameManager gameManager;
    public int slownessChaos;
    public float ChoasDuration;
    public Animator animator;

    [Header("Test Chaos Mode")]
    public bool activateChaosMode;
    void Start()
    {
        if (StaticData.chaosmode == true)
        {
            Debug.Log("Chaos Mode Activate!");
        }
    }
    
    public void EventNofi(string name, string info, Color color)
    {
        Debug.Log("Run Event");
        eventname.text = name;
        eventname.color = color;
        description.text = info;
        animator.SetTrigger("isEvent");
    }
}
