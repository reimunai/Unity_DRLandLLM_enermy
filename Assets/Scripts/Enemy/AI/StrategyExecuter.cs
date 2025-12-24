using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class StrategyExecuter : MonoBehaviour
{
    // Start is called before the first frame update
    public NavMeshAgent agent;
    
    public EnemyState CurrentState;
    [Header("AI Idle State")] 
    public float WanderRadius = 10f;
    public float MinWanderDelay = 2f;
    public float MaxWanderDelay = 5f;
    public float DestinationThreshold = 0.5f;
    
    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        var newState = new IdleState(this);
        ChangeCurrentState(newState);
    }

    private void Update()
    {
        if (CurrentState != null)
        {
            CurrentState.OnUpdate();
        }
    }

    private void ChangeCurrentState(EnemyState newState)
    {
        if (CurrentState != null)
        {
            CurrentState.OnExit();
        }
        
        CurrentState = newState;
        CurrentState.OnEnter();
    }

    public void StrategyCommandHandler(LLMStrategyCommand strategyCommand)
    {
        Debug.Log(strategyCommand);
        Debug.Log("Executing Strategy Command");
    }
}
