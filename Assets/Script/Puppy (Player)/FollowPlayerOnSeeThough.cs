using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPlayerOnSeeThough : MonoBehaviour
{
    public static int position = Shader.PropertyToID("_FollowPlayer");
    public static int ditcher = Shader.PropertyToID("_Dither");

    [Header("Wall Layers Material")]
    [SerializeField] public Material wallMaterial;
    [Header("Layer Masks")]
    [SerializeField] public LayerMask behindWall;
    [Header("Set Up")]
    [SerializeField] public Camera cam;
    [Header("SmoothTransiton")]
    public float ditcherSize = 1f;
    public float currentSize = 0f;
    public float speedTransition = 2f;
    void Update()
    {
        var playerposition = cam.transform.position - transform.position;
        var ray = new Ray(transform.position, playerposition.normalized);
        //Layer 1
        if(Physics.Raycast(ray, 3000, behindWall))
        {
            currentSize = Mathf.Lerp(currentSize, ditcherSize, Time.deltaTime * speedTransition);
        }
        else
        {
            currentSize = Mathf.Lerp(currentSize, -1, Time.deltaTime * speedTransition);
        }
        wallMaterial.SetFloat(ditcher, currentSize);
        var viewplayer = cam.WorldToViewportPoint(transform.position);
        wallMaterial.SetVector(position, viewplayer);
    }
}
