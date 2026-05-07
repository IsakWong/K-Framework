// PrototypeSO<T>：通用的 ScriptableObject 原型模板
// 作为不可变数据资产存储 T 的静态配置，运行时通过 CreateRuntimeInstance() 克隆使用。
// 相比 PrototypeConfig<T> 的 Variant 覆写模式，本方案更简单：直接 [SerializeReference] 存储原型实例，
// 利用 Unity Inspector 直接编辑，运行时 MemberwiseClone。

using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 通用原型 ScriptableObject 基类。
/// 存储 T 类型实例作为不可变数据模板，运行时通过 Clone() 创建独立副本。
/// T 必须实现 IPrototype&lt;T&gt; 接口。
/// 
/// 继承示例：
/// <code>
/// [CreateAssetMenu(fileName = "BuffConfig", menuName = "JASA/Buff/Buff 配置")]
/// public class BuffConfig : PrototypeSO&lt;Buff&gt; { }
/// </code>
/// Inspector 中创建 SO → 在 Template 字段选择具体子类型 → 配置参数 → 拖到引用处。
/// 运行时调用 config.CreateRuntimeInstance() 获得独立副本。
/// </summary>
public abstract class PrototypeSO<T> : ScriptableObject where T : class, IPrototype<T>
{
    [Header("模板")]
    [SerializeReference]
    [LabelText("原型")]
    [Tooltip("选择具体子类型后在下方配置参数。运行时通过 Clone() 创建独立实例。")]
    [InlineProperty]
    public T Template;

    /// <summary>
    /// 从模板创建运行时实例（Clone）。
    /// </summary>
    public T CreateRuntimeInstance()
    {
        if (Template == null)
        {
            Debug.LogError($"[PrototypeSO<{typeof(T).Name}>] {name} 未配置模板！");
            return null;
        }

        return Template.Clone();
    }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        // 子类可覆写做额外校验（如自动填充名称等）
    }
#endif
}
