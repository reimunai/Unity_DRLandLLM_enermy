using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class StrategyCommandEvent : UnityEvent<LLMStrategyCommand> { }

[RequireComponent(typeof(EnvPerceiver))]
[RequireComponent(typeof(StrategyExecuter))]
public class LLMPlanner : MonoBehaviour
{
    public float callLlmTime = 10f;

    [SerializeField] private String envContent = "";
    
    [SerializeField] private EnvPerceiver envPerceiver;
    [SerializeField] private StrategyExecuter strategyExecuter;
    public StrategyCommandEvent OnStrategyCommand = new StrategyCommandEvent();

    private float _timer;
    private void Start()
    {
        envPerceiver = GetComponent<EnvPerceiver>();
        strategyExecuter = GetComponent<StrategyExecuter>();
        OnStrategyCommand?.AddListener(strategyExecuter.StrategyCommandHandler);
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= callLlmTime)
        {
            envContent = envPerceiver.PackageEnvContent();
            CallLlmForStrategy();
            _timer = 0;
        }
    }

    private void CallLlmForStrategy()
    {
        LLMStrategyCommand command = new LLMStrategyCommand();
        OnStrategyCommand?.Invoke(command);
    }
}
