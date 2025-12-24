using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RetreatState : EnemyState
{
    private StrategyExecuter strategyExecuter;
    private NavMeshAgent agent;
    private EnvPerceiver envPerceiver;
    private Vector3 retreatDestination;
    private float nextRepathTime;
    private bool hasReachedDestination = false;
    
    // 撤退相关参数
    private float retreatSpeed = 5f;
    private float repathInterval = 0.5f;
    private float retreatDistance = 15f; // 撤退的目标距离
    private float minRetreatDistance = 8f; // 最小安全距离
    private float maxRetreatTime = 10f; // 最大撤退时间
    private float retreatStartTime;
    
    public RetreatState(StrategyExecuter strategyExecuter)
    {
        this.strategyExecuter = strategyExecuter;
        this.agent = strategyExecuter.agent;
        this.envPerceiver = strategyExecuter.GetComponent<EnvPerceiver>();
        
        // 可以在这里初始化撤退速度等参数
        // 如果StrategyExecuter中有相关配置，可以从那里获取
        this.retreatSpeed = strategyExecuter.retreatSpeed;
        this.retreatDistance = strategyExecuter.retreatDistance;
        this.minRetreatDistance = strategyExecuter.minRetreatDistance;
        this.maxRetreatTime = strategyExecuter.maxRetreatTime;
    }
    
    public override void OnEnter()
    {
        Debug.Log("进入回避状态");
        
        // 记录开始撤退的时间
        retreatStartTime = Time.time;
        
        // 设置撤退时的移动速度
        agent.speed = retreatSpeed;
        agent.stoppingDistance = 0.1f;
        
        // 立即寻找第一个撤退位置
        FindRetreatPosition();
        nextRepathTime = Time.time + repathInterval;
    }
    
    public override void OnUpdate()
    {
        // 检查是否超过最大撤退时间
        if (Time.time - retreatStartTime >= maxRetreatTime)
        {
            Debug.Log("超过最大撤退时间");
            return;
        }
        
        // 检查是否到达目的地
        if (!hasReachedDestination && agent.remainingDistance <= strategyExecuter.DestinationThreshold)
        {
            hasReachedDestination = true;
            Debug.Log("到达目的地");
            
            // 到达目的地后，检查是否还需要继续撤退
            if (ShouldContinueRetreating())
            {
                // 短暂延迟后继续撤退
                FindRetreatPosition();
            }
        }
        
        // 定期重新计算路径，确保远离玩家
        if (Time.time >= nextRepathTime)
        {
            UpdateRetreatPath();
            nextRepathTime = Time.time + repathInterval;
        }
        
        // 检查玩家距离，如果玩家太远，可以考虑停止撤退
        if (IsPlayerTooFar())
        {
            Debug.Log("玩家太远，停止撤退");
            // 这里可以触发状态转换
        }
    }
    
    public override void OnExit()
    {
        Debug.Log("退出回避状态");
        
        // 恢复默认移动速度
        agent.speed = strategyExecuter.chaseSpeed;
        agent.stoppingDistance = 0f;
    }
    
    private void FindRetreatPosition()
    {
        // 获取最近的玩家
        var nearestPlayer = envPerceiver?.GetNearestVisiblePlayer();
        if (nearestPlayer == null)
        {
            // 如果没有检测到玩家，使用最后已知位置或随机撤退
            RetreatFromLastKnownPosition();
            return;
        }
        
        Vector3 playerPosition = nearestPlayer.playerTransform.position;
        Vector3 currentPosition = strategyExecuter.transform.position;
        
        // 计算远离玩家的方向
        Vector3 awayFromPlayer = (currentPosition - playerPosition).normalized;
        
        // 计算撤退位置（远离玩家的方向）
        Vector3 retreatPosition = currentPosition + awayFromPlayer * retreatDistance;
        
        // 使用 NavMesh 寻找有效位置
        NavMeshHit hit;
        if (NavMesh.SamplePosition(retreatPosition, out hit, retreatDistance, NavMesh.AllAreas))
        {
            retreatDestination = hit.position;
            agent.SetDestination(retreatDestination);
            hasReachedDestination = false;
            
            Debug.Log($"撤退到位置: {retreatDestination}");
        }
        else
        {
            // 如果找不到有效位置，尝试其他方向
            FindAlternativeRetreatPosition(playerPosition, currentPosition);
        }
    }
    
    private void FindAlternativeRetreatPosition(Vector3 playerPosition, Vector3 currentPosition)
    {
        // 尝试多个不同的远离方向
        for (int i = 0; i < 8; i++) // 尝试8个方向
        {
            // 计算一个旋转的角度（从原远离方向旋转）
            float angle = i * 45f; // 每45度尝试一次
            Quaternion rotation = Quaternion.Euler(0, angle, 0);
            Vector3 alternativeDirection = rotation * (currentPosition - playerPosition).normalized;
            
            Vector3 alternativePosition = currentPosition + alternativeDirection * retreatDistance;
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(alternativePosition, out hit, retreatDistance, NavMesh.AllAreas))
            {
                retreatDestination = hit.position;
                agent.SetDestination(retreatDestination);
                hasReachedDestination = false;
                
                Debug.Log($"撤退到备选位置: {retreatDestination}");
                return;
            }
        }
        
        // 如果所有方向都失败，撤退到当前位置的反方向（小距离）
        Vector3 fallbackRetreat = currentPosition + (currentPosition - playerPosition).normalized * minRetreatDistance;
        NavMeshHit fallbackHit;
        if (NavMesh.SamplePosition(fallbackRetreat, out fallbackHit, minRetreatDistance, NavMesh.AllAreas))
        {
            retreatDestination = fallbackHit.position;
            agent.SetDestination(retreatDestination);
            hasReachedDestination = false;
            
            Debug.Log($"撤退到默认位置（后退）: {retreatDestination}");
        }
    }
    
    private void RetreatFromLastKnownPosition()
    {
        // 如果没有当前玩家，撤退到随机位置
        Vector3 randomDirection = Random.insideUnitSphere * retreatDistance;
        randomDirection += strategyExecuter.transform.position;
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, retreatDistance, NavMesh.AllAreas))
        {
            retreatDestination = hit.position;
            agent.SetDestination(retreatDestination);
            hasReachedDestination = false;
            
            Debug.Log($"撤退到随机位置: {retreatDestination}");
        }
    }
    
    private void UpdateRetreatPath()
    {
        // 检查玩家是否还在附近
        var nearestPlayer = envPerceiver?.GetNearestVisiblePlayer();
        if (nearestPlayer != null && nearestPlayer.isVisible)
        {
            // 如果玩家仍然可见，重新计算撤退路径
            float distanceToPlayer = Vector3.Distance(
                strategyExecuter.transform.position, 
                nearestPlayer.playerTransform.position
            );
            
            // 如果玩家仍然太近，继续撤退
            if (distanceToPlayer < minRetreatDistance)
            {
                FindRetreatPosition();
            }
        }
    }
    
    private bool ShouldContinueRetreating()
    {
        var nearestPlayer = envPerceiver?.GetNearestVisiblePlayer();
        if (nearestPlayer == null || !nearestPlayer.isVisible)
        {
            // 玩家不可见，可以停止撤退
            return false;
        }
        
        float distanceToPlayer = Vector3.Distance(
            strategyExecuter.transform.position, 
            nearestPlayer.playerTransform.position
        );
        
        // 如果玩家仍然在安全距离内，继续撤退
        return distanceToPlayer < minRetreatDistance;
    }
    
    private bool IsPlayerTooFar()
    {
        var nearestPlayer = envPerceiver?.GetNearestVisiblePlayer();
        if (nearestPlayer == null)
        {
            return true;
        }
        
        float distanceToPlayer = Vector3.Distance(
            strategyExecuter.transform.position, 
            nearestPlayer.playerTransform.position
        );
        
        // 如果玩家距离大于撤退距离的2倍，认为太远了
        return distanceToPlayer > retreatDistance * 2f;
    }
    
    // 公开方法，供其他状态调用
    public bool IsRetreating()
    {
        return !hasReachedDestination;
    }
    
    public Vector3 GetRetreatDestination()
    {
        return retreatDestination;
    }
}