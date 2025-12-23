using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvPerceiver : MonoBehaviour
{
    //打包观察到的环境为自然语言
    public string PackageEnvContent()
    {
        Debug.Log("EnvPerceiver package content");
        var envContent = "Test Env Content";
        return envContent;
    }
}
