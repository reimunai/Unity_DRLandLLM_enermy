using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmoothFllowCamera : MonoBehaviour
{
    // Start is called before the first frame update
    public Transform fllowTarget;
    public float smooth = 0.1f;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (fllowTarget != null)
        {
            Vector2 targetPos = Vector2.Lerp(transform.position, fllowTarget.position, smooth);
            transform.position = new Vector3(targetPos.x, targetPos.y, transform.position.z);
        }
    }
}
