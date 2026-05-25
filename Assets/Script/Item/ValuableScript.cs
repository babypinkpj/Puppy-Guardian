using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ValuableScript : MonoBehaviour
{
[Header("Value")]
    public string itemName;
    public int value = 1;

    private void Start()
    {
        // สามารถทำเอฟเฟกต์ spawn หรือเสียงตอนตกได้
        Debug.Log(itemName + " spawned!");
    }

    private void OnTriggerEnter(Collider other)
    {
        // ถ้า player เก็บได้
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player collected " + itemName + "!");
            Destroy(gameObject); // เก็บแล้วหายไป
        }
    }
}
