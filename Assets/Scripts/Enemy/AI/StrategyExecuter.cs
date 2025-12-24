using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class StrategyExecuter : MonoBehaviour
{
    // Start is called before the first frame update
    
    public EnemyState CurrentState;
    
    [Header("AI Wander State")] 
    public NavMeshAgent agent;
    public float WanderRadius = 10f;
    public float MinWanderDelay = 2f;
    public float MaxWanderDelay = 5f;
    public float DestinationThreshold = 0.5f;

    [Header("AI Idle State")] 
    public EnemyFocus enemyFocus;
    public float scansAngle = 45f;
    public float scanSpeed = 90f; // 度/秒
    public float scanStartDelay = 1f; // 开始扫描前的延迟
    
    [Header("AI Chase State")]
    public float chaseSpeed = 4f;
    public float repathInterval = 0.3f;
    public float maxLostTargetTime = 3f;
    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        enemyFocus = GetComponent<EnemyFocus>();
        
        var newState = new ChaseState(this);
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
