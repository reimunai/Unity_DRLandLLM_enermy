using UnityEngine;
using UnityEngine.AI;

public class ChaseState : EnemyState
{
    private StrategyExecuter executer;
    private EnvPerceiver perceiver;
    private EnemyFocus enemyFocus;
    private NavMeshAgent agent;
    private EnemyShoot enemyShoot;
    
    private Transform currentTarget;
    private float repathTimer = 0f;
    private float repathInterval = 0.3f; // 重新寻路间隔
    private float lastKnownPositionTimer = 0f;
    private float maxLostTargetTime = 3f; // 丢失目标后最多追踪时间
    private Vector2 lastKnownPosition;
    private bool hasTargetInSight = false;
    
    public ChaseState(StrategyExecuter executer)
    {
        this.executer = executer;
        this.perceiver = executer.GetComponent<EnvPerceiver>();
        this.enemyFocus = executer.enemyFocus;
        this.enemyShoot = executer.GetComponent<EnemyShoot>();
        this.agent = executer.agent;
    }
    
    public override void OnEnter()
    {
        Debug.Log("进入追击状态");
        
        // 停止游荡行为，开启追击
        if (agent != null)
        {
            agent.isStopped = false;
            agent.speed = 4f; // 追击时速度可以更快
        }
        
        // 初始化目标
        FindAndSetTarget();
        
        // 设置初始目标为不可见
        hasTargetInSight = false;
        lastKnownPositionTimer = 0f;
    }
    
    public override void OnUpdate()
    {
        if (perceiver == null || agent == null) return;
        
        // 更新计时器
        repathTimer += Time.deltaTime;
        
        // 检查当前目标是否有效
        if (currentTarget == null)
        {
            FindAndSetTarget();
            if (currentTarget == null)
            {
                Debug.Log("未找到有效目标，保持在追击状态但无目标");
                return;
            }
        }
        
        // 更新目标状态
        UpdateTargetVisibility();
        
        // 根据可见性执行不同行为
        if (hasTargetInSight)
        {
            // 目标可见：瞄准并射击
            HandleVisibleTarget();
        }
        else
        {
            // 目标不可见：前往最后已知位置
            HandleHiddenTarget();
        }
        
        // 检查是否需要重新寻路
        if (repathTimer >= repathInterval)
        {
            UpdateDestination();
            repathTimer = 0f;
        }
    }
    
    public override void OnExit()
    {
        Debug.Log("退出追击状态");
        
        // 清理焦点
        if (enemyFocus != null)
        {
            enemyFocus.ClearFocus();
        }
        
        // 停止移动
        if (agent != null)
        {
            agent.isStopped = true;
        }
    }
    
    private void FindAndSetTarget()
    {
        // 通过感知器获取最近的可见玩家
        var nearestPlayer = perceiver.GetNearestVisiblePlayer();
        
        if (nearestPlayer != null && nearestPlayer.playerTransform != null)
        {
            SetTarget(nearestPlayer.playerTransform);
        }
        else
        {
            // 如果没有可见玩家，获取所有检测到的玩家（包括不可见的）
            var allPlayers = perceiver.GetDetectedPlayers();
            if (allPlayers.Count > 0)
            {
                // 选择距离最近的玩家（即使不可见）
                Transform closestPlayer = null;
                float minDistance = float.MaxValue;
                
                foreach (var player in allPlayers)
                {
                    if (player.playerTransform != null && player.distance < minDistance)
                    {
                        minDistance = player.distance;
                        closestPlayer = player.playerTransform;
                        lastKnownPosition = player.lastKnownPosition;
                    }
                }
                
                if (closestPlayer != null)
                {
                    SetTarget(closestPlayer);
                }
            }
        }
    }
    
    private void SetTarget(Transform target)
    {
        currentTarget = target;
        
        // 设置焦点目标
        if (enemyFocus != null)
        {
            enemyFocus.SetFocusTarget(target);
        }
        
        // 记录最后已知位置
        lastKnownPosition = target.position;
        lastKnownPositionTimer = 0f;
        
        Debug.Log($"设置追击目标: {target.name}");
    }
    
