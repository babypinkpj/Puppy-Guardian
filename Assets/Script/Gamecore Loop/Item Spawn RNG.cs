using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSpawnRNG : MonoBehaviour
{
    [SerializeField] public List<GameObject> Items = new List<GameObject>();
    [SerializeField] public Transform[] itemlocation;
    void Start()
    {
        StartSpawnItem();
    }
    void StartSpawnItem()
    {
        foreach (Transform transform in itemlocation)
        {
            if (transform != null)
            {
                int itemrandom = Random.Range(0, Items.Count);
                Instantiate(Items[itemrandom], transform.position, transform.rotation);
            }
        }
    }
}
