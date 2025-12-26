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

    // 撤退相关参数
    private float retreatSpeed;
    private float repathInterval;
    private float retreatDistance; // 与玩家保持的理想距离
    private float minRetreatDistance; // 最小安全距离
    private float maxRetreatTime;
    private float retreatStartTime;
    private float nextRepathTime;
    private bool isRetreating = false;

    // 防卡住检测
    private float stuckCheckInterval = 0.5f;
    private float nextStuckCheckTime;
    private Vector3 lastPosition;
    private float stuckThreshold = 0.3f;
    private int stuckCount = 0;
    private int maxStuckCount = 3;

    // 撤退尝试相关
    private int retreatAttempts = 0;
    private int maxRetreatAttempts = 5;

    // 玩家位置缓存
    private Vector3 lastPlayerPosition;
    private float playerPositionUpdateInterval = 0.2f;
    private float nextPlayerPositionUpdateTime;

    public RetreatState(StrategyExecuter strategyExecuter)
    {
        this.strategyExecuter = strategyExecuter;
        this.agent = strategyExecuter.agent;
        this.envPerceiver = strategyExecuter.GetComponent<EnvPerceiver>();

        if (strategyExecuter != null)
        {
            this.retreatSpeed = strategyExecuter.retreatSpeed;
            this.retreatDistance = strategyExecuter.retreatDistance;
            this.minRetreatDistance = strategyExecuter.minRetreatDistance;
            this.maxRetreatTime = strategyExecuter.maxRetreatTime;
        }
    }

    public override void OnEnter()
    {
        Debug.Log("进入回避状态");

        // 重置状态
        retreatStartTime = Time.time;
        retreatAttempts = 0;
        stuckCount = 0;
        isRetreating = true;

        // 设置导航参数
        agent.speed = retreatSpeed;
        agent.stoppingDistance = 0.5f; // 增加一些停止距离
        agent.autoBraking = true;
        agent.acceleration = 20f;

        // 记录初始位置
        lastPosition = strategyExecuter.transform.position;

        // 获取初始玩家位置
        UpdatePlayerPosition();

        // 计算理想的撤退位置（距离玩家retreatDistance）
        CalculateIdealRetreatPosition();

        nextRepathTime = Time.time + repathInterval;
        nextStuckCheckTime = Time.time + stuckCheckInterval;
        nextPlayerPositionUpdateTime = Time.time + playerPositionUpdateInterval;
    }

    public override void OnUpdate()
    {
        if (!isRetreating) return;

        // 检查是否超过最大撤退时间
        if (Time.time - retreatStartTime >= maxRetreatTime)
        {
            Debug.Log("超过最大撤退时间，停止撤退");
            agent.ResetPath();
            return;
        }

        // 更新玩家位置
        if (Time.time >= nextPlayerPositionUpdateTime)
        {
            UpdatePlayerPosition();
            nextPlayerPositionUpdateTime = Time.time + playerPositionUpdateInterval;
        }

        // 防卡住检测
        if (Time.time >= nextStuckCheckTime)
        {
            CheckIfStuck();
            nextStuckCheckTime = Time.time + stuckCheckInterval;
        }

        // 检查是否到达目的地
        if (agent.hasPath && !agent.pathPending)
        {
            float remainingDistance = agent.remainingDistance;

            if (remainingDistance <= agent.stoppingDistance + 0.3f ||
                (remainingDistance < 0.5f && agent.velocity.magnitude < 0.1f))
            {
                OnReachedDestination();
                return;
            }
        }
        else if (!agent.hasPath && !agent.pathPending)
        {
            // 如果没有路径，重新计算
            Debug.Log("没有路径，重新计算撤退位置");
            CalculateIdealRetreatPosition();
        }

        // 定期重新计算路径
        if (Time.time >= nextRepathTime)
        {
            UpdateRetreatPath();
            nextRepathTime = Time.time + repathInterval;
        }

        // 检查是否达到理想距离
        if (IsAtIdealDistance())
        {
            Debug.Log("已达到理想撤退距离");
            agent.ResetPath();
            return;
        }
    }

    public override void OnExit()
    {
        Debug.Log("退出回避状态");

        isRetreating = false;

        // 重置导航参数
        if (strategyExecuter != null)
        {
            agent.speed = strategyExecuter.chaseSpeed;
            agent.stoppingDistance = 0f;
        }

        // 停止移动
        agent.ResetPath();
    }

    private void UpdatePlayerPosition()
    {
        var nearestPlayer = envPerceiver?.GetNearestVisiblePlayer();
        if (nearestPlayer != null && nearestPlayer.isVisible)
        {
            lastPlayerPosition = nearestPlayer.playerTransform.position;
        }
        else if (Vector3.Distance(lastPlayerPosition, Vector3.zero) < 0.1f)
        {
            // 如果没有玩家位置记录，使用敌人当前位置的反方向
            lastPlayerPosition = strategyExecuter.transform.position -
                               strategyExecuter.transform.forward * retreatDistance;
        }
    }

    private void CalculateIdealRetreatPosition()
    {

        retreatAttempts++;

        Vector3 currentPosition = strategyExecuter.transform.position;

        // 计算当前位置到玩家的距离
        float currentDistanceToPlayer = Vector3.Distance(currentPosition, lastPlayerPosition);

        // 如果已经在理想距离，寻找一个稍微远一点的位置
        float targetDistance = retreatDistance;
        if (currentDistanceToPlayer >= retreatDistance * 0.9f)
        {
            targetDistance = retreatDistance * 1.2f; // 已经接近理想距离，稍微远一点
        }

        // 方法1：直接计算距离玩家targetDistance的位置
        Vector3 directionFromPlayer = (currentPosition - lastPlayerPosition).normalized;
        Vector3 idealPosition = lastPlayerPosition + directionFromPlayer * targetDistance;

        // 尝试找到最接近理想位置的NavMesh位置
        FindBestRetreatPosition(idealPosition, currentPosition);
    }

    private void FindBestRetreatPosition(Vector3 idealPosition, Vector3 currentPosition)
    {
        // 先尝试理想位置
        NavMeshHit hit;
        if (NavMesh.SamplePosition(idealPosition, out hit, retreatDistance, NavMesh.AllAreas))
        {
            // 检查路径是否可达
            if (IsPositionReachable(hit.position))
            {
                SetRetreatDestination(hit.position);
                return;
            }
        }

        // 如果理想位置不可达，尝试周围的位置
        float searchRadius = 5f;
        int searchAttempts = 8;

        for (int i = 0; i < searchAttempts; i++)
        {
            // 计算搜索角度
            float angle = (360f / searchAttempts) * i;
            Quaternion rotation = Quaternion.Euler(0, angle, 0);

            // 从玩家位置向外搜索
            Vector3 searchDirection = rotation * (idealPosition - lastPlayerPosition).normalized;
            Vector3 searchPosition = lastPlayerPosition + searchDirection * retreatDistance;

            // 调整高度
            searchPosition.y = idealPosition.y;

            if (NavMesh.SamplePosition(searchPosition, out hit, searchRadius, NavMesh.AllAreas))
            {
                // 计算这个位置距离理想位置有多远
                float distanceToIdeal = Vector3.Distance(hit.position, idealPosition);

                // 检查是否可达
                if (IsPositionReachable(hit.position))
                {
                    SetRetreatDestination(hit.position);
                    Debug.Log($"找到替代撤退位置，距离理想位置: {distanceToIdeal:F1}米");
                    return;
                }
            }
        }

        // 如果还是找不到，尝试后退到最小安全距离
        Debug.Log("无法找到理想距离的位置，尝试最小撤退");
        Vector3 fallbackDirection = (currentPosition - lastPlayerPosition).normalized;
        Vector3 fallbackPosition = currentPosition + fallbackDirection * minRetreatDistance;

        if (NavMesh.SamplePosition(fallbackPosition, out hit, minRetreatDistance * 2, NavMesh.AllAreas))
        {
            SetRetreatDestination(hit.position);
            Debug.Log($"使用最小撤退位置");
        }
        else
        {
            // 最后尝试：随机方向
            Vector3 randomDirection = Random.insideUnitSphere.normalized;
            randomDirection.y = 0;
            Vector3 randomPosition = currentPosition + randomDirection * minRetreatDistance;

            if (NavMesh.SamplePosition(randomPosition, out hit, minRetreatDistance, NavMesh.AllAreas))
            {
                SetRetreatDestination(hit.position);
                Debug.Log($"使用随机撤退位置");
            }
            else
            {
                Debug.LogWarning("无法找到任何撤退位置");
                agent.ResetPath();
            }
        }
    }

    private bool IsPositionReachable(Vector3 position)
    {
        if (!agent.isOnNavMesh) return false;

        NavMeshPath path = new NavMeshPath();
        if (agent.CalculatePath(position, path))
        {
            return path.status == NavMeshPathStatus.PathComplete;
        }
        return false;
    }

    private void SetRetreatDestination(Vector3 destination)
    {
        retreatDestination = destination;

        // 重置当前路径
        agent.ResetPath();

        // 设置新目的地
        agent.SetDestination(retreatDestination);

        // 重置卡住计数
        stuckCount = 0;

        Debug.Log($"设置撤退目的地: {destination}, 距离玩家: {Vector3.Distance(destination, lastPlayerPosition):F1}米");
    }

    private void UpdateRetreatPath()
    {
        if (!isRetreating) return;

        // 检查是否需要调整位置以保持理想距离
        float currentDistance = Vector3.Distance(strategyExecuter.transform.position, lastPlayerPosition);

        if (Mathf.Abs(currentDistance - retreatDistance) > retreatDistance * 0.3f)
        {
            // 距离偏差超过30%，重新计算
            Debug.Log($"距离偏差过大({currentDistance:F1}/{retreatDistance:F1})，重新计算");
            CalculateIdealRetreatPosition();
        }
    }

    private void OnReachedDestination()
    {
        Debug.Log("到达撤退目的地");

        // 检查当前距离是否理想
        float currentDistance = Vector3.Distance(strategyExecuter.transform.position, lastPlayerPosition);

        if (currentDistance < retreatDistance * 0.8f)
        {
            // 还是太近，继续撤退
            Debug.Log($"仍然太近({currentDistance:F1}/{retreatDistance:F1})，继续撤退");
            CalculateIdealRetreatPosition();
        }
        else
        {
            // 达到安全距离，可以停止
            Debug.Log($"达到安全距离({currentDistance:F1}/{retreatDistance:F1})");
            agent.ResetPath();
        }
    }

    private void CheckIfStuck()
    {
        Vector3 currentPosition = strategyExecuter.transform.position;
        float distanceMoved = Vector3.Distance(currentPosition, lastPosition);

        if (distanceMoved < stuckThreshold && agent.velocity.magnitude < 0.1f)
        {
            stuckCount++;
            Debug.Log($"可能卡住了，卡住计数: {stuckCount}");

            if (stuckCount >= maxStuckCount)
            {
                Debug.Log("确认卡住，尝试新路径");
                lastPosition = currentPosition;
                CalculateIdealRetreatPosition();
                stuckCount = 0;
            }
        }
        else
        {
            stuckCount = 0;
        }

        lastPosition = currentPosition;
    }

    private bool IsAtIdealDistance()
    {
        float currentDistance = Vector3.Distance(strategyExecuter.transform.position, lastPlayerPosition);

        // 如果当前距离在理想距离的±20%范围内，认为达到理想距离
        return currentDistance >= retreatDistance * 0.8f &&
               currentDistance <= retreatDistance * 1.2f;
    }

    // 公开方法
    public bool IsCurrentlyRetreating()
    {
        return isRetreating;
    }

    public Vector3 GetRetreatDestination()
    {
        return retreatDestination;
    }

    public float GetCurrentDistanceToPlayer()
    {
        return Vector3.Distance(strategyExecuter.transform.position, lastPlayerPosition);
    }

    public float GetDesiredRetreatDistance()
    {
        return retreatDistance;
    }

    // 强制重新计算路径
    public void ForceRecalculatePath()
    {
        if (isRetreating)
        {
            UpdatePlayerPosition();
            CalculateIdealRetreatPosition();
        }
    }
}