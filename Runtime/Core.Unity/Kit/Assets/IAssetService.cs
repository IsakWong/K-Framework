using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// 资源服务接口。
///
/// 只有两个入口：阶段批量加载（label）与按路径读取（配置 / UI）。
/// 游戏资源一律用直接引用挂在配置上，运行期不经过本服务。
/// </summary>
public interface IAssetService
{
    // ─── 阶段加载（唯一异步 IO 入口）───

    Task<IList<T>> LoadAssetsAsync<T>(string label, Action<int, int> onProgress = null) where T : Object;
    bool IsLabelLoaded(string label);
    void ReleaseLabel(string label);

    // ─── 路径读取（配置 / UI）───

    Task<T> LoadAsync<T>(string path) where T : Object;
    T Load<T>(string path) where T : Object;
    bool TryLoad<T>(string path, out T value) where T : Object;

    // ─── 释放 ───

    void ReleaseAll();
}
