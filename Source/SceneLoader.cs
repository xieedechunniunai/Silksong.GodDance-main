using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    public IEnumerator LoadSceneAndGetGameObject()
    {
        // 加载场景
        AsyncOperation loadSceneOperation = SceneManager.LoadSceneAsync("Song_17", LoadSceneMode.Additive);
        
        // 等待场景加载完成
        while (!loadSceneOperation.isDone)
        {
            yield return null;
        }
        
        // 查找 GameObject
        GameObject spikeCollider = GameObject.Find("Spike Collider");
        
        if (spikeCollider != null)
        {
            Debug.Log("成功找到 Spike Collider");
            // 在这里对 spikeCollider 进行操作
        }
        else
        {
            Debug.LogError("未找到 Spike Collider");
        }
    }
}