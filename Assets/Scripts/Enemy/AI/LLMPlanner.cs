/*using System;
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

        // 监听伤害事件
        if (envPerceiver != null)
        {
            var healthInteractor = GetComponent<HealthInteractor>();
            if (healthInteractor != null)
            {
                healthInteractor.OnDamageTaken.AddListener((damage, sourcePos, sourceTag) =>
                {
                    envPerceiver.RecordDamage(damage, sourcePos, sourceTag);
                    // 立即更新策略（可选）
                    // CallLlmForStrategy();
                });
            }
        }
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= callLlmTime)
        {
            envContent = envPerceiver.PackageEnvContent();
            CallLlmForStrategy();
            _timer = 0;
            Debug.Log(envContent);
        }
    }

    private void CallLlmForStrategy()
    {
        LLMStrategyCommand command = new LLMStrategyCommand();
        OnStrategyCommand?.Invoke(command);
    }
    private string ConstructLLMPrompt(string environmentContext)
    {
        return $@"你是一个2D游戏AI决策系统。基于以下环境信息，选择合适的行动策略：

{environmentContext}

可用的行动类型：
1. Idle - 保持静止，观察环境
2. GoTarget - 移动到指定位置
3. Chase - 追逐最近的可见玩家
4. Attack - 攻击目标玩家
5. Retreat - 撤退到安全位置

请根据当前情况，选择最合适的行动，并说明理由。请以JSON格式返回决策：
{{
    ""action"": ""ActionName"",
    ""reason"": ""你的推理过程"",
    ""target_position"": {{""x"": 0.0, ""y"": 0.0}},
    ""priority"": 1
}}";
    }
    private Vector2 CalculateRetreatPosition()
    {
        // 2D撤退逻辑
        var players = envPerceiver.GetDetectedPlayers();
        if (players.Count > 0)
        {
            // 远离最近的玩家
            Vector2 awayDirection = (Vector2)transform.position - players[0].position;
            Vector2 retreatPos = (Vector2)transform.position + awayDirection.normalized * 10f;
            return retreatPos;
        }

        // 如果没检测到玩家，后退一段距离
        Vector2 backward = -transform.right * 10f;
        return (Vector2)transform.position + backward;
    }
}

[System.Serializable]
public class Vector2Data
{
    public float x;
    public float y;
}

*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

public class StrategyCommandEvent : UnityEvent<LLMStrategyCommand> { }

[RequireComponent(typeof(EnvPerceiver))]
[RequireComponent(typeof(StrategyExecuter))]
public class LLMPlanner : MonoBehaviour
{
    [Header("LLM配置")]
    public float callLlmTime = 10f;
    [SerializeField] private bool useLLM = true;
    [SerializeField] private bool debugMode = true;

    /*[Header("传统算法后备")]
    [SerializeField] private TraditionalAI traditionalAI;*/

    [SerializeField] private string envContent = "";
    [SerializeField] private EnvPerceiver envPerceiver;
    [SerializeField] private StrategyExecuter strategyExecuter;
    public StrategyCommandEvent OnStrategyCommand = new StrategyCommandEvent();

    private float _timer;
    private bool _isProcessing = false;
    private LLMStrategyCommand _lastCommand;

    private void Start()
    {
        envPerceiver = GetComponent<EnvPerceiver>();
        strategyExecuter = GetComponent<StrategyExecuter>();
        /*traditionalAI = GetComponent<TraditionalAI>() ?? gameObject.AddComponent<TraditionalAI>();*/

        OnStrategyCommand?.AddListener(strategyExecuter.StrategyCommandHandler);

        // 初始化系统提示词
        InitializeSystemPrompt();
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= callLlmTime && !_isProcessing)
        {
            envContent = envPerceiver.PackageEnvContent();
            Debug.Log(envContent);
            CallForStrategy();
            _timer = 0;
        }
    }

    private void CallForStrategy()
    {
        if (useLLM && DeepSeekChatManager.Instance != null)
        {
            Debug.Log("CallForStrategy");
            CallLlmForStrategy();
        }
        else
        {
            // 使用传统算法作为后备
            //UseTraditionalAI();
        }
    }

    private void InitializeSystemPrompt()
    {
        // 可以在这里初始化系统提示词
        string systemPrompt = @"你是一个2D游戏的AI敌人决策系统。你的任务是根据环境信息做出合理的战术决策。

游戏规则：
1. 你是游戏中的敌人，目标是击败玩家
2. 你有多种行动选择：待机、移动、追击、攻击、撤退
3. 需要考虑自身血量、玩家位置、距离等因素

请用JSON格式返回决策，确保格式正确。";

        // 如果需要，可以在这里设置系统消息
        // DeepSeekChatManager.Instance?.SetSystemPrompt(systemPrompt);
    }

    private void CallLlmForStrategy()
    {
        if (_isProcessing) return;

        _isProcessing = true;

        // 构造完整的提示词
        string prompt = ConstructLLMPrompt();

        if (debugMode)
        {
            Debug.Log($"发送给LLM的提示词：\n{prompt}");
        }

        // 调用DeepSeek API
        DeepSeekChatManager.Instance.SendMessage(
            prompt,
            OnLLMResponse,
            OnLLMError
        );
    }

    private string ConstructLLMPrompt()
    {
        StringBuilder prompt = new StringBuilder();

        prompt.AppendLine("请根据以下游戏环境信息，为我选择一个合适的行动策略。");
        prompt.AppendLine("请仔细分析环境状态，做出明智的决策。");
        prompt.AppendLine();
        prompt.AppendLine("【当前环境信息】");
        prompt.AppendLine(envContent);
        prompt.AppendLine();
        prompt.AppendLine("【可选行动】");
        prompt.AppendLine("1. Idle - 保持静止，观察环境（当没有玩家可见或需要等待时机时）");
        prompt.AppendLine("2. GoTarget - 移动到指定坐标位置（需要提供目标位置）");
        prompt.AppendLine("3. Chase - 追逐最近的可见玩家（当玩家可见且距离较远时）");
        // prompt.AppendLine("4. Attack - 攻击目标玩家（当玩家在攻击范围内时）");
        prompt.AppendLine("5. Retreat - 撤退到安全位置（当生命值低或需要恢复时）");
        prompt.AppendLine();
        prompt.AppendLine("【决策要求】");
        prompt.AppendLine("- 根据当前情况选择最合适的行动");
        prompt.AppendLine("- 说明选择该行动的理由");
        prompt.AppendLine("- 如果需要移动或撤退，请提供具体的目标位置坐标");
        prompt.AppendLine("- 优先考虑生存，生命值低于30%时应考虑撤退");
        prompt.AppendLine("- 有可见玩家时优先考虑攻击或追击");
        prompt.AppendLine();
        prompt.AppendLine("请以严格的JSON格式返回决策，格式如下：");
        prompt.AppendLine("{");
        prompt.AppendLine("  \"action\": \"行动名称\",");
        prompt.AppendLine("  \"reason\": \"你的决策理由，详细说明为什么选择这个行动\",");
        prompt.AppendLine("  \"target_position\": {\"x\": 0.0, \"y\": 0.0},");
        prompt.AppendLine("  \"priority\": 1");
        prompt.AppendLine("}");
        prompt.AppendLine();
        prompt.AppendLine("注意：行动名称必须精确匹配上述可选行动的名称。");

        return prompt.ToString();
    }

    private void OnLLMResponse(string response)
    {
        _isProcessing = false;

        if (debugMode)
        {
            Debug.Log($"收到LLM响应：{response}");
        }

        try
        {
            // 尝试解析JSON响应
            LLMDecision decision = ParseLLMResponse(response);

            if (decision != null)
            {
                // 转换为策略命令
                LLMStrategyCommand command = ConvertDecisionToCommand(decision);

                // 验证命令合理性
                if (ValidateCommand(command))
                {
                    _lastCommand = command;
                    OnStrategyCommand?.Invoke(command);

                    if (debugMode)
                    {
                        Debug.Log($"执行命令：{command.Action}, 理由：{decision.reason}");
                    }
                }
                else
                {
                    Debug.LogWarning("LLM生成的命令不合理，使用传统AI");
                    /*UseTraditionalAI();*/
                }
            }
            else
            {
                Debug.LogWarning("无法解析LLM响应，使用传统AI");
                //UseTraditionalAI();
            }
        }
        catch (System.Exception e)
        {
            _isProcessing = false;
            Debug.LogError($"解析LLM响应时出错：{e.Message}");
            //UseTraditionalAI();
        }
    }

    private void OnLLMError(string error)
    {
        _isProcessing = false;
        Debug.LogWarning($"LLM调用失败：{error}，使用传统AI");
        //UseTraditionalAI();
    }

    private LLMDecision ParseLLMResponse(string response)
    {
        try
        {
            // 清理响应文本，提取JSON部分
            string jsonText = ExtractJsonFromResponse(response);

            if (string.IsNullOrEmpty(jsonText))
            {
                if (debugMode)
                {
                    Debug.LogWarning($"无法从响应中提取JSON：{response}");
                }
                return null;
            }

            // 解析JSON
            LLMDecision decision = JsonUtility.FromJson<LLMDecision>(jsonText);

            if (decision == null)
            {
                if (debugMode)
                {
                    Debug.LogWarning($"JSON解析失败：{jsonText}");
                }
                return null;
            }

            // 验证必需字段
            if (string.IsNullOrEmpty(decision.action))
            {
                Debug.LogWarning("LLM响应缺少action字段");
                return null;
            }

            return decision;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"解析响应时出错：{e.Message}");
            return null;
        }
    }

    private string ExtractJsonFromResponse(string response)
    {
        // 查找JSON开始和结束位置
        int startIndex = response.IndexOf('{');
        int endIndex = response.LastIndexOf('}');

        if (startIndex >= 0 && endIndex > startIndex)
        {
            return response.Substring(startIndex, endIndex - startIndex + 1);
        }

        return response;
    }

    private LLMStrategyCommand ConvertDecisionToCommand(LLMDecision decision)
    {
        LLMStrategyCommand command = new LLMStrategyCommand();

        // 根据行动名称设置ActionType
        switch (decision.action.ToLower())
        {
            case "idle":
                command.Action = ActionType.Idle;
                break;
            case "gotarget":
                command.Action = ActionType.GoTarget;
                if (decision.target_position != null)
                {
                    command.TargetPosition = new Vector2(
                        decision.target_position.x,
                        decision.target_position.y
                    );
                }
                break;
            case "chase":
                command.Action = ActionType.Chase;
                // 设置最近的玩家为目标
                var nearestPlayer = envPerceiver.GetNearestVisiblePlayer();
                if (nearestPlayer != null)
                {
                    command.TargetObject = nearestPlayer.playerTransform;
                }
                break;
            case "attack":
                command.Action = ActionType.Attack;
                // 设置最近的玩家为目标
                var targetPlayer = envPerceiver.GetNearestVisiblePlayer();
                if (targetPlayer != null)
                {
                    command.TargetObject = targetPlayer.playerTransform;
                }
                break;
            case "retreat":
                command.Action = ActionType.Retreat;
                if (decision.target_position != null)
                {
                    command.TargetPosition = new Vector2(
                        decision.target_position.x,
                        decision.target_position.y
                    );
                }
                else
                {
                    // 计算撤退位置
                    command.TargetPosition = CalculateRetreatPosition();
                }
                break;
            default:
                command.Action = ActionType.Idle;
                break;
        }

        return command;
    }

    private bool ValidateCommand(LLMStrategyCommand command)
    {
        // 验证命令的合理性
        switch (command.Action)
        {
            case ActionType.GoTarget:
            case ActionType.Retreat:
                // 检查目标位置是否合理
                if (Vector2.Distance(command.TargetPosition, (Vector2)transform.position) > 100f)
                {
                    return false; // 目标位置太远
                }
                break;

            case ActionType.Chase:
            case ActionType.Attack:
                // 检查是否有目标玩家
                if (command.TargetObject == null)
                {
                    return false;
                }
                break;
        }

        return true;
    }

    private Vector2 CalculateRetreatPosition()
    {
        // 撤退逻辑：远离最近的玩家
        var players = envPerceiver.GetDetectedPlayers();
        Vector2 retreatPos = (Vector2)transform.position;

        if (players.Count > 0)
        {
            Vector2 awayDirection = (Vector2)transform.position - players[0].position;
            retreatPos = (Vector2)transform.position + awayDirection.normalized * 8f;
        }
        else
        {
            // 随机撤退方向
            Vector2 randomDir = UnityEngine.Random.insideUnitCircle.normalized;
            retreatPos = (Vector2)transform.position + randomDir * 5f;
        }

        return retreatPos;
    }

