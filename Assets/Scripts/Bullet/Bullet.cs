using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("子弹设置")]
    [SerializeField] private float lifeTime = 2f;
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private int damage = 1;
    [SerializeField] private string selfTag;
    [SerializeField] private string hitTag;
    
    void Start()
    {
        // 自动销毁子弹
        Destroy(gameObject, lifeTime);
    }
    
    void OnTriggerEnter2D(Collider2D collision)
    {
        // 检查碰撞对象
        if (collision.CompareTag(hitTag) && !collision.CompareTag(selfTag))
        {
            // Debug.Log("Aa");
            // 对敌人造成伤害
            var healthInteractor = collision.GetComponent<HealthInteractor>();
            if (healthInteractor != null)
            {
                healthInteractor.TakeDamage(damage);
            }
            
            // 生成命中效果
            if (hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, Quaternion.identity);
            }
            
            // 销毁子弹
            Destroy(gameObject);
        }
        // 忽略玩家和子弹的碰撞
        else if (!collision.CompareTag("Player") && !collision.CompareTag("Bullet"))
        {
            // 生成命中效果
            if (hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, Quaternion.identity);
            }
            
            Destroy(gameObject);
        }
    }
    
    public void SetDamage(int newDamage)
    {
        damage = newDamage;
    }

    public void SetTag(string hit, string self)
    {
        selfTag = self;
        hitTag = hit;
    }
}