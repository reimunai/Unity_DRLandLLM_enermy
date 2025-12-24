using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

public class EnvPerceiver : MonoBehaviour
{
    [Header("感知配置")]
    [SerializeField] private float perceptionRadius = 15f;
    [SerializeField] private float perceptionAngle = 180f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] public LayerMask obstacleLayer;
    [SerializeField] private float perceptionInterval = 0.5f;

    [Header("状态监控")]
    [SerializeField] private HealthInteractor healthInteractor;
    [SerializeField] private Transform selfTransform;

    private float _perceptionTimer;
    private List<DetectedPlayer> _detectedPlayers = new List<DetectedPlayer>();
    private SelfStatus _selfStatus = new SelfStatus();

    // 关键事件监听
    public UnityEvent<DamageEvent> OnDamageTaken = new UnityEvent<DamageEvent>();

    [System.Serializable]
    public class DetectedPlayer
    {
        public Transform playerTransform;
        public Vector2 position;
        public float distance;
        public float angle;
        public HealthInteractor health;
        public bool isVisible;
        public Vector2 lastKnownPosition;
        public float lastDetectionTime;
    }

    [System.Serializable]
    public class SelfStatus
    {
        public Vector2 position;
        public int currentHealth;
        public int maxHealth;
        public float healthPercentage;
        public Vector2 forwardDirection;
        public bool isAlive = true;
    }

    [System.Serializable]
    public class DamageEvent
    {
        public int damageAmount;
        public Vector2 damageSourcePosition;
        public string damageSourceTag;
        public float timestamp;
    }

    private void Start()
    {
        // 获取自身组件
        healthInteractor = GetComponent<HealthInteractor>();
        selfTransform = transform;
    }

    private void Update()
    {
        _perceptionTimer += Time.deltaTime;

        if (_perceptionTimer >= perceptionInterval)
        {
            UpdateSelfStatus();
            ScanForPlayers();
            _perceptionTimer = 0f;
        }
    }

    private void UpdateSelfStatus()
    {
        if (healthInteractor != null && selfTransform != null)
        {
            _selfStatus.position = selfTransform.position;
            // 这里需要HealthInteractor提供健康值访问方法
            _selfStatus.currentHealth = healthInteractor.GetCurrentHealth();
            _selfStatus.maxHealth = healthInteractor.GetMaxHealth();
            _selfStatus.healthPercentage = (float)_selfStatus.currentHealth / _selfStatus.maxHealth;
            _selfStatus.forwardDirection = selfTransform.right; // 2D中通常使用right作为朝向
        }
    }

    private void ScanForPlayers()
    {
        // 清空之前的检测结果
        _detectedPlayers.Clear();

        // 2D 圆形检测范围内的玩家
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(
            transform.position,
            perceptionRadius,
            playerLayer
        );
        
        foreach (var collider in hitColliders)
        {
            // 检查是否为玩家
            if (collider.CompareTag("Player"))
            {
                Vector2 directionToPlayer = collider.transform.position - transform.position;
                float distance = directionToPlayer.magnitude;

                // 计算角度（2D）
                Vector2 forward2D = transform.right; // 2D中通常使用right作为朝向
                float angle = Vector2.Angle(forward2D, directionToPlayer);

                // 检查是否在视野角度内
                if (angle <= perceptionAngle / 2f)
                {
                    // 检查是否有障碍物遮挡（2D射线检测）
                    bool isVisible = !Physics2D.Raycast(
                        transform.position,
                        directionToPlayer.normalized,
                        distance,
                        obstacleLayer
                    );

                    DetectedPlayer detectedPlayer = new DetectedPlayer
                    {
                        playerTransform = collider.transform,
                        position = collider.transform.position,
                        distance = distance,
                        angle = angle,
                        health = collider.GetComponent<HealthInteractor>(),
                        isVisible = isVisible,
                        lastKnownPosition = collider.transform.position,
                        lastDetectionTime = Time.time
                    };

                    _detectedPlayers.Add(detectedPlayer);
                }
            }
        }
    }

    // 结构化环境信息（2D版本）
    public string PackageEnvContent()
    {
        StringBuilder envDescription = new StringBuilder();

        // 1. 自我状态描述
        envDescription.AppendLine("【自我状态】");
        envDescription.AppendLine($"- 当前位置: ({_selfStatus.position.x:F1}, {_selfStatus.position.y:F1})");
        envDescription.AppendLine($"- 生命值: {_selfStatus.currentHealth}/{_selfStatus.maxHealth} ({_selfStatus.healthPercentage:P0})");
        envDescription.AppendLine($"- 朝向角度: {GetForwardAngle():F0}度");

        // 2. 检测到的玩家信息
        envDescription.AppendLine("\n【环境中的玩家】");
        if (_detectedPlayers.Count == 0)
        {
            envDescription.AppendLine("- 未检测到玩家");
        }
        else
        {
            for (int i = 0; i < _detectedPlayers.Count; i++)
            {
                var player = _detectedPlayers[i];
                string visibility = player.isVisible ? "可见" : "被遮挡";
                envDescription.AppendLine($"- 玩家{i + 1}: 位置({player.position.x:F1}, {player.position.y:F1})");
                envDescription.AppendLine($"  距离: {player.distance:F1}米, 相对角度: {player.angle:F0}度, 状态: {visibility}");
                if (player.health != null)
                {
                    envDescription.AppendLine($"  玩家生命值: {player.health.GetCurrentHealth()}/{player.health.GetMaxHealth()}");
                }
            }
        }

        // 3. 最近的关键事件
        envDescription.AppendLine("\n【关键事件】");
        // 这里可以添加最近受到的伤害、听到的声音等事件

        // 4. 环境特征
        envDescription.AppendLine("\n【环境特征】");
        envDescription.AppendLine($"- 感知半径: {perceptionRadius}米");
        envDescription.AppendLine($"- 视野角度: {perceptionAngle}度");

        return envDescription.ToString();
    }

    // 获取朝向角度（0-360度）
    private float GetForwardAngle()
    {
        Vector2 forward = transform.right;
        float angle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;
        return angle;
    }

    // 绘制调试视图（2D版本）
    private void OnDrawGizmosSelected()
    {
        // 绘制感知范围（2D圆形）
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, perceptionRadius);

        // 绘制视野扇形
        Vector2 forward = transform.right;
        float halfAngle = perceptionAngle / 2f;

        Vector2 leftDir = Quaternion.Euler(0, 0, halfAngle) * forward;
        Vector2 rightDir = Quaternion.Euler(0, 0, -halfAngle) * forward;

        Gizmos.color = new Color(0, 1, 1, 0.3f); // 半透明青色

        // 绘制扇形
        int segments = 20;
        Vector2 prevPoint = (Vector2)transform.position + leftDir * perceptionRadius;
        for (int i = 1; i <= segments; i++)
        {
            float t = (float)i / segments;
            float angle = Mathf.Lerp(halfAngle, -halfAngle, t);
            Vector2 dir = Quaternion.Euler(0, 0, angle) * forward;
            Vector2 point = (Vector2)transform.position + dir * perceptionRadius;

            Gizmos.DrawLine(transform.position, point);
            Gizmos.DrawLine(prevPoint, point);
            prevPoint = point;
        }

        // 绘制检测到的玩家
        Gizmos.color = Color.red;
        foreach (var player in _detectedPlayers)
        {
            Gizmos.DrawWireSphere(player.position, 0.5f);
            Gizmos.DrawLine(transform.position, player.position);
        }
    }

    // 公开方法供外部访问
    public List<DetectedPlayer> GetDetectedPlayers() => _detectedPlayers;
    public SelfStatus GetSelfStatus() => _selfStatus;

    // 事件触发方法
    public void RecordDamage(int damage, Vector2 sourcePosition, string sourceTag)
    {
        DamageEvent damageEvent = new DamageEvent
        {
            damageAmount = damage,
            damageSourcePosition = sourcePosition,
            damageSourceTag = sourceTag,
            timestamp = Time.time
        };

        OnDamageTaken?.Invoke(damageEvent);
    }

    // 辅助方法：获取最近的可见玩家
    public DetectedPlayer GetNearestVisiblePlayer()
    {
        DetectedPlayer nearest = null;
        float minDistance = float.MaxValue;

        foreach (var player in _detectedPlayers)
        {
            if (player.isVisible && player.distance < minDistance)
            {
                minDistance = player.distance;
                nearest = player;
            }
        }

        return nearest;
    }

    // 辅助方法：获取所有可见玩家
    public List<DetectedPlayer> GetAllVisiblePlayers()
    {
        return _detectedPlayers.FindAll(p => p.isVisible);
    }
}