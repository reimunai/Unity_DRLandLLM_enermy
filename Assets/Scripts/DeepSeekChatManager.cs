using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
public class DeepSeekChatManager : MonoBehaviour
{
    [Header("API配置")]
    public string apiKey = "你的-DeepSeek-API-KEY";
    public string apiUrl = "https://api.deepseek.com/v1/chat/completions";
    public string initmessage = "你是一个有帮助的AI助手";
    
    [Header("对话设置")]
    public List<DeepSeekMessage> conversationHistory = new List<DeepSeekMessage>();
    
    public static DeepSeekChatManager Instance;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 初始化系统消息
            var systemMessage = new DeepSeekMessage
            {
                role = "system",
                content = initmessage
            };
            conversationHistory.Add(systemMessage);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void SendMessage(string userMessage, System.Action<string> onResponse, System.Action<string> onError = null)
    {
        StartCoroutine(SendChatRequest(userMessage, onResponse, onError));
    }
    
    private IEnumerator SendChatRequest(string userMessage, System.Action<string> onResponse, System.Action<string> onError)
    {
        // 添加用户消息到历史记录
        var userMsg = new DeepSeekMessage
        {
            role = "user",
            content = userMessage
        };
        conversationHistory.Add(userMsg);
        
        // 创建请求数据
        var requestData = new DeepSeekRequest
        {
            messages = conversationHistory
        };
        
        string jsonData = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        
        // 创建UnityWebRequest
        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                // 解析响应
                var response = JsonUtility.FromJson<DeepSeekResponse>(request.downloadHandler.text);
                
                if (response.choices != null && response.choices.Count > 0)
                {
                    string assistantReply = response.choices[0].message.content;
                    
                    // 添加助手回复到历史记录
                    var assistantMsg = new DeepSeekMessage
                    {
                        role = "assistant",
                        content = assistantReply
                    };
                    conversationHistory.Add(assistantMsg);
                    
                    onResponse?.Invoke(assistantReply);
                }
                else
                {
                    onError?.Invoke("API返回数据格式错误");
                }
            }
            else
            {
                onError?.Invoke($"请求失败: {request.error}");
            }
        }
    }
    
    // 清空对话历史（保留系统消息）
    public void ClearConversation()
    {
        var systemMessage = conversationHistory.Find(msg => msg.role == "system");
        conversationHistory.Clear();
        if (systemMessage != null)
        {
            conversationHistory.Add(systemMessage);
        }
    }
}