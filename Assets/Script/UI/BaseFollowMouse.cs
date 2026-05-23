using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseFollowMouse : MonoBehaviour
{
    public RectTransform target;
    public float followSpeed = 0.1f; 
    void Update()
    {
        Vector3 mouse = Input.mousePosition;

        Vector2 desiredPos = new Vector2(mouse.x, mouse.y) * followSpeed;

        target.anchoredPosition = Vector2.Lerp(
            target.anchoredPosition,
            desiredPos,
            Time.deltaTime * 5f
        );
    }
}
