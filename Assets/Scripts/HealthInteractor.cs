using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthInteractor : MonoBehaviour
{
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private int currentHealth;
    
    [Header("死亡效果")]
    [SerializeField] private GameObject deathEffect;
    // Start is called before the first frame update
    void Start()
    {
        currentHealth = maxHealth;
    }
    
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        
        if (currentHealth <= 0)
        {
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
}
