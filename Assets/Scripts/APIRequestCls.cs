using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

[System.Serializable]
public class DeepSeekMessage
{
    public string role;
    public string content;
}

[System.Serializable]
public class DeepSeekRequest
{
    public string model = "deepseek-chat";
    public List<DeepSeekMessage> messages;
    public double temperature = 0.7;
    public int maxTokens = 2048;
}

[System.Serializable]
public class DeepSeekResponse
{
    public List<Choice> choices;
    
    [System.Serializable]
    public class Choice
    {
        public DeepSeekMessage message;
        public string finishReason;
    }
}