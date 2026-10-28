using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class AshWorldAIProfile
{
    public int chunkIndex;
    public int seed;
    public string biome;
    public string enemyType;
    public int enemyCount;
    public float enemyHealthMultiplier;
    public float enemySpeedMultiplier;
    public int lootTier;
    public string description;
    public string source;
}

public class AshWorldAIClient : MonoBehaviour
{
    public string serverBaseUrl = "https://chroniclesofash-ai.onrender.com";

    public void RequestSegment(int chunkIndex, int seed, Action<AshWorldAIProfile> callback)
    {
        StartCoroutine(RequestRoutine(chunkIndex, seed, callback));
    }

    IEnumerator RequestRoutine(int chunkIndex, int seed, Action<AshWorldAIProfile> callback)
    {
        string json = JsonUtility.ToJson(new RequestData { chunkIndex = chunkIndex, seed = seed, playerLevel = 1 });
        using (var request = new UnityWebRequest(serverBaseUrl.TrimEnd('/') + "/api/world/segment", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = 22;
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("[Ash World AI] Offline fallback: " + request.error);
                yield break;
            }
            AshWorldAIProfile profile = JsonUtility.FromJson<AshWorldAIProfile>(request.downloadHandler.text);
            if (profile != null && callback != null) callback(profile);
        }
    }

    [Serializable]
    class RequestData
    {
        public int chunkIndex;
        public int seed;
        public int playerLevel;
    }
}
