using Framework.Foundation;
using Mopsicus.InfiniteScroll;
using System;
using System.Collections.Generic;
using Framework.DebugGUI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using Object = UnityEngine.Object;

public class DebugManager : PersistentSingleton<DebugManager>, IDebugService
{
    private GameObject DebugPanelInstance;
    
    public Canvas DebugCanvas;

    private void OnDrawGizmos()
    {
        for (var i = _gizmosElements.Count - 1; i >= 0; i--)
        {
            _gizmosElements[i].OnGizmos.Invoke();
            if (_gizmosElements[i].Time < 0)
                _gizmosElements.RemoveAt(i);
        }
    }

    private void OnGUI()
    {
    }

    void FixedUpdate()
    {
        for (var i = _gizmosElements.Count - 1; i >= 0; i--)
        {
            _gizmosElements[i].Time -= Time.fixedDeltaTime;
        }
    }
    
    private List<DrawGizmosElement> _gizmosElements = new();

    protected override void OnServiceRegistered()
    {
        ServiceLocator.Register<IDebugService>(this);
    }


    public DrawGizmosElement DrawGizmos(Action action, float time)
    {
        var element = new DrawGizmosElement();
        element.OnGizmos = action;
        element.Time = time;
        _gizmosElements.Add(element);
        return element;
    }
    
    public void DrawRectangle(Vector3 center, Vector2 size, Vector2 direction, float duration, Color color)
    {
        Action action = () =>
        {
            Gizmos.color = color;
            var halfSize = size * 0.5f;
            
            // Normalize direction and calculate perpendicular vectors
            Vector3 right = direction;
            var up = Quaternion.AngleAxis(90, Vector3.forward) * right;

            var p1 = center + (Vector3)(right * halfSize.x + up * halfSize.y);
            var p2 = center + (Vector3)(-right * halfSize.x + up * halfSize.y);
            var p3 = center + (Vector3)(-right * halfSize.x - up * halfSize.y);
            var p4 = center + (Vector3)(right * halfSize.x - up * halfSize.y);

            Debug.DrawLine(p1, p2);
            Debug.DrawLine(p2, p3);
            Debug.DrawLine(p3, p4);
            Debug.DrawLine(p4, p1);
        };
        DrawGizmos(action, duration);
    }
    
    public void DrawSphere(Vector3 center, float radius, Matrix4x4 matrix, float duration, Color color)
    {
        Action action = () =>
        {
            Gizmos.matrix = matrix;
            Gizmos.color = color;
            Gizmos.DrawWireSphere(center, radius);
        };
        DrawGizmos(action, duration);
    }


    public static DrawGizmosElement AddGizmos(Action action, float time = 0.1f)
    {
        var instance = Instance;
        return instance.DrawGizmos(action, time);
    }

    // ==================== 2D Gizmos 静态接口 ====================
    
    /// <summary>
    /// 绘制2D矩形
    /// </summary>
    public static void DrawRect(Vector3 center, Vector2 size, float duration = -1, Color? color = null)
    {
        DrawRectangle(center, size, Vector2.right, duration, color ?? Color.white);
    }
    
    /// <summary>
    /// 绘制2D旋转矩形
    /// </summary>
    public static void DrawRectangle(Vector3 center, Vector2 size, Vector2 direction, float duration = -1, Color? color = null)
    {
        Instance.DrawRectangle(center, size, direction, duration, color ?? Color.white);
    }

    /// <summary>
    /// 绘制2D圆形
    /// </summary>
    public static void DrawCircle(Vector3 center, float radius, float duration = -1, Color? color = null)
    {
        Color circleColor = color ?? Color.white;
        Action action = () =>
        {
            Gizmos.color = circleColor;
            // 在非编辑器环境下使用线段绘制圆形
            float angleStep = 360f / 12;
            Vector3 prevPoint = center + new Vector3(radius, 0, 0);
            
            for (int i = 1; i <= 12; i++)
            {
                float angle = angleStep * i * Mathf.Deg2Rad;
                Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
                Debug.DrawLine(prevPoint, newPoint, circleColor);
                prevPoint = newPoint;
            }
        };
        Instance.DrawGizmos(action, duration);
    }

