using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("移动设置")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    
    [Header("射击设置")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 0.2f;
    [SerializeField] private float bulletSpeed = 10f;
    [SerializeField] private int damage = 1;
    
    [Header("组件引用")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Camera mainCamera;
    
    // 私有变量
    private Vector2 movement;
    private Vector2 mousePosition;
    private float nextFireTime = 0f;
    
    void Start()
    {
        // 如果组件未手动分配，自动获取
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
        
        if (mainCamera == null)
            mainCamera = Camera.main;
    }
    
    void Update()
    {
        // 获取输入
        HandleInput();
        
        // 处理射击
        HandleShooting();
        
        // 旋转玩家朝向鼠标
        RotateTowardsMouse();
    }
    
    void FixedUpdate()
    {
        // 物理移动
        MovePlayer();
    }
    
    void HandleInput()
    {
        // 获取WASD或方向键输入
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        
        movement = new Vector2(horizontal, vertical).normalized;
        
        // 获取鼠标位置
        mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
    }
    
    void MovePlayer()
    {
        // 使用Rigidbody2D进行移动
        rb.velocity = movement * moveSpeed;
    }
    
    void RotateTowardsMouse()
    {
        if (mousePosition == Vector2.zero) return;
        
        // 计算朝向鼠标的向量
        Vector2 direction = mousePosition - rb.position;
        
        // 计算角度并应用旋转
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        
        // 平滑旋转
        Quaternion targetRotation = Quaternion.AngleAxis(angle, Vector3.forward);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
    
    void HandleShooting()
    {
        // 检查是否可以射击（左键按下）
        if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
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
            bulletScript.SetDamage(damage);
        }
        
        // 可选：播放射击音效
        // AudioManager.Instance.PlaySound("Shoot");
    }
}
