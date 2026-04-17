

using UnityEngine;

/// <summary>
/// 用于在Inspector中显示自定义名称的特性
/// </summary>
public class DisplayNameAttribute : PropertyAttribute
{
    public string Name { get; private set; }

    public DisplayNameAttribute(string name)
    {
        Name = name;
    }
}