using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class IdleState : EnemyState
{
    private EnemyFocus _enemyFocus;

    private float _preScanAngle;
    private float _preScanSpeed;
    private float _preScanStartDelay;
    public IdleState(StrategyExecuter strategyExecuter)
    {
        _enemyFocus = strategyExecuter.enemyFocus;
        _preScanSpeed = _enemyFocus._scanSpeed = strategyExecuter.scanSpeed;
        _preScanAngle = _enemyFocus._scansAngle = strategyExecuter.scansAngle; 
        _preScanStartDelay = _enemyFocus._scanStartDelay = strategyExecuter.scanStartDelay;
    }
    public override void OnEnter()
    {
        _enemyFocus.focusMode = FocusMode.Normal;
    }

    public override void OnUpdate()
    {
        
    }

    public override void OnExit()
    {
        _enemyFocus._scansAngle = _preScanAngle;
        _enemyFocus._scanSpeed = _preScanSpeed;
        _enemyFocus._scanStartDelay = _preScanStartDelay;
        _enemyFocus = null;
    }
}