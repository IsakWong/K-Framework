using System;
using System.Collections.Generic;

namespace KFramework.Editor
{
    /// <summary>
    /// 编辑器订阅管理器，统一管理 IDisposable 订阅的生命周期
    /// </summary>
    public sealed class KEditorSubscriber : IDisposable
    {
        private readonly List<IDisposable> _disposables = new();

        public void Add(IDisposable disposable)
        {
            _disposables.Add(disposable);
        }

        public void Add(Action unsubscribe)
        {
            _disposables.Add(new ActionDisposable(unsubscribe));
        }

        public void Clear()
        {
            foreach (var d in _disposables)
                d?.Dispose();
            _disposables.Clear();
        }

        public void Dispose()
        {
            Clear();
        }

        private sealed class ActionDisposable : IDisposable
        {
            private Action _action;
            public ActionDisposable(Action action) => _action = action;
            public void Dispose() => _action?.Invoke();
        }
    }
}
