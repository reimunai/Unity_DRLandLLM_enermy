using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LLMActionCommand
{
    public Vector2 TargetPosition;
}

public enum ActionType
{
    None,
    GoTarget,
    Focus,
    Fire,
}