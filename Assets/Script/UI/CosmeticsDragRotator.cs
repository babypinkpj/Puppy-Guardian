using UnityEngine;
using UnityEngine.EventSystems;

public class CosmeticsDragRotator : MonoBehaviour
{
    [Tooltip("The parent transform containing the currently active 3D model to rotate.")]
    public Transform targetModelParent;
    
    [Tooltip("Speed of automatic rotation over time.")]
    public float autoRotateSpeed = -30f;

    private void Update()
    {
        if (targetModelParent == null) return;

        foreach (Transform child in targetModelParent)
        {
            if (child.gameObject.activeInHierarchy)
            {
                child.Rotate(0f, autoRotateSpeed * Time.deltaTime, 0f, Space.World);
            }
        }
    }
}
