using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public abstract class Selection2D
{
    public abstract void DoSelection<T>(Vector3 pos, Vector2 direction, int mask, out List<T> result,
        Func<T, bool> cond = null) where T : Component;
    
    /// <summary>
    /// 绘制选择区域的Gizmos
    /// </summary>
    /// <param name="pos">位置</param>
    /// <param name="direction">方向</param>
    /// <param name="duration">持续时间（秒）,-1为单帧绘制</param>
    /// <param name="color">颜色</param>
    public abstract void DrawGizmos(Vector3 pos, Vector2 direction, float duration = -1, Color? color = null);
}



[Serializable]
public class RectangleSelection : Selection2D
{
    [DisplayName("矩形的尺寸")]
    public Vector2 Size;

    public override void DoSelection<T>(Vector3 pos, Vector2 direction, int mask, out List<T> result, Func<T, bool> cond = null)
    {
        Utility.SelectComponentBox2D<T>(pos, Size, Vector2.Angle(Vector2.right, direction), mask, out result, null, cond);
        DrawGizmos(pos, direction);
    }
    
    public override void DrawGizmos(Vector3 pos, Vector2 direction, float duration = -1, Color? color = null)
    {
        DebugManager.DrawRectangle(pos, Size, direction, duration, color);
    }
}


[Serializable]
public class CircleSelection : Selection2D
{
    [DisplayName("半径")]
    public float Radius;

    public override void DoSelection<T>(Vector3 pos, Vector2 direction, int mask, out List<T> result, Func<T, bool> cond = null)
    {
        Utility.SelectComponentCircle2D<T>(pos, Radius, mask, out result, null, cond);
        DrawGizmos(pos, direction);
    }
    
    public override void DrawGizmos(Vector3 pos, Vector2 direction, float duration = -1, Color? color = null)
    {
        DebugManager.DrawCircle(pos, Radius, duration, color);
    }
}

[Serializable]
public class SectorSelection : Selection2D
{
    [DisplayName("半径")]
    public float Radius;
    
    [DisplayName("角度")]
    public float Angle;

    public override void DoSelection<T>(Vector3 pos, Vector2 direction, int mask, out List<T> result, Func<T, bool> cond = null)
    {
        Utility.SelectComponentSector2D<T>(pos, direction, Radius, Angle, mask, out result, null, cond);
        DrawGizmos(pos, direction);
    }
    
    public override void DrawGizmos(Vector3 pos, Vector2 direction, float duration = -1, Color? color = null)
    {
        DebugManager.DrawWedge(pos, direction, Radius, Angle, duration, color);
    }
}

