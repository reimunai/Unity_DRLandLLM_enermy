using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class HealthInteractor : MonoBehaviour
{

    [SerializeField] private int maxHealth = 3;
    [SerializeField] private int currentHealth;

    [Header("事件")]
    public UnityEvent<int> OnHealthChanged;
    public UnityEvent<int, Vector3, string> OnDamageTaken; // 伤害值，来源位置，来源标签
    public UnityEvent OnDeath;

    [Header("死亡效果")]
    [SerializeField] private GameObject deathEffect;
    // Start is called before the first frame update
    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage, Vector3 sourcePosition = default, string sourceTag = "")
    {
        currentHealth -= damage;

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
        // 生成死亡效果
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }
        
        // 可选：增加分数
        // ScoreManager.Instance.AddScore(scoreValue);
        
        Destroy(gameObject);
    }

    // 添加获取健康值的方法
    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public float GetHealthPercentage() => (float)currentHealth / maxHealth;
}
