using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class HealthInteractor : MonoBehaviour
{
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private int currentHealth;

    [Header("生命恢复设置")]
    [SerializeField] private bool enableHealthRegeneration = true;
    [SerializeField] private float regenerationDelay = 10f; // 多久不受伤害开始恢复
    [SerializeField] private float regenerationRate = 1f; // 每秒恢复多少生命
    [SerializeField] private float regenerationInterval = 1f; // 恢复间隔（秒）

    [Header("事件")]
    public UnityEvent<int> OnHealthChanged;
    public UnityEvent<int, Vector3, string> OnDamageTaken; // 伤害值，来源位置，来源标签
    public UnityEvent OnDeath;
    public UnityEvent OnHealthRegenerated; // 生命恢复事件

    [Header("死亡效果")]
    [SerializeField] private GameObject deathEffect;

    // 私有变量
    private float timeSinceLastDamage = 0f;
    private float regenerationTimer = 0f;
    private Coroutine regenerationCoroutine;

    // Start is called before the first frame update
    void Start()
    {
        currentHealth = maxHealth;

        if (enableHealthRegeneration)
        {
            StartRegenerationCheck();
        }
    }

    void Update()
    {
        if (!enableHealthRegeneration || currentHealth >= maxHealth)
            return;

        // 更新自上次受伤的时间
        timeSinceLastDamage += Time.deltaTime;

        // 如果已经超过恢复延迟时间，开始恢复生命
        if (timeSinceLastDamage >= regenerationDelay)
        {
            regenerationTimer += Time.deltaTime;

            if (regenerationTimer >= regenerationInterval)
            {
                RegenerateHealth();
                regenerationTimer = 0f;
            }
        }
    }

    public void TakeDamage(int damage, Vector3 sourcePosition = default, string sourceTag = "")
    {
        // 重置受伤计时器
        timeSinceLastDamage = 0f;
        regenerationTimer = 0f;

        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;

        // 触发事件
        OnHealthChanged?.Invoke(currentHealth);
        OnDamageTaken?.Invoke(damage, sourcePosition, sourceTag);

        if (currentHealth <= 0)
        {
            OnDeath?.Invoke();
            Die();
        }
    }

    void Die()
    {
        // 停止所有协程
        if (regenerationCoroutine != null)
        {
            StopCoroutine(regenerationCoroutine);
        }

        // 生成死亡效果
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }

        // 可选：增加分数
        // ScoreManager.Instance.AddScore(scoreValue);

        Destroy(gameObject);
    }

    /// <summary>
    /// 恢复生命值
    /// </summary>
    private void RegenerateHealth()
    {
        if (currentHealth >= maxHealth)
            return;

        // 计算恢复的生命值（使用整数）
        int healthToAdd = Mathf.CeilToInt(regenerationRate * regenerationInterval);

        // 确保不会超过最大生命值
        int newHealth = currentHealth + healthToAdd;
        currentHealth = Mathf.Min(newHealth, maxHealth);

        // 触发事件
        OnHealthChanged?.Invoke(currentHealth);
        OnHealthRegenerated?.Invoke();
    }

    /// <summary>
    /// 开始生命恢复检查
    /// </summary>
    private void StartRegenerationCheck()
    {
        if (regenerationCoroutine != null)
        {
            StopCoroutine(regenerationCoroutine);
        }

        regenerationCoroutine = StartCoroutine(RegenerationRoutine());
    }

    /// <summary>
    /// 生命恢复协程（替代Update方法的可选方案）
    /// </summary>
    private IEnumerator RegenerationRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f); // 每0.5秒检查一次

            if (currentHealth >= maxHealth)
                continue;

            timeSinceLastDamage += 0.5f;

            if (timeSinceLastDamage >= regenerationDelay)
            {
                RegenerateHealth();
            }
        }
    }

    /// <summary>
    /// 启用或禁用生命恢复
    /// </summary>
    public void SetHealthRegeneration(bool enabled)
    {
        enableHealthRegeneration = enabled;
        timeSinceLastDamage = 0f;
        regenerationTimer = 0f;
    }

    /// <summary>
    /// 设置恢复参数
    /// </summary>
    public void SetRegenerationParameters(float delay, float rate, float interval)
    {
        regenerationDelay = Mathf.Max(0, delay);
        regenerationRate = Mathf.Max(0, rate);
        regenerationInterval = Mathf.Max(0.1f, interval);
    }

    /// <summary>
    /// 立即恢复指定数量的生命值
    /// </summary>
    public void Heal(int amount)
    {
        if (amount <= 0 || currentHealth >= maxHealth)
            return;

        currentHealth += amount;
        if (currentHealth > maxHealth)
            currentHealth = maxHealth;

        OnHealthChanged?.Invoke(currentHealth);
        OnHealthRegenerated?.Invoke();
    }

    /// <summary>
    /// 完全恢复生命值
    /// </summary>
    public void FullHeal()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth);
        OnHealthRegenerated?.Invoke();
    }

    // 添加获取健康值的方法
    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public float GetHealthPercentage() => (float)currentHealth / maxHealth;

    /// <summary>
    /// 获取距离下一次恢复的时间（秒）
    /// </summary>
    public float GetTimeUntilRegeneration()
    {
        if (!enableHealthRegeneration || currentHealth >= maxHealth)
            return 0f;

        return Mathf.Max(0, regenerationDelay - timeSinceLastDamage);
    }

    /// <summary>
    /// 获取恢复进度（0到1）
    /// </summary>
    public float GetRegenerationProgress()
    {
        if (!enableHealthRegeneration || currentHealth >= maxHealth)
            return 0f;

        return Mathf.Clamp01(timeSinceLastDamage / regenerationDelay);
    }
}