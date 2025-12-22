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