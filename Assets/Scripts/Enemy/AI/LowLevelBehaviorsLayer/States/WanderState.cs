using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class WanderState : EnemyState
{
    private StrategyExecuter strategyExecuter;
    private NavMeshAgent agent;
    private Vector3 startPosition;
    private float nextWanderTime;
    private bool hasReachedDestination = true;
    
    public WanderState(StrategyExecuter strategyExecuter)
    {
        this.strategyExecuter = strategyExecuter;
        this.agent = strategyExecuter.agent;
        this.startPosition = strategyExecuter.transform.position;
    }
    
    public override void OnEnter()
    {
        Debug.Log("Idle State Enter");
        nextWanderTime = Time.time + GetRandomDelay();
    }
    
    public override void OnUpdate()
    {
        Debug.Log("Idle State Update");
        
        // 检查是否到达目的地
        if (!hasReachedDestination && agent.remainingDistance <= strategyExecuter.DestinationThreshold)
        {
            hasReachedDestination = true;
            nextWanderTime = Time.time + GetRandomDelay();
        }
        
        // 如果到达目的地且等待时间结束，设置新目的地
        if (hasReachedDestination && Time.time >= nextWanderTime)
        {
            WanderToRandomPosition();
        }
    }
    
    public override void OnExit()
    {
        Debug.Log("Idle State Exit");
    }
    
    private float GetRandomDelay()
    {
        return Random.Range(strategyExecuter.MinWanderDelay, strategyExecuter.MaxWanderDelay);
    }
    
    private void WanderToRandomPosition()
    {
        // 在半径内获取随机点
        Vector3 randomDirection = Random.insideUnitSphere * strategyExecuter.WanderRadius;
        randomDirection += startPosition;
        
        // 使用 NavMesh 寻找有效位置
        NavMeshHit hit;
        Vector3 finalPosition = startPosition;
        
        if (NavMesh.SamplePosition(randomDirection, out hit, strategyExecuter.WanderRadius, NavMesh.AllAreas))
        {
            finalPosition = hit.position;
        }
        
        // 设置目的地
        agent.SetDestination(finalPosition);
        hasReachedDestination = false;
        
        Debug.Log($"Wandering to position: {finalPosition}");
    }
}
