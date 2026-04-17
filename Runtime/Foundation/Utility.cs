
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

[Serializable]
public class DoubleArray<T> : System.Object
{
    public int height, width;
    public T[] data;
    public DoubleArray(int width, int height)
        : this (width*height)
    {
        this.width = width;
        this.height = height;
        data = new T[width * height];
    }
    public DoubleArray(int total)
        //: base(width*height)
    {
        data = new T[total];
    }
    public T this[int x, int y]
    {
        get
        {
            return data[y * width + x];
        }
        set
        {
            data[y * width + x] = value;
        }
    }
    public T this[int index]
    {
        get
        {
            return data[index];
        }
        set
        {
            data[index] = value;
        }
    }

    /// <summary>
    /// 获取指定维度的长度
    /// </summary>
    /// <param name="dimension">维度索引 (0 = 行数/height, 1 = 列数/width)</param>
    /// <returns>指定维度的长度</returns>
    public int GetLength(int dimension)
    {
        if (dimension == 0)
            return width;
        else if (dimension == 1)
            return height ;
        else
            throw new IndexOutOfRangeException($"Dimension {dimension} is out of range. Valid dimensions are 0 and 1.");
    }

    /// <summary>
    /// 获取数组的总元素数量
    /// </summary>
    public int Length => data.Length;

    /// <summary>
    /// 获取数组的秩（维度数），对于 DoubleArray 总是返回 2
    /// </summary>
    public int Rank => 2;

    /// <summary>
    /// 检查索引是否在有效范围内
    /// </summary>
    public bool IsValidIndex(int row, int column)
    {
        return row >= 0 && row < width && column >= 0 && column < height;
    }

    /// <summary>
    /// 清空数组（将所有元素设置为默认值）
    /// </summary>
    public void Clear()
    {
        Array.Clear(data, 0, data.Length);
    }

    /// <summary>
    /// 填充数组（将所有元素设置为指定值）
    /// </summary>
    public void Fill(T value)
    {
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = value;
        }
    }
}

public static class Utility
{
    #region Vector Conversion Extensions

    // ========== Vector3 转换 ==========
    
    /// <summary>
    /// Vector3 转 Vector3Int (四舍五入)
    /// </summary>
    public static Vector3Int ToVector3Int(this Vector3 a)
    {
        return new Vector3Int(Mathf.RoundToInt(a.x), Mathf.RoundToInt(a.y), Mathf.RoundToInt(a.z));
    }

    /// <summary>
    /// Vector3 转 Vector2 (丢弃 z)
    /// </summary>
    public static Vector2 ToVector2(this Vector3 a)
    {
        return new Vector2(a.x, a.y);
    }

    /// <summary>
    /// Vector3 转 Vector2Int (四舍五入，丢弃 z)
    /// </summary>
    public static Vector2Int ToVector2Int(this Vector3 a)
    {
        return new Vector2Int(Mathf.RoundToInt(a.x), Mathf.RoundToInt(a.y));
    }

    // ========== Vector3Int 转换 ==========
    
    /// <summary>
    /// Vector3Int 转 Vector3
    /// </summary>
    public static Vector3 ToVector3(this Vector3Int a)
    {
        return new Vector3(a.x, a.y, a.z);
    }

    /// <summary>
    /// Vector3Int 转 Vector2 (丢弃 z)
    /// </summary>
    public static Vector2 ToVector2(this Vector3Int a)
    {
        return new Vector2(a.x, a.y);
    }

    /// <summary>
    /// Vector3Int 转 Vector2Int (丢弃 z)
    /// </summary>
    public static Vector2Int ToVector2Int(this Vector3Int a)
    {
        return new Vector2Int(a.x, a.y);
    }

    // ========== Vector2 转换 ==========
    
    /// <summary>
    /// Vector2 转 Vector3 (z = 0)
    /// </summary>
    public static Vector3 ToVector3(this Vector2 a, float z = 0)
    {
        return new Vector3(a.x, a.y, z);
    }

    /// <summary>
    /// Vector2 转 Vector3Int (四舍五入，z = 0)
    /// </summary>
    public static Vector3Int ToVector3Int(this Vector2 a, int z = 0)
    {
        return new Vector3Int(Mathf.RoundToInt(a.x), Mathf.RoundToInt(a.y), z);
    }

    /// <summary>
    /// Vector2 转 Vector2Int (四舍五入)
    /// </summary>
    public static Vector2Int ToVector2Int(this Vector2 a)
    {
        return new Vector2Int(Mathf.RoundToInt(a.x), Mathf.RoundToInt(a.y));
    }

    // ========== Vector2Int 转换 ==========
    
