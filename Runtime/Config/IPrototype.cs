// IPrototype<T>：原型模式接口
// 实现此接口的类型可通过 Clone() 创建独立副本，
// 配合 PrototypeSO<T> 使用实现 SO 模板 → 运行时实例的配置模式。

/// <summary>
/// 原型模式接口。
/// 配合 PrototypeSO&lt;T&gt; 使用：SO 存储模板，运行时 Clone() 生成独立实例。
/// </summary>
public interface IPrototype<out T>
{
    T Clone();
}
