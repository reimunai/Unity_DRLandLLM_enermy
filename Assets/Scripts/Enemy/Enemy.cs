using System;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    void Start()
    {
         var agent = GetComponent<NavMeshAgent>();
         agent.updateRotation = false;
         agent.updateUpAxis = false;
    }
}