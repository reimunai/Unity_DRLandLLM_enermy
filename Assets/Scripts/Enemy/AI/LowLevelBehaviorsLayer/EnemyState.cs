using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public abstract class EnemyState
{
    public abstract void OnEnter();
    public abstract void OnUpdate();
    public abstract void OnExit();
}