    /// <summary>
    /// 绘制2D线段
    /// </summary>
    public static void DrawLine(Vector3 from, Vector3 to, float duration = -1, Color? color = null)
    {
        Color lineColor = color ?? Color.white;
        Action action = () =>
        {
            
            Debug.DrawLine(from, to, lineColor);
        };
        Instance.DrawGizmos(action, duration);
    }

    /// <summary>
    /// 绘制2D射线
    /// </summary>
    public static void DrawRay(Vector3 from, Vector3 direction, float duration = -1, Color? color = null)
    {
        DrawLine(from, from + direction, duration, color);
    }

    /// <summary>
    /// 绘制2D箭头
    /// </summary>
    public static void DrawArrow(Vector3 from, Vector3 to, float duration = -1, Color? color = null, float arrowHeadLength = 0.3f, float arrowHeadAngle = 25f)
    {
        Color arrowColor = color ?? Color.white;
        Action action = () =>
        {
            Gizmos.color = arrowColor;
            Debug.DrawLine(from, to);
            
            Vector3 direction = (to - from).normalized;
            Vector3 right = Quaternion.Euler(0, 0, arrowHeadAngle) * -direction;
            Vector3 left = Quaternion.Euler(0, 0, -arrowHeadAngle) * -direction;
            
            Debug.DrawLine(to, to + right * arrowHeadLength);
            Debug.DrawLine(to, to + left * arrowHeadLength);
        };
        Instance.DrawGizmos(action, duration);
    }

    /// <summary>
    /// 绘制2D十字
    /// </summary>
    public static void DrawCross(Vector3 center, float size = 0.5f, float duration = -1, Color? color = null)
    {
        Color crossColor = color ?? Color.white;
        float halfSize = size * 0.5f;
        Action action = () =>
        {
            Gizmos.color = crossColor;
            Debug.DrawLine(center + new Vector3(-halfSize, 0, 0), center + new Vector3(halfSize, 0, 0));
            Debug.DrawLine(center + new Vector3(0, -halfSize, 0), center + new Vector3(0, halfSize, 0));
        };
        Instance.DrawGizmos(action, duration);
    }

    /// <summary>
    /// 绘制2D扇形
    /// </summary>
    public static void DrawWedge(Vector3 center, Vector2 direction, float radius, float angle, float duration = -1, Color? color = null, int segments = 20)
    {
        Color wedgeColor = color ?? Color.white;
        Action action = () =>
        {
            Gizmos.color = wedgeColor;
            float startAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - angle * 0.5f;
            float angleStep = angle / segments;
            
            Vector3 prevPoint = center;
            Debug.DrawLine(center, center + Quaternion.Euler(0, 0, startAngle) * Vector3.right * radius);
            
            for (int i = 0; i <= segments; i++)
            {
                float currentAngle = (startAngle + angleStep * i) * Mathf.Deg2Rad;
                Vector3 point = center + new Vector3(Mathf.Cos(currentAngle), Mathf.Sin(currentAngle), 0) * radius;
                
                if (i > 0)
                    Debug.DrawLine(prevPoint, point);
                
                prevPoint = point;
            }
            
            Debug.DrawLine(prevPoint, center);
        };
        Instance.DrawGizmos(action, duration);
    }

