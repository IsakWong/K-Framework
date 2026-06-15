using System;

namespace KFramework.Editor
{
    /// <summary>
    /// 标记一个类为 KFrameworkToolsWindow 的工具页面
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class KToolPageAttribute : Attribute
    {
        public string Name { get; }
        public string Kit { get; set; } = "System";
        public int Priority { get; set; }

        public KToolPageAttribute(string name)
        {
            Name = name;
        }
    }
}
