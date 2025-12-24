using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyFocus : MonoBehaviour
{
    public Transform focusTarget;
    public Vector2 focusPosition;
    public FocusMode focusMode = FocusMode.Normal;
    
    [Header("扫描设置")]
    public float _scansAngle = 45f;
    public float _scanSpeed = 90f; // 度/秒
    public float _scanStartDelay = 1f; // 开始扫描前的延迟
    
    [Header("目标锁定设置")]
    [SerializeField] private float _rotationSpeed = 180f; // 度/秒
    
    private NavMeshAgent _agent;
    private float _currentScanAngle = 0f;
    private bool _isScanningRight = true;
    private bool _isScanning = false;
    
    // Start is called before the first frame update
    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.updateRotation = false;
        _agent.updateUpAxis = false;
        
        // 延迟开始扫描
        Invoke("StartScanning", _scanStartDelay);
    }

    // Update is called once per frame
    void Update()
    {
        switch (focusMode)
        {
            case FocusMode.Normal:
                UpdateNormalMode();
                break;
            case FocusMode.Position:
                UpdatePositionMode();
                break;
            case FocusMode.Target:
                UpdateTargetMode();
                break;
        }
    }
    
    private void UpdateNormalMode()
    {
        if (_agent.hasPath && _agent.remainingDistance > _agent.stoppingDistance)
        {
            // 有移动路径时，朝向移动方向
            Vector2 moveDirection = _agent.velocity.normalized;
            
            if (moveDirection.magnitude > 0.1f)
            {
                float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
                _currentScanAngle = 0f; // 重置扫描角度
            }
        }
        else if (_isScanning)
        {
            // 静止时进行扫描
            PerformScanning();
        }
    }
    
    private void UpdatePositionMode()
    {
        if (focusMode != FocusMode.Position) return;
        
        // 朝向指定位置
        Vector2 direction = focusPosition - (Vector2)transform.position;
        
        if (direction.magnitude > 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            RotateTowardsAngle(targetAngle);
        }
    }
    
    private void UpdateTargetMode()
    {
        if (focusMode != FocusMode.Target || focusTarget == null) return;
        
        // 朝向目标
        Vector2 direction = (Vector2)focusTarget.position - (Vector2)transform.position;
        
        if (direction.magnitude > 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            RotateTowardsAngle(targetAngle);
        }
    }
    
    private void PerformScanning()
    {
        if (!_isScanning) return;
        
        // 更新扫描角度
        float scanStep = _scanSpeed * Time.deltaTime;
        
        if (_isScanningRight)
        {
            _currentScanAngle += scanStep;
            if (_currentScanAngle >= _scansAngle)
            {
                _currentScanAngle = _scansAngle;
                _isScanningRight = false;
            }
        }
        else
        {
            _currentScanAngle -= scanStep;
            if (_currentScanAngle <= -_scansAngle)
            {
                _currentScanAngle = -_scansAngle;
                _isScanningRight = true;
            }
        }
        
        // 应用扫描角度（基于当前基础朝向）
        Vector2 baseDirection = GetBaseDirection();
        float baseAngle = Mathf.Atan2(baseDirection.y, baseDirection.x) * Mathf.Rad2Deg;
        float finalAngle =  _currentScanAngle;
        
        transform.rotation = Quaternion.Euler(0, 0, finalAngle);
    }
    
    private Vector2 GetBaseDirection()
    {
        // 如果没有移动，使用当前朝向作为基础方向
        if (!_agent.hasPath || _agent.remainingDistance <= _agent.stoppingDistance)
        {
            float currentAngle = transform.eulerAngles.z;
            float radianAngle = currentAngle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radianAngle), Mathf.Sin(radianAngle));
        }
        
        // 如果有移动，使用移动方向作为基础方向
        return _agent.velocity.normalized;
    }
    
    private void RotateTowardsAngle(float targetAngle)
    {
        float currentAngle = transform.eulerAngles.z;
        
        // 确保角度在0-360范围内
        targetAngle = Mathf.Repeat(targetAngle, 360);
        currentAngle = Mathf.Repeat(currentAngle, 360);
        
        // 计算最短旋转路径
        float angleDifference = Mathf.DeltaAngle(currentAngle, targetAngle);
        
        // 计算旋转步长
        float maxRotationStep = _rotationSpeed * Time.deltaTime;
        float rotationStep = Mathf.Clamp(angleDifference, -maxRotationStep, maxRotationStep);
        
        // 应用旋转
        float newAngle = currentAngle + rotationStep;
        transform.rotation = Quaternion.Euler(0, 0, newAngle);
    }
    
    private void StartScanning()
    {
        _isScanning = true;
        _currentScanAngle = 0f;
        _isScanningRight = true;
    }
    
    // 公共方法用于切换模式
    public void SetFocusMode(FocusMode newMode)
    {
        focusMode = newMode;
        
        if (focusMode == FocusMode.Normal && !_isScanning)
        {
            _isScanning = true;
        }
    }
    
    public void SetFocusPosition(Vector2 position)
    {
        focusPosition = position;
        focusMode = FocusMode.Position;
        _isScanning = false;
    }
    
    public void SetFocusTarget(Transform target)
    {
        focusTarget = target;
        focusMode = FocusMode.Target;
        _isScanning = false;
    }
    
    public void ClearFocus()
    {
        focusMode = FocusMode.Normal;
        focusTarget = null;
        _isScanning = true;
    }
    
    // 在Inspector中可视化扫描范围
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || !_isScanning) return;
        
        Vector2 baseDirection = GetBaseDirection();
        float baseAngle = Mathf.Atan2(baseDirection.y, baseDirection.x) * Mathf.Rad2Deg;
        
        float leftAngle = baseAngle - _scansAngle * Mathf.Deg2Rad;
        float rightAngle = baseAngle + _scansAngle * Mathf.Deg2Rad;
        
        Vector2 leftDirection = new Vector2(Mathf.Cos(leftAngle), Mathf.Sin(leftAngle));
        Vector2 rightDirection = new Vector2(Mathf.Cos(rightAngle), Mathf.Sin(rightAngle));
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, leftDirection * 5f);
        Gizmos.DrawRay(transform.position, rightDirection * 5f);
        
        // 当前扫描方向
        float currentAngle = baseAngle + _currentScanAngle * Mathf.Deg2Rad;
        Vector2 currentDirection = new Vector2(Mathf.Cos(currentAngle), Mathf.Sin(currentAngle));
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, currentDirection * 3f);
    }
}

public enum FocusMode
{
    Normal,
    Position,
    Target
}