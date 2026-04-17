using UnityEngine;

namespace Framework.Foundation
{
    public static class MathExtension
    {

        /// <summary>
        /// 计算一个Collider和一个Box的最近接触点
        /// </summary>
        /// <param name="selfCollider">自身的Collider</param>
        /// <param name="boxCenter">Box的中心点</param>
        /// <param name="boxHalfExtents">Box的半尺寸</param>
        /// <param name="boxOrientation">Box的方向</param>
        /// <param name="intersectionPoint">输出的交点</param>
        /// <returns>如果相交则返回true</returns>
        public static bool TryGetIntersectionPointWithBox(Collider selfCollider, Vector3 boxCenter, Vector3 boxHalfExtents, Quaternion boxOrientation, out Vector3 intersectionPoint)
        {
            // 1. 检查两者是否真的重叠
            if (Physics.CheckBox(boxCenter, boxHalfExtents, boxOrientation, selfCollider.gameObject.layer, QueryTriggerInteraction.Ignore))
            {
                // 2. 如果重叠，计算在自身Collider上离Box中心最近的点
                intersectionPoint = selfCollider.ClosestPoint(boxCenter);
                return true;
            }

            intersectionPoint = Vector3.zero;
            return false;
        }

        public static bool TryGetIntersectionPoint(Collider selfCollider, Vector3 boxCenter, out Vector3 intersectionPoint)
        {
            // 2. 如果重叠，计算在自身Collider上离Box中心最近的点
            intersectionPoint = selfCollider.ClosestPoint(boxCenter);
            return true;
        }

        public static float NormalizeAngle(float lfAngle)
        {
            lfAngle = lfAngle % 360;
            return lfAngle;
        }

        public static float GetAngleInXZ(Vector3 direction)
        {
            direction.y = 0;
            if (direction == Vector3.zero)
            {
                direction = Vector3.forward;
            }

            direction.Normalize();

            var angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            return angle < 0 ? angle + 360 : angle;
        }

        public static float ClampAngle(float currentAngle, float minAngle, float maxAngle)
        {
            var wrapAround = Mathf.Abs(maxAngle - minAngle) > 180f;
            var isWithinRange = wrapAround
                ? currentAngle >= minAngle || currentAngle <= maxAngle
                : currentAngle >= minAngle && currentAngle <= maxAngle;

            // 超出范围时修正角度
            if (!isWithinRange)
            {
                var deltaToMin = Mathf.Abs(Mathf.DeltaAngle(currentAngle, minAngle));
                var deltaToMax = Mathf.Abs(Mathf.DeltaAngle(currentAngle, maxAngle));

                currentAngle = deltaToMin < deltaToMax ? minAngle : maxAngle;
            }

            return currentAngle;
        }

        public static Vector3 QuadraticBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            var u = 1 - t;
            var tt = t * t;
            var uu = u * u;

            return uu * p0 + 2 * u * t * p1 + tt * p2;
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
            var acc = 2 * distance / t / t;
            return acc;
        }

        public static float CalculateSpeed(float distance, float t)
        {
            var acc = 2 * distance / t / t;
            return t * acc;
        }

        public static Vector3 RotateDirectionY(Vector3 direction, float angle)
        {
            direction.Normalize();
            var rotatedDirection = Quaternion.Euler(0, angle, 0) * direction;
            rotatedDirection.Normalize();
            return rotatedDirection;
        }

        public static Vector3 RandomPointInCircle(Vector3 central, Vector3 length)
        {
            return central + new Vector3(Random.Range(-length.x, length.x), Random.Range(-length.y, length.y),
                Random.Range(-length.z, length.z));
        }

        public static Vector3 projectedOnPlane(this Vector3 thisVector, Vector3 planeNormal)
        {
            return Vector3.ProjectOnPlane(thisVector, planeNormal);
        }

        public static bool IsInSection(Vector3 origin, Vector3 point, Vector3 direction, float sectorAngle,
            float sectorRadius)
        {
            //点乘积结果
            var dot = Vector3.Dot(direction.normalized, (point - origin).normalized);
            //反余弦计算角度
            var offsetAngle = Mathf.Acos(dot) * Mathf.Rad2Deg; //弧度转度
            return offsetAngle < sectorAngle * .5f && direction.magnitude < sectorRadius;
        }
    }
}