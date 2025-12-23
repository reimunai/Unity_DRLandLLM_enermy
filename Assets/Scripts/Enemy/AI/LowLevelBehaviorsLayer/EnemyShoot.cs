using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyShoot : MonoBehaviour
{
    [Header("射击设置")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 0.2f;
    [SerializeField] private float bulletSpeed = 10f;
    [SerializeField] private int damage = 1;
    
    private float _timer = 0f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (_timer <= fireRate)
        {
            _timer += Time.deltaTime;
        }
        
    }

    void HandleShooting()
    {
        if ( _timer >= fireRate)
        {
            Shoot();
            _timer = 0f;
        }
    }
    
    void Shoot()
    {
        if (bulletPrefab == null || firePoint == null)
        {
            Debug.LogWarning("子弹预制体或发射点未设置！");
            return;
        }
        
        // 创建子弹
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        
        // 获取子弹的Rigidbody2D
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            // 设置子弹速度（朝向前方）
            bulletRb.velocity = bullet.transform.up * bulletSpeed;
        }
        
        // 设置子弹伤害
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.SetTag("Player", transform.tag);
            bulletScript.SetDamage(damage);
        }
        
        // 可选：播放射击音效
        // AudioManager.Instance.PlaySound("Shoot");
    }
}
