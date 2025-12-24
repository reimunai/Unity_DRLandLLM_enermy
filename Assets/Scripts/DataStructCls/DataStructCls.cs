using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LLMStrategyCommand
{
    public ActionType Action;
    public Transform TargetObject;
    public Vector2 TargetPosition;
}

public enum ActionType
{
    Idle,
    GoTarget,
    Chase,
    Attack,
    Retreat
}
// DataStructCls.cs 中添加这些类


// LLM决策数据结构
[System.Serializable]
public class LLMDecision
{
    public string action;
    public string reason;
    public TargetPosition target_position;
    public int priority;
}

[System.Serializable]
public class TargetPosition
{
    public float x;
    public float y;
}