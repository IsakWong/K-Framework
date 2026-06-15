using System;
using UnityEngine;

/// <summary>
/// 调试绘制服务接口
/// 提供 Gizmos 绘制管理能力（矩形、球体、线段、箭头等）
/// </summary>
public interface IDebugService
{
    DrawGizmosElement DrawGizmos(Action action, float time);
    void DrawRectangle(Vector3 center, Vector2 size, Vector2 direction, float duration, Color color);
    void DrawSphere(Vector3 center, float radius, Matrix4x4 matrix, float duration, Color color);
}
