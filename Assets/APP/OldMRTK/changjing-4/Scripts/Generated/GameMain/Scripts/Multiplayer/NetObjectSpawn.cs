using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Changjing_4.ExportRuntime
{
    public class NetObjectSpawn : MonoBehaviour
    {

        public void CmdSpawnPrefab(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            Instantiate(prefab, position, rotation);
        }
    }
}