    private void UpdateTargetVisibility()
    {
        if (currentTarget == null) return;
        
        // 检查目标是否在感知范围内且可见
        var detectedPlayers = perceiver.GetDetectedPlayers();
        bool targetIsVisible = false;
        
        foreach (var player in detectedPlayers)
        {
            if (player.playerTransform == currentTarget && player.isVisible)
            {
                targetIsVisible = true;
                lastKnownPosition = player.position;
                lastKnownPositionTimer = 0f; // 重置计时器
                break;
            }
        }
        
        hasTargetInSight = targetIsVisible;
        
        if (!targetIsVisible)
        {
            lastKnownPositionTimer += Time.deltaTime;
            
            // 如果目标丢失时间过长，重新寻找目标
            if (lastKnownPositionTimer >= maxLostTargetTime)
            {
                FindAndSetTarget();
            }
        }
    }
    
    private void HandleVisibleTarget()
    {
        if (currentTarget == null) return;
        
        // 计算到目标的距离
        float distanceToTarget = Vector2.Distance(
            agent.transform.position, 
            currentTarget.position
        );
        
        // 根据距离调整行为
        if (distanceToTarget > agent.stoppingDistance)
        {
            // 继续追击
            UpdateDestination();
        }
        else
        {
            // 到达攻击距离，停止移动并射击
            if (agent != null && agent.isActiveAndEnabled)
            {
                agent.isStopped = true;
            }
        }
        
        // 瞄准目标
        if (enemyFocus != null && enemyFocus.focusMode != FocusMode.Target)
        {
            enemyFocus.SetFocusTarget(currentTarget);
        }
        
        // 射击
        if (enemyShoot != null)
        {
            // 使用反射调用HandleShooting方法（因为它是private的）
            enemyShoot.HandleShooting();
        }
    }
    
    private void HandleHiddenTarget()
    {
        // 前往最后已知位置
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = false;
            
            // 设置目的地为最后已知位置
            agent.SetDestination(lastKnownPosition);
            
            // 检查是否到达最后已知位置
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                // 到达后，如果仍然没有目标，重新搜索
                FindAndSetTarget();
                
                // 如果仍然没有目标，可以停留并警戒
                if (currentTarget == null && lastKnownPositionTimer > maxLostTargetTime * 0.5f)
                {
                    // 可以在这里添加警戒行为，如扫描周围
                    if (enemyFocus != null)
                    {
                        enemyFocus.ClearFocus();
                    }
                }
            }
        }
    }
    
    private void UpdateDestination()
    {
        if (currentTarget == null || agent == null || !agent.isActiveAndEnabled) return;
        
        if (hasTargetInSight)
        {
            // 直接追击可见目标
            agent.SetDestination(currentTarget.position);
        }
        else
        {
            // 前往最后已知位置
            agent.SetDestination(lastKnownPosition);
        }
    }
    
    // 辅助方法：检查目标是否有效
    private bool IsTargetValid()
    {
        if (currentTarget == null) return false;
        
        // 检查目标是否被销毁或禁用
        if (!currentTarget.gameObject.activeInHierarchy) return false;
        
        // 检查目标健康状态（如果可用）
        var targetHealth = currentTarget.GetComponent<HealthInteractor>();
        if (targetHealth != null && targetHealth.GetCurrentHealth() <= 0)
        {
            return false;
        }
        
        return true;
    }
    
    // 公开方法：强制设置新目标
    public void ForceSetTarget(Transform newTarget)
    {
        if (newTarget != null)
        {
            SetTarget(newTarget);
        }
    }
    
    // 公开方法：获取当前目标
    public Transform GetCurrentTarget() => currentTarget;
    
    // 公开方法：检查是否有可见目标
    public bool HasTargetInSight() => hasTargetInSight;
}