using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StrategyExecuter : MonoBehaviour
{
    // Start is called before the first frame update
    public void StrategyCommandHandler(LLMStrategyCommand strategyCommand)
    {
        Debug.Log("Executing Strategy Command");
    }
}