    /// <summary>
    /// Vector2Int 转 Vector2
    /// </summary>
    public static Vector2 ToVector2(this Vector2Int a)
    {
        return new Vector2(a.x, a.y);
    }

    /// <summary>
    /// Vector2Int 转 Vector3 (z = 0)
    /// </summary>
    public static Vector3 ToVector3(this Vector2Int a, float z = 0)
    {
        return new Vector3(a.x, a.y, z);
    }

    /// <summary>
    /// Vector2Int 转 Vector3Int (z = 0)
    /// </summary>
    public static Vector3Int ToVector3Int(this Vector2Int a, int z = 0)
    {
        return new Vector3Int(a.x, a.y, z);
    }

    // ========== 特殊转换（Floor, Ceil） ==========
    
    /// <summary>
    /// Vector3 转 Vector3Int (向下取整)
    /// </summary>
    public static Vector3Int FloorToInt(this Vector3 a)
    {
        return new Vector3Int(Mathf.FloorToInt(a.x), Mathf.FloorToInt(a.y), Mathf.FloorToInt(a.z));
    }

    /// <summary>
    /// Vector3 转 Vector3Int (向上取整)
    /// </summary>
    public static Vector3Int CeilToInt(this Vector3 a)
    {
        return new Vector3Int(Mathf.CeilToInt(a.x), Mathf.CeilToInt(a.y), Mathf.CeilToInt(a.z));
    }

    /// <summary>
    /// Vector2 转 Vector2Int (向下取整)
    /// </summary>
    public static Vector2Int FloorToInt(this Vector2 a)
    {
        return new Vector2Int(Mathf.FloorToInt(a.x), Mathf.FloorToInt(a.y));
    }

    /// <summary>
    /// Vector2 转 Vector2Int (向上取整)
    /// </summary>
    public static Vector2Int CeilToInt(this Vector2 a)
    {
        return new Vector2Int(Mathf.CeilToInt(a.x), Mathf.CeilToInt(a.y));
    }

    #endregion

    public static Vector3 NormalizedInXY(this Vector3 direction)
    {
        direction.z = 0;
        if(direction.x == 0 && direction.y == 0)
            direction.x = 1;
        return direction.normalized;
    } 
    public static Camera GetCamera(Canvas _canvas)
    {
        if (_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return _canvas.worldCamera;
    }
    public static Vector3 DirectionBetweenUnit(this UnitBase a, UnitBase b, bool ignoreY = false)
    {
        var delta = b.transform.position - a.transform.position;
        if (ignoreY)
        {
            delta.y = 0;
        }

        delta.Normalize();
        return delta;
    }

    public static float DistanceBetweenPosition(Vector3 a, Vector3 b, bool ignoreY = true)
    {
        var delta = a - b;
        if (ignoreY)
        {
            delta.y = 0;
        }

        return delta.magnitude;
    }

    public static float DistanceBetweenGameUnit(this UnitBase a, UnitBase b, bool ignoreY = true)
    {
        var delta = a.transform.position - b.transform.position;
        if (ignoreY)
        {
            delta.y = 0;
        }

        return delta.magnitude;
    }


    public static List<T> GetRandomElements<T>(List<T> list, int count)
    {
        if (list == null || list.Count < count || count < 0)
        {
            throw new ArgumentException("Invalid list or count");
        }

        var random = new System.Random();
        var copy = new List<T>(list);
        for (var i = 0; i < count; i++)
        {
            var j = random.Next(i, copy.Count);
            (copy[i], copy[j]) = (copy[j], copy[i]);
        }

        return copy.GetRange(0, count);
    }

    public static bool DetectWalkable(Vector3 position, float maxDistance, out Vector3 result)
    {
        var ray = new Ray(position + Vector3.up * 0.5f, Vector3.down);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, maxDistance, LayerMask.GetMask("Ground")))
        {
            result = hit.point;
            if (NavMesh.SamplePosition(result, out var newHit, 1.0f, NavMesh.AllAreas))
            {
                result = newHit.position;
                return true;
            }
        }