/*    private void UseTraditionalAI()
    {
        if (traditionalAI != null)
        {
            LLMStrategyCommand command = traditionalAI.GenerateCommand(envPerceiver);
            if (command != null)
            {
                _lastCommand = command;
                OnStrategyCommand?.Invoke(command);

                if (debugMode)
                {
                    Debug.Log($"使用传统AI生成命令：{command.Action}");
                }
            }
        }
        else
        {
            Debug.LogWarning("传统AI未配置，使用默认待机命令");
            LLMStrategyCommand defaultCommand = new LLMStrategyCommand
            {
                Action = ActionType.Idle
            };
            OnStrategyCommand?.Invoke(defaultCommand);
        }
    }*/

    // 紧急情况立即调用（如受到伤害时）
    public void EmergencyStrategyUpdate()
    {
        if (!_isProcessing)
        {
            _timer = callLlmTime; // 立即触发策略更新
        }
    }

    // 获取最后执行的命令（用于调试）
    public LLMStrategyCommand GetLastCommand()
    {
        return _lastCommand;
    }

    // 设置是否使用LLM
    public void SetUseLLM(bool use)
    {
        useLLM = use;
        if (debugMode)
        {
            Debug.Log($"切换到{(use ? "LLM" : "传统AI")}模式");
        }
    }
}