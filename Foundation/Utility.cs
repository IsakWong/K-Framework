using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Framework.Foundation
{
    public class MathUtility
    {
        public static float NormalizeAngle(float lfAngle)
        {
            lfAngle = lfAngle % 360;
            return lfAngle;
        }
        public static float GetAngleInXZ(Vector3 direction)
        {
            direction.y = 0;
            if (direction == Vector3.zero) direction = Vector3.forward;
            direction.Normalize();
        
            float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            return angle < 0 ? angle + 360 : angle;
        }
        public static float ClampAngle(float currentAngle , float minAngle, float maxAngle)
        {
            bool wrapAround = Mathf.Abs(maxAngle - minAngle) > 180f;
            bool isWithinRange = wrapAround ? 
                (currentAngle >= minAngle || currentAngle <= maxAngle) :
                (currentAngle >= minAngle && currentAngle <= maxAngle);

            // 超出范围时修正角度
            if (!isWithinRange)
            {
                float deltaToMin = Mathf.Abs(Mathf.DeltaAngle(currentAngle, minAngle));
                float deltaToMax = Mathf.Abs(Mathf.DeltaAngle(currentAngle, maxAngle));
    
                currentAngle = deltaToMin < deltaToMax ? minAngle : maxAngle;
            }
            return currentAngle;
        }
        public static Vector3 Bezier(float t, Vector3 a, Vector3 b, Vector3 c)
        {
            var ab = Vector3.Lerp(a, b, t);
            var bc = Vector3.Lerp(b, c, t);
            return Vector3.Lerp(ab, bc, t);
        }

        public static Vector3 RandomDirection()
        {
            return Random.insideUnitSphere.normalized;
        }

        //
        public static float CaclulateAcc(float distance, float t)
        {
            float acc = (2 * distance) / t / t;
            return acc;
        }

        public static float CalculateSpeed(float distance, float t)
        {
            float acc = (2 * distance) / t / t;
            return t * acc;
        }

        public static Vector3 RotateDirectionY(Vector3 direction, float angle)
        {
            direction.Normalize();
            Vector3 rotatedDirection = Quaternion.Euler(0, angle, 0) * direction;
            rotatedDirection.Normalize();
            return rotatedDirection;
        }

        public static Vector3 RandomPointInCircle(Vector3 central, Vector3 length)
        {
            return central + new Vector3(Random.Range(-length.x, length.x), Random.Range(-length.y, length.y),
                Random.Range(-length.z, length.z));
        }
    }
}

public static class Utility
{
    public static bool IsInSection(Vector3 origin, Vector3 point, Vector3 direction, float sectorAngle,
        float sectorRadius)
    {
        //点乘积结果
        float dot = Vector3.Dot(direction.normalized, (point - origin).normalized);
        //反余弦计算角度
        float offsetAngle = Mathf.Acos(dot) * Mathf.Rad2Deg; //弧度转度
        return offsetAngle < sectorAngle * .5f && direction.magnitude < sectorRadius;
    }

    public static Vector3 DetectGround(Vector3 position, int mask, float maxDistance = 10)
    {
        Ray ray = new Ray(position + Vector3.up * 0.5f, Vector3.down);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, maxDistance, mask))
        {
            var newLocation = hit.point;
            return newLocation;
        }

        return position;
    }

    public static Vector3 DetectGround(this Transform trans, int mask)
    {
        Ray ray = new Ray(trans.position + Vector3.up * 0.5f, Vector3.down);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 10, mask))
        {
            var newLocation = hit.point;
            return newLocation;
        }

        return trans.position;
    }

    public static bool SafeAccess<T>(this List<T> self, int idx, out T def)
    {
        if (idx < self.Count && idx >= 0)
        {
            def = self[idx];
            return true;
        }

        def = default(T);
        return false;
    }

    public static T RandomAccess<T>(this List<T> self) where T : class
    {
        if (self.Count == 0)
            return null;
        int index = Random.Range(0, self.Count);
        return self[index];
    }

    public static T RandomAccessStruct<T>(this List<T> self) where T : struct
    {
        if (self.Count == 0)
            return new T();
        int index = Random.Range(0, self.Count);
        return self[index];
    }

    static public Rect GetWorldRect(RectTransform rt)
    {
        // Convert the rectangle to world corners and grab the top left
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        Vector3 topLeft = corners[0];

        // Rescale the size appropriately based on the current Canvas scale
        Vector2 scaledSize = new Vector2(rt.rect.size.x, rt.rect.size.y);

        return new Rect(topLeft, scaledSize);
    }

    public static T GetOrAddComponent<T>(this MonoBehaviour self) where T : MonoBehaviour
    {
        if (self.GetComponent<T>() == null)
            return self.gameObject.AddComponent<T>();
        return self.GetComponent<T>();
    }
}