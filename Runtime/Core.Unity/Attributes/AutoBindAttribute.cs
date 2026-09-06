using System;

/// <summary>
/// 自动绑定特性：标记在字段上，初始化时自动从子物体中查找并绑定组件。
/// <para>
/// 查找规则：
/// 1. 若指定了 Name，按该名称查找子物体
/// 2. 否则按字段名推导：去除 _ 前缀，首字母大写（_muzzle → Muzzle）
/// 3. 找到子物体后，根据字段类型调用 GetComponent
/// 4. 若字段类型为 Transform / GameObject，直接赋值无需 GetComponent
/// </para>
/// <example>
/// [AutoBind] private Transform Muzzle;                    // 找 "Muzzle" 子物体
/// [AutoBind("FlashLight")] private Light2D _flash;        // 找 "FlashLight" 子物体上的 Light2D
/// [AutoBind] private SpriteRenderer _spriteRenderer;      // 找 "SpriteRenderer" 子物体
/// [AutoBind(Self = true)] private Rigidbody2D _rb;        // 从自身 GetComponent
/// </example>
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class AutoBindAttribute : Attribute
{
    /// <summary>子物体名称（为空时按字段名推导）</summary>
    public string Name { get; }

    /// <summary>为 true 时从自身 GetComponent，不搜索子物体</summary>
    public bool Self { get; set; }

    /// <summary>找不到时是否静默（不打印警告）</summary>
    public bool Optional { get; set; }

    public AutoBindAttribute() { }

    public AutoBindAttribute(string childName)
    {
        Name = childName;
    }
}
