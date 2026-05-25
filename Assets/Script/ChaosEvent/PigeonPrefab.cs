using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PigeonPrefab : MonoBehaviour
{
    public Transform bird;
    public int spawnOrder;
    public float speed = 6f;
    void Start()
    {
        transform.position = bird.position;
        Vector3 angles = transform.eulerAngles;
        if (spawnOrder % 2 == 0 )
        {
            angles.x = 0f;
            angles.z = 0f;
            angles.y = 90f;
            transform.eulerAngles = angles;
        }
        else if (spawnOrder % 2 == 1)
        {
            angles.x = 0f;
            angles.z = 0f;
            angles.y = -90f;
            transform.eulerAngles = angles;
        }
        StartCoroutine(despawn());
    }

    void Update()
    {
        if (spawnOrder % 2 == 0)
        {
            transform.Translate(Vector3.forward * speed * Time.deltaTime);
        }
        else if (spawnOrder % 2 == 1)
        {
            transform.Translate(Vector3.forward * speed * Time.deltaTime);
        }
    }

    IEnumerator despawn()
    {
        yield return new WaitForSeconds(2f);
        Destroy(gameObject);
    }
}
