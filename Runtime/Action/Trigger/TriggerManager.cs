// Copyright (c) 2025 NetEase Inc. All rights reserved.
// Author: isakwong
// Date: 2026/04/29

using System.Collections.Generic;
using UnityEngine;

namespace KFramework
{
    /// <summary>
    /// 全局触发器注册表 + 协程驱动者。
    /// 首次调用 KTrigger.Register() 时按需创建 DontDestroyOnLoad 实例。
    ///
    /// 不要直接挂载到场景——通过单例访问。
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class TriggerManager : MonoBehaviour
    {
        private static TriggerManager _instance;
        private static bool _appQuitting;

        public static TriggerManager Instance
        {
            get
            {
                if (_appQuitting) return null;
                if (_instance == null)
                {
                    var go = new GameObject("[TriggerManager]");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<TriggerManager>();
                }
                return _instance;
            }
        }

        private readonly List<KTrigger> _activeTriggers = new();
        public IReadOnlyList<KTrigger> ActiveTriggers => _activeTriggers;

        internal void RegisterInternal(KTrigger trigger)
        {
            if (!_activeTriggers.Contains(trigger))
                _activeTriggers.Add(trigger);
        }

        internal void UnregisterInternal(KTrigger trigger)
        {
            _activeTriggers.Remove(trigger);
        }

        private void OnApplicationQuit()
        {
            _appQuitting = true;
            // 退出时统一解绑，避免事件回调访问已销毁对象
            for (int i = _activeTriggers.Count - 1; i >= 0; i--)
                _activeTriggers[i].Unregister();
            _activeTriggers.Clear();
        }
    }
}
