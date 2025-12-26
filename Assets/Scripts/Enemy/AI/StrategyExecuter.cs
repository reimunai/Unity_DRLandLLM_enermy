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
    
    [Header("AI Retreat State")]
    public float retreatSpeed = 5f;
    public float retreatDistance = 20f;
    public float minRetreatDistance = 8f;
    public float maxRetreatTime = 10f;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        enemyFocus = GetComponent<EnemyFocus>();
        
        var newState = new IdleState(this);
        // newState.isAggressive = true;
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
        if(CurrentState == newState)
            return;
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
        Debug.Log("excute层执行LLM指令");
        
        if (strategyCommand == null)
        {
            Debug.LogWarning("空指令");
            return;
        }
        
        switch (strategyCommand.Action)
        {
            case ActionType.Idle:
                ChangeCurrentState(new IdleState(this));
                Debug.Log("切换IdleState");
                break;
                
            case ActionType.GoTarget:
                ChangeCurrentState(new IdleState(this));
                Debug.Log("切换IdleState 并前往位置");
                
                // 设置导航目标位置
                if (agent != null && agent.isActiveAndEnabled)
                {
                    Vector3 targetPos = new Vector3(
                        strategyCommand.TargetPosition.x, 
                        strategyCommand.TargetPosition.y,
                        0
                    );
                    agent.ResetPath();
                    agent.SetDestination(targetPos);
                    Debug.Log($"前往: {targetPos}");
                }
                else
                {
                    Debug.LogWarning("NavMeshAgent is not available or disabled");
                }
                break;
                
            case ActionType.Chase:
                ChangeCurrentState(new ChaseState(this));
                Debug.Log("切换 ChaseState");
                break;
                
            case ActionType.Attack:
                Debug.Log("执行 Attack 指令");
                // 获取EnemyShoot组件并调用射击
                EnemyShoot enemyShoot = GetComponent<EnemyShoot>();
                if (enemyShoot != null)
                {
                    enemyShoot.HandleShooting();
                }
                else
                {
                    Debug.LogWarning("EnemyShoot component not found on this GameObject");
                }
                break;
                
            case ActionType.Retreat:
                ChangeCurrentState(new RetreatState(this));
                Debug.Log("切换 RetreatState");
                break;
                
            default:
                Debug.LogWarning($"未知指令: {strategyCommand.Action}");
                break;
        }
    }
}
