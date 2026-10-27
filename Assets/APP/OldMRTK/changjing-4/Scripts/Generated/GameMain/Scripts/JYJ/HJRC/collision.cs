using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Changjing_4.ExportRuntime
{
    public class collision : MonoBehaviour
    {

        public GameObject prefabA; // 物体A的预设 
        public Vector3 spawnPosition;
        private int spawnCount = 1; // 用于记录生成的次数  
        private int maxSpawns = 50; // 最多生成次数 
        private bool isSpawning = false; // 是否正在生成预制体  

        private void OnTriggerEnter(Collider other)
        {
            // 检查触发器的物体是否是带有"instant"标签的物体  
            if (other.CompareTag("instant") && !isSpawning && spawnCount < maxSpawns)
            {
                StartCoroutine(SpawnPrefabAfterDelay());
                isSpawning = true; // 标记为正在生成  
                  
            }
        }

        // 协程：在3秒后生成预制体，然后重置isSpawning并增加spawnCount  

        IEnumerator SpawnPrefabAfterDelay()
        {
            yield return new WaitForSeconds(5f); // 等待5秒  
            if (LoadSceneObj.isMult)
            {
                NetObjectSpawn netObjectSpawn = GetComponent<NetObjectSpawn>();
                if (netObjectSpawn != null)
                {
                    netObjectSpawn.CmdSpawnPrefab(prefabA, spawnPosition, Quaternion.identity);
                }
                else
                {
                    Instantiate(prefabA, spawnPosition, Quaternion.identity);
                }
            }
            else
            {
                Instantiate(prefabA, spawnPosition, Quaternion.identity); // 生成预制体  
            }
            spawnCount++; // 增加生成次数  
            isSpawning = false; // 重置正在生成状态  

        }
    }
}