    /// <summary>
    /// 绘制2D圆弧
    /// </summary>
    public static void DrawArc(Vector3 center, Vector2 direction, float radius, float angle, float duration = -1, Color? color = null, int segments = 20)
    {
        Color arcColor = color ?? Color.white;
        Action action = () =>
        {
            Gizmos.color = arcColor;
            float startAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - angle * 0.5f;
            float angleStep = angle / segments;
            
            Vector3 prevPoint = center + Quaternion.Euler(0, 0, startAngle) * Vector3.right * radius;
            
            for (int i = 1; i <= segments; i++)
            {
                float currentAngle = (startAngle + angleStep * i) * Mathf.Deg2Rad;
                Vector3 point = center + new Vector3(Mathf.Cos(currentAngle), Mathf.Sin(currentAngle), 0) * radius;
                Debug.DrawLine(prevPoint, point);
                prevPoint = point;
            }
        };
        Instance.DrawGizmos(action, duration);
    }

    /// <summary>
    /// 绘制2D多边形
    /// </summary>
    public static void DrawPolygon(Vector3[] points, float duration = -1, Color? color = null, bool closed = true)
    {
        if (points == null || points.Length < 2) return;
        
        Color polygonColor = color ?? Color.white;
        Action action = () =>
        {
            Gizmos.color = polygonColor;
            for (int i = 0; i < points.Length - 1; i++)
            {
                Debug.DrawLine(points[i], points[i + 1]);
            }
            
            if (closed && points.Length > 2)
            {
                Debug.DrawLine(points[points.Length - 1], points[0]);
            }
        };
        Instance.DrawGizmos(action, duration);
    }

    /// <summary>
    /// 绘制2D点
    /// </summary>
    public static void DrawPoint(Vector3 position, float size = 0.1f, float duration = -1, Color? color = null)
    {
        Color pointColor = color ?? Color.white;
        Action action = () =>
        {
            Gizmos.color = pointColor;
            Gizmos.DrawSphere(position, size);
        };
        Instance.DrawGizmos(action, duration);
    }

    /// <summary>
    /// 绘制2D路径
    /// </summary>
    public static void DrawPath(Vector3[] points, float duration = -1, Color? color = null)
    {
        DrawPolygon(points, duration, color, false);
    }

    /// <summary>
    /// 绘制2D Bounds
    /// </summary>
    public static void DrawBounds(Bounds bounds, float duration = -1, Color? color = null)
    {
        Color boundsColor = color ?? Color.white;
        Action action = () =>
        {
            Gizmos.color = boundsColor;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        };
        Instance.DrawGizmos(action, duration);
    }

    /// <summary>
    /// 绘制文本（仅在Editor中有效）
    /// </summary>
    public static void DrawText(Vector3 position, string text, float duration = -1, Color? color = null)
    {
#if UNITY_EDITOR
        Color textColor = color ?? Color.white;
        Action action = () =>
        {
            var style = new GUIStyle();
            style.normal.textColor = textColor;
            UnityEditor.Handles.Label(position, text, style);
        };
        Instance.DrawGizmos(action, duration);
#endif
    }

