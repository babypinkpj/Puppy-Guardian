using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RobberAI : MonoBehaviour
{
    [SerializeField] public NavMeshAgent Robber;
    [SerializeField] public Transform locateRobbing;
    [SerializeField] public Transform robberspawner;
    [SerializeField] public float stealingTimer = 10f;

    private void Start()
    {
        Debug.Log("Robber Status : Checking Closest Item");
        StartCoroutine(RunningToItem());
    }
    IEnumerator RunningToItem()
    {
        yield return new WaitForSeconds(1f);
        Debug.Log("Robber Status : Going to the targetted Item");
        Robber.SetDestination(locateRobbing.position);
        yield return new WaitUntil(() => !Robber.pathPending && Robber.remainingDistance <= Robber.stoppingDistance);
        Debug.Log("Robber Status : Stealing Item");
        StartCoroutine(StealStage());
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player Hitbox")
        {
            Debug.Log("Player attack!");
            StartCoroutine(Stunned());
        }
    }
    IEnumerator Stunned()
    {
        float currentspeed = Robber.speed;
        Robber.speed = 0f;
        yield return new WaitForSeconds(0.65f);
        Robber.speed = currentspeed;
    }

    IEnumerator StealStage()
    {
        yield return new WaitForSeconds((float)stealingTimer);
        StartCoroutine(Escape());
    }

    IEnumerator Escape()
    {
        Robber.SetDestination(robberspawner.position);
        Robber.stoppingDistance = 0.5f;
        yield return new WaitUntil(() => !Robber.pathPending && Robber.remainingDistance <= Robber.stoppingDistance);
        Destroy(gameObject);
    }
}
