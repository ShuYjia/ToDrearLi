using UnityEngine;

namespace MRAdaptiveTraining
{
    [CreateAssetMenu(menuName = "MR 教学/任务评价规则", fileName = "TrainingPolicy")]
    public sealed class TrainingPolicyAsset : ScriptableObject
    {
        [Tooltip("演示默认值不代表有效的教学标准；真实接入前按任务校准并更改版本号。")]
        public TrainingPolicy rules = new TrainingPolicy();
    }
}