    /// <summary>
    /// 绘制2D椭圆
    /// </summary>
    public static void DrawEllipse(Vector3 center, float radiusX, float radiusY, float duration = -1, Color? color = null, int segments = 32)
    {
        Color ellipseColor = color ?? Color.white;
        Action action = () =>
        {
            Gizmos.color = ellipseColor;
            float angleStep = 360f / segments;
            Vector3 prevPoint = center + new Vector3(radiusX, 0, 0);
            
            for (int i = 1; i <= segments; i++)
            {
                float angle = angleStep * i * Mathf.Deg2Rad;
                Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radiusX, Mathf.Sin(angle) * radiusY, 0);
                Debug.DrawLine(prevPoint, newPoint);
                prevPoint = newPoint;
            }
        };
        Instance.DrawGizmos(action, duration);
    }

    /// <summary>
    /// 绘制2D胶囊体
    /// </summary>
    public static void DrawCapsule2D(Vector3 center, Vector2 size, float duration = -1, Color? color = null, int segments = 16)
    {
        Color capsuleColor = color ?? Color.white;
        Action action = () =>
        {
            Gizmos.color = capsuleColor;
            float radius = Mathf.Min(size.x, size.y) * 0.5f;
            float height = Mathf.Max(size.x, size.y);
            float halfLength = (height - radius * 2) * 0.5f;
            
            bool isVertical = size.y > size.x;
            Vector3 offset = isVertical ? new Vector3(0, halfLength, 0) : new Vector3(halfLength, 0, 0);
            
            Vector3 top = center + offset;
            Vector3 bottom = center - offset;
            
            // 绘制两个半圆
            float angleStep = 180f / segments;
            Vector3 prevTop = top + (isVertical ? new Vector3(radius, 0, 0) : new Vector3(0, radius, 0));
            Vector3 prevBottom = bottom + (isVertical ? new Vector3(-radius, 0, 0) : new Vector3(0, -radius, 0));
            
            for (int i = 1; i <= segments; i++)
            {
                float angle = angleStep * i * Mathf.Deg2Rad;
                Vector3 newTop, newBottom;
                
                if (isVertical)
                {
                    newTop = top + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
                    newBottom = bottom + new Vector3(-Mathf.Cos(angle) * radius, -Mathf.Sin(angle) * radius, 0);
                }
                else
                {
                    newTop = top + new Vector3(Mathf.Sin(angle) * radius, Mathf.Cos(angle) * radius, 0);
                    newBottom = bottom + new Vector3(-Mathf.Sin(angle) * radius, -Mathf.Cos(angle) * radius, 0);
                }

                Debug.DrawLine(prevTop, newTop);
                Debug.DrawLine(prevBottom, newBottom);
                prevTop = newTop;
                prevBottom = newBottom;
            }
            
            // 绘制两条连线
            if (isVertical)
            {
                Debug.DrawLine(top + new Vector3(radius, 0, 0), bottom + new Vector3(radius, 0, 0));
                Debug.DrawLine(top + new Vector3(-radius, 0, 0), bottom + new Vector3(-radius, 0, 0));
            }
            else
            {
                Debug.DrawLine(top + new Vector3(0, radius, 0), bottom + new Vector3(0, radius, 0));
                Debug.DrawLine(top + new Vector3(0, -radius, 0), bottom + new Vector3(0, -radius, 0));
            }
        };
        Instance.DrawGizmos(action, duration);
    }

    /// <summary>
    /// 绘制2D网格
    /// </summary>
    public static void DrawGrid(Vector3 center, Vector2 size, int cellsX, int cellsY, float duration = -1, Color? color = null)
    {
        Color gridColor = color ?? Color.gray;
        Action action = () =>
        {
            Gizmos.color = gridColor;
            Vector3 halfSize = new Vector3(size.x * 0.5f, size.y * 0.5f, 0);
            Vector3 start = center - halfSize;
            
            float cellWidth = size.x / cellsX;
            float cellHeight = size.y / cellsY;
            
            // 绘制垂直线
            for (int i = 0; i <= cellsX; i++)
            {
                Vector3 from = start + new Vector3(i * cellWidth, 0, 0);
                Vector3 to = from + new Vector3(0, size.y, 0);
                Debug.DrawLine(from, to);
            }
            
            // 绘制水平线
            for (int i = 0; i <= cellsY; i++)
            {
                Vector3 from = start + new Vector3(0, i * cellHeight, 0);
                Vector3 to = from + new Vector3(size.x, 0, 0);
                Debug.DrawLine(from, to);
            }
        };
        Instance.DrawGizmos(action, duration);
    }

    /// <summary>
    /// 绘制坐标轴
    /// </summary>
    public static void DrawAxis(Vector3 position, float length = 1.0f, float duration = -1)
    {
        Action action = () =>
        {
            // X轴 - 红色
            Gizmos.color = Color.red;
            Debug.DrawLine(position, position + Vector3.right * length);
            
            // Y轴 - 绿色
            Gizmos.color = Color.green;
            Debug.DrawLine(position, position + Vector3.up * length);
            
            // Z轴 - 蓝色
            Gizmos.color = Color.blue;
            Debug.DrawLine(position, position + Vector3.forward * length);
        };
        Instance.DrawGizmos(action, duration);
    }

    /// <summary>
    /// 绘制带方向的矩形（显示朝向）
    /// </summary>
    public static void DrawOrientedRect(Vector3 center, Vector2 size, Vector2 direction, float duration = -1, Color? color = null)
    {
        DrawRectangle(center, size, direction, duration, color);
        DrawArrow(center, center + (Vector3)direction.normalized * (size.x * 0.5f), duration, Color.yellow, 0.2f);
    }

    /// <summary>
    /// 绘制BoxCollider2D范围
    /// </summary>
    public static void DrawBoxCollider2D(BoxCollider2D collider, float duration = -1, Color? color = null)
    {
        if (collider == null) return;
        
        Vector3 center = collider.transform.TransformPoint(collider.offset);
        Vector2 size = collider.size * (Vector2)collider.transform.lossyScale;
        Vector2 direction = collider.transform.right;
        
        DrawRectangle(center, size, direction, duration, color);
    }

    /// <summary>
    /// 绘制CircleCollider2D范围
    /// </summary>
    public static void DrawCircleCollider2D(CircleCollider2D collider, float duration = -1, Color? color = null)
    {
        if (collider == null) return;
        
        Vector3 center = collider.transform.TransformPoint(collider.offset);
        float radius = collider.radius * Mathf.Max(collider.transform.lossyScale.x, collider.transform.lossyScale.y);
        
        DrawCircle(center, radius, duration, color);
    }

    // ==================== 原有方法保持不变 ====================

    /// <summary>
    /// 绘制贝塞尔曲线
    /// </summary>
    public static void DrawBezier(Vector3 startPoint, Vector3 controlPoint1, Vector3 endPoint, float duration = 1.0f, Color? color = null, bool showControlPoints = true)
    {
        Color bezierColor = color ?? Color.white;
        Action task = () =>
        {
            Gizmos.color = bezierColor;
            var segments = 20;
            var prevPoint = startPoint;

            for (var i = 1; i <= segments; i++)
            {
                var t = i / (float)segments;
                var point = MathExtension.QuadraticBezierPoint(t,
                    startPoint,
                    controlPoint1,
                    endPoint);

                Debug.DrawLine(prevPoint, point);
                prevPoint = point;
            }

            if (showControlPoints)
            {
                
                Debug.DrawLine(startPoint, controlPoint1, Color.gray);
                Debug.DrawLine(controlPoint1, endPoint, Color.gray);

                // 绘制控制点
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(controlPoint1, 0.1f);
            }
        };

        Instance.DrawGizmos(task, duration);
    }

    public static void AddTriangle(Vector3[] vertices, int[] triangles, int idx, Vector3 a, Vector3 b, Vector3 c)
    {
        vertices[idx * 3] = a;
        vertices[idx * 3 + 1] = b;
        vertices[idx * 3 + 2] = c;
        triangles[idx * 3] = idx * 3;
        triangles[idx * 3 + 1] = idx * 3 + 1;
        triangles[idx * 3 + 2] = idx * 3 + 2;
    }

    public static Mesh GenerateSectionMesh(float radius, float angle)
    {
        var segments = 10;
        var mesh = new Mesh();

        var vertexCount = segments + 2;
        var vertices = new Vector3[vertexCount];
        var triangles = new int[segments * 3];

        // Center vertex
        vertices[0] = Vector3.zero;

        // Calculate vertices
        var angleStep = angle / segments;
        for (var i = 0; i <= segments; i++)
        {
            var currentAngle = Mathf.Deg2Rad * (angleStep * i);
            vertices[i + 1] = new Vector3(Mathf.Cos(currentAngle) * radius, 0, Mathf.Sin(currentAngle) * radius);
        }

        // Calculate triangles
        for (var i = 0; i < segments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        return mesh;
    }
}