        result = position;
        return false;
    }

    public static Vector3 DetectGround(Vector3 position, float maxDistance = 3)
    {
        var ray = new Ray(position + Vector3.up * 0.5f, Vector3.down);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxDistance, LayerMask.GetMask("Ground")))
        {
            var newLocation = hit.point;
            return newLocation;
        }
        else
        {
            ray = new Ray(position - Vector3.up * 0.5f, Vector3.up);
            if(Physics.Raycast(ray, out hit, maxDistance, LayerMask.GetMask("Ground")))
            {
                var newLocation = hit.point;
                return newLocation;
            }
        }

        return position;
    }

    public static bool DrawGizmos = true;

    public static bool SafeAccess<T>(this List<T> self, int idx, out T def)
    {
        if (idx < self.Count && idx >= 0)
        {
            def = self[idx];
            return true;
        }

        def = default;
        return false;
    }

    public static T RandomAccess<T>(this List<T> self) where T : class
    {
        if (self.Count == 0)
        {
            return null;
        }

        var index = Random.Range(0, self.Count);
        return self[index];
    }

    public static T RandomAccessStruct<T>(this List<T> self) where T : struct
    {
        if (self.Count == 0)
        {
            return new T();
        }

        var index = Random.Range(0, self.Count);
        return self[index];
    }

    public static Rect GetWorldRect(RectTransform rt)
    {
        // Convert the rectangle to world corners and grab the top left
        var corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        var topLeft = corners[0];

        // Rescale the size appropriately based on the current Canvas scale
        var scaledSize = new Vector2(rt.rect.size.x, rt.rect.size.y);

        return new Rect(topLeft, scaledSize);
    }

    private static Collider[] s_ColliderCache = new Collider[64];
    /// <summary>
    /// Efficiently finds components of type T on objects within a sphere.
    /// </summary>
    /// <typeparam name="T">The component type to find.</typeparam>
    /// <param name="pos">The center of the sphere.</param>
    /// <param name="radius">The radius of the sphere.</param>
    /// <param name="mask">A LayerMask that is used to selectively ignore Colliders when casting.</param>
    /// <param name="result">The list to populate with found components. It will be cleared first.</param>
    /// <param name="action">An optional action to perform on each found component.</param>
    /// <param name="cond">An optional condition to filter which components are added.</param>

    public static List<T> SelectComponentSphere<T>(Vector3 pos, float radius, int mask, out List<T> result, Action<T> action = null, Func<T, bool> cond = null)
    {
        // 2. Ensure the provided list is not null and clear it to reuse its memory.
        result = new List<T>();
        Array.Clear(s_ColliderCache,0 , s_ColliderCache.Length);
        // 3. Perform the query. The method returns the number of colliders found.
        int hitCount = Physics.OverlapSphereNonAlloc(pos, radius, s_ColliderCache, mask, QueryTriggerInteraction.Collide);
        // 4. Process the results. Iterate only up to hitCount.
        for (int i = 0; i < hitCount; i++)
        {
            // Using TryGetComponent is slightly more efficient than GetComponent if the component may be missing.
            if (s_ColliderCache[i].TryGetComponent<T>(out T component))
            {
                // 5. Apply optional condition.
                if (cond == null || cond(component))
                {
                    result?.Add(component);
                    // 6. Apply optional action.
                    action?.Invoke(component);
                }
            }
        }
        return result;
    }

    public static List<T> SelectComponentBox<T>(Vector3 pos, Vector3 radius, Vector3 direction, int mask, out List<T> result, Action<T> action = null, Func<T, bool> cond = null) where T : Component 
    {
        // 2. Ensure the provided list is not null and clear it to reuse its memory.
        result = new List<T>();
        Array.Clear(s_ColliderCache, 0, s_ColliderCache.Length);
        // 3. Perform the query. The method returns the number of colliders found.
        int hitCount = Physics.OverlapBoxNonAlloc(pos, radius, s_ColliderCache, Quaternion.LookRotation(direction), mask, QueryTriggerInteraction.Collide);
        // 4. Process the results. Iterate only up to hitCount.
        for (int i = 0; i < hitCount; i++)
        {
            // Using TryGetComponent is slightly more efficient than GetComponent if the component may be missing.
            if (s_ColliderCache[i].TryGetComponent<T>(out T component))
            {
                // 5. Apply optional condition.
                if (cond == null || cond(component))
                {
                    result.Add(component);
                    // 6. Apply optional action.
                    action?.Invoke(component);
                }
            }
        }
        return result;
    }

    public static T GetOrAddComponent<T>(this MonoBehaviour self) where T : MonoBehaviour
    {
        if (self.GetComponent<T>() == null)
        {
            return self.gameObject.AddComponent<T>();
        }

        return self.GetComponent<T>();
    }

    /// <summary>
    /// 对 Transform 的所有子物体执行指定操作
    /// </summary>
    /// <param name="parent">父 Transform</param>
    /// <param name="action">要执行的操作</param>
    public static void ForEachChild(this Transform parent, Action<Transform> action)
    {
        if (parent == null || action == null)
        {
            return;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            action(parent.GetChild(i));
        }
    }

    /// <summary>
    /// 对 Transform 的所有子物体执行指定操作（带索引）
    /// </summary>
    /// <param name="parent">父 Transform</param>
    /// <param name="action">要执行的操作，参数为子物体和索引</param>
    public static void ForEachChild(this Transform parent, Action<Transform, int> action)
    {
        if (parent == null || action == null)
        {
            return;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            action(parent.GetChild(i), i);
        }
    }

    /// <summary>
    /// 对 Transform 的所有子物体执行指定操作，并可以中断遍历
    /// </summary>
    /// <param name="parent">父 Transform</param>
    /// <param name="action">要执行的操作，返回 false 时中断遍历</param>
    public static void ForEachChild(this Transform parent, Func<Transform, bool> action)
    {
        if (parent == null || action == null)
        {
            return;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            if (!action(parent.GetChild(i)))
            {
                break;
            }
        }
    }

    private static Collider2D[] s_Collider2DCache = new Collider2D[64];

    /// <summary>
    /// 在2D圆形范围内查找组件
    /// </summary>
    public static List<T> SelectComponentCircle2D<T>(Vector2 pos, float radius, int mask, out List<T> result, Action<T> action = null, Func<T, bool> cond = null) where T : Component
    {
        result = new List<T>();
        Array.Clear(s_Collider2DCache, 0, s_Collider2DCache.Length);
        int hitCount = Physics2D.OverlapCircleNonAlloc(pos, radius, s_Collider2DCache, mask);
        for (int i = 0; i < hitCount; i++)
        {
            if (s_Collider2DCache[i].TryGetComponent<T>(out T component))
            {
                if (cond == null || cond(component))
                {
                    result.Add(component);
                    action?.Invoke(component);
                }
            }
        }
        return result;
    }

    /// <summary>
    /// 在2D盒形范围内查找组件
    /// </summary>
    public static List<T> SelectComponentBox2D<T>(Vector2 pos, Vector2 size, float angle, int mask, out List<T> result, Action<T> action = null, Func<T, bool> cond = null) where T : Component
    {
        result = new List<T>();
        Array.Clear(s_Collider2DCache, 0, s_Collider2DCache.Length);
        int hitCount = Physics2D.OverlapBoxNonAlloc(pos, size, angle, s_Collider2DCache, mask);
        for (int i = 0; i < hitCount; i++)
        {
            if (s_Collider2DCache[i].TryGetComponent<T>(out T component))
            {
                if (cond == null || cond(component))
                {
                    result.Add(component);
                    action?.Invoke(component);
                }
            }
        }
        return result;
    }

    /// <summary>
    /// 在2D扇形范围内查找组件
    /// </summary>
    /// <typeparam name="T">要查找的组件类型</typeparam>
    /// <param name="pos">扇形的中心点（扇形顶点）</param>
    /// <param name="direction">扇形的朝向</param>
    /// <param name="radius">扇形的半径</param>
    /// <param name="angle">扇形的角度（度数，总角度）</param>
    /// <param name="mask">Layer mask</param>
    /// <param name="result">输出结果列表</param>
    /// <param name="action">对每个找到的组件执行的操作</param>
    /// <param name="cond">过滤条件</param>
    /// <returns>找到的组件列表</returns>
    public static List<T> SelectComponentSector2D<T>(Vector2 pos, Vector2 direction, float radius, float angle, int mask, out List<T> result, Action<T> action = null, Func<T, bool> cond = null) where T : Component
    {
        result = new List<T>();
        Array.Clear(s_Collider2DCache, 0, s_Collider2DCache.Length);
        
        // 先在圆形范围内查找所有碰撞体
        int hitCount = Physics2D.OverlapCircleNonAlloc(pos, radius, s_Collider2DCache, mask);
        
        // 归一化方向向量
        Vector2 normalizedDirection = direction.normalized;
        float halfAngle = angle * 0.5f;
        
        for (int i = 0; i < hitCount; i++)
        {
            if (s_Collider2DCache[i].TryGetComponent<T>(out T component))
            {
                // 计算从中心点到碰撞体的方向
                Vector2 toTarget = (Vector2)s_Collider2DCache[i].transform.position - pos;
                
                // 检查是否在扇形角度范围内
                float angleToTarget = Vector2.Angle(normalizedDirection, toTarget);
                
                if (angleToTarget <= halfAngle)
                {
                    // 在扇形范围内
                    if (cond == null || cond(component))
                    {
                        result.Add(component);
                        action?.Invoke(component);
                    }
                }
            }
        }
        
        return result;
    }
    
    public static RectInt Outter(this RectInt bounds, int top, int bottom, int left, int right)
    {
        return new RectInt(bounds.xMin - left, bounds.yMin - bottom, bounds.width + left + right, bounds.height + top + bottom);
    }
}