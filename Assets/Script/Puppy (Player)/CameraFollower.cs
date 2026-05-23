using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollower : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float smoothcam;
    private Vector3 _offest;
    private Vector3 currentvelocity = Vector3.zero;

    private void Awake()
    {
        _offest = transform.position - player.position;
    }
    void LateUpdate()
    {
        var playerPosition = player.position + _offest;
        transform.position = Vector3.SmoothDamp(transform.position, playerPosition, ref currentvelocity ,smoothcam);
    }
}
