using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobberSpawner : MonoBehaviour
{
    [SerializeField] GameObject Robber;
    [SerializeField] Transform[] robberspawner;
    [SerializeField] ItemSpawnRNG spawnRNG;
    void Start()
    {
        spawner();
    }
    void spawner()
    {
        int setspawnlocate = Random.Range(0, robberspawner.Length);
        Vector3 spawnpos = new Vector3(robberspawner[setspawnlocate].position.x, robberspawner[setspawnlocate].position.y + 2.36f, robberspawner[setspawnlocate].position.z);
        GameObject robberSetUp = Instantiate(Robber, spawnpos, robberspawner[setspawnlocate].rotation);
        RobberAI setUp = robberSetUp.GetComponent<RobberAI>();
        int settargetteditem = Random.Range(0, spawnRNG.itemlocation.Length);
        if (setUp != null)
        {
            setUp.robberspawner = robberspawner[setspawnlocate];
            setUp.locateRobbing = spawnRNG.itemlocation[settargetteditem];
        }
    }
}
