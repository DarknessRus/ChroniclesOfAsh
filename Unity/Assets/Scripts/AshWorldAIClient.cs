using System;
using System.Collections;
using System.Collections.Generic;
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
    public string serverBaseUrl = "https://chronicles-of-ash-ai.onrender.com";
    readonly List<string> recentBiomes = new List<string>();

    public void RequestSegment(int chunkIndex, int seed, Action<AshWorldAIProfile> callback)
    {
        StartCoroutine(RequestRoutine(chunkIndex, seed, callback));
    }

    IEnumerator RequestRoutine(int chunkIndex, int seed, Action<AshWorldAIProfile> callback)
    {
        string json = JsonUtility.ToJson(new RequestData { chunkIndex = chunkIndex, seed = seed, playerLevel = 1, previousBiomes = recentBiomes.ToArray() });
        using (var request = new UnityWebRequest(serverBaseUrl.TrimEnd('/') + "/api/world/segment", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = 60;
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("[Ash World AI] Offline fallback: " + request.error);
                yield break;
            }
            AshWorldAIProfile profile = JsonUtility.FromJson<AshWorldAIProfile>(request.downloadHandler.text);
            if (profile != null)
            {
                recentBiomes.Add(profile.biome);
                if (recentBiomes.Count > 4) recentBiomes.RemoveAt(0);
                Debug.Log("[Ash World AI] Received " + profile.biome + " / " + profile.enemyType + " x" + profile.enemyCount + " for chunk " + chunkIndex);
                if (callback != null) callback(profile);
            }
        }
    }

    [Serializable]
    class RequestData
    {
        public int chunkIndex;
        public int seed;
        public int playerLevel;
        public string[] previousBiomes;
    }
}
