using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DespawnItem : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(despawn());
    }
    IEnumerator despawn()
    {
        yield return new WaitForSeconds(7.5f);
        StartCoroutine(Shrink());
    }
    IEnumerator Shrink()
    {
        float duration = 0.5f;
        float t = 0f;
        Vector3 startScale = transform.localScale;
        Vector3 endScale = Vector3.zero;

        while (t < duration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, endScale, t / duration);
            yield return null;
        }

        transform.localScale = Vector3.zero;
        gameObject.SetActive(false); // hide after shrinking
    }
}
