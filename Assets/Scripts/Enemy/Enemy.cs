using System;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{

    /*float xrate = 0.02f;
    float yrate = 0.01f;
    float zrate = 0.5f;

    float ammount = 0.2f;
    float angle = 20;

    int xdir = 1;
    int ydir = 1;
    int zdir = 1;

    float xval;
    float yval;
    float zval;
    // Start is called before the first frame update
    void Start()
    {
        xval = transform.localScale.x;
        yval = transform.localScale.y;
        zval = transform.eulerAngles.z;

        transform.localScale = transform.localScale +
            new Vector3(UnityEngine.Random.Range(0, ammount * xval),
            UnityEngine.Random.Range(0, ammount * yval), 0);
        transform.transform.Rotate(new Vector3(0, 0, UnityEngine.Random.Range(0, angle)));

        var agent = GetComponent<NavMeshAgent>();
         agent.updateRotation = false;
         agent.updateUpAxis = false;
    }

    void Update()
    {
        if ((Math.Abs(transform.eulerAngles.z - zval) + angle) % 360 < 0 || (Math.Abs(transform.eulerAngles.z - zval) + angle) % 360 > 2 * angle)
        {
            zdir *= -1;
        }
        if (Math.Abs(transform.lossyScale.x - xval) > ammount * xval)
        {
            xdir *= -1;
        }
        if (Math.Abs(transform.lossyScale.y - yval) > ammount * yval)
        {
            ydir *= -1;
        }
        transform.Rotate(new Vector3(0, 0, zrate * zdir));
        transform.localScale = transform.localScale + new Vector3(ammount * xrate * xdir * xval, ammount * yrate * ydir * yval, 0);
    }*/


    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // 禁用NavMeshAgent的自动旋转
        agent.updateRotation = false;

        // 设置合适的速度
        agent.speed = 5f;
    }

    void Update()
    {
        if (agent.hasPath && agent.remainingDistance > agent.stoppingDistance)
        {
            // 获取移动方向并旋转物体
            Vector2 moveDirection = agent.velocity.normalized;

            if (moveDirection.magnitude > 0.1f)
            {
                // 计算旋转角度（2D中朝向X轴右侧为0度）
                float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }
    }
}

