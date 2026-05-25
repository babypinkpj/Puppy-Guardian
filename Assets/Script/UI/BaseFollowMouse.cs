using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseFollowMouse : MonoBehaviour
{
    public RectTransform target;
    public float followSpeed = 0.1f; 

    [Header("Movement Limits")]
    public bool enableLimit = false;
    public Vector2 minLimit;
    public Vector2 maxLimit;

    void Update()
    {
        Vector3 mouse = Input.mousePosition;

        Vector2 desiredPos = new Vector2(mouse.x, mouse.y) * followSpeed;

        if (enableLimit)
        {
            desiredPos.x = Mathf.Clamp(desiredPos.x, minLimit.x, maxLimit.x);
            desiredPos.y = Mathf.Clamp(desiredPos.y, minLimit.y, maxLimit.y);
        }

        target.anchoredPosition = Vector2.Lerp(
            target.anchoredPosition,
            desiredPos,
            Time.deltaTime * 5f
        );
    }
}
