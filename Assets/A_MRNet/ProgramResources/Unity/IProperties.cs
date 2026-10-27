using UnityEngine;
using System;
using System.Collections.Generic;


/// <summary>
/// 属性
/// </summary>
[Serializable]
public class Property {
    public string name;
    public GameObject value;
}

namespace Unity {

    [Serializable]
    /// <summary>
    /// 属性列表
    /// </summary>
    public class IProperties  {

        public Property[] _properties;

        private Dictionary<string, Property> _propMap;

        /// <summary>
        /// 获取属性
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public GameObject GetProperty(string name) {
            if (_properties == null) {
                return null;
            }

            if (_propMap == null) {
                _propMap = new Dictionary<string, Property>();
            }

            Property p = null;
            
            if (_propMap.TryGetValue(name, out p)) {
                return p.value;
            }

            for (int i = 0; i < _properties.Length; i++) {
                p = _properties[i];
                if (p.name.Equals(name)) {
                    _propMap.Add(name, p);
                    return p.value;
                }
            }

            return null;
        }
    }
}

