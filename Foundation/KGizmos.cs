using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace K1
{
    public class DrawGizmosElement
    {
        public Action OnGizmos;
        public Matrix4x4 World;
        public Color GizmosColor;
        public float Time;
    };

    public class KGizmos : KSingleton<KGizmos>
    {
        public DrawGizmosElement DrawGizmos(Action action, float time)
        {
            DrawGizmosElement element = new DrawGizmosElement();
            element.OnGizmos = action;
            element.Time = time;
            GizmosElements.Add(element);
            return element;
        }

        public void DrawSphere(Vector3 center, float radius, Matrix4x4 matrix, float duration, Color color)
        {
            Action action = () =>
            {
                Gizmos.matrix = matrix;
                Gizmos.color = color;
                Gizmos.DrawSphere(center, radius);
            };
            DrawGizmos(action, duration);
        }

        private List<DrawGizmosElement> GizmosElements = new List<DrawGizmosElement>();

        public static void AddTriangle(Vector3[] vertices, int[] triangles, int idx, Vector3 a, Vector3 b, Vector3 c)
        {
            vertices[idx * 3] = a;
            vertices[idx * 3 + 1] = b;
            vertices[idx * 3 + 2] = c;
            triangles[idx * 3] = idx * 3;
            triangles[idx * 3 + 1] = idx * 3 + 1;
            triangles[idx * 3 + 2] = idx * 3 + 2;
        }

        public void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                for (int i = GizmosElements.Count - 1; i >= 0; i--)
                {
                    if (GizmosElements[i].Time < 0)
                    {
                        GizmosElements.RemoveAt(i);
                        continue;
                    }

                    GizmosElements[i].Time -= Time.fixedDeltaTime;
                    GizmosElements[i].OnGizmos.Invoke();
                }
            }
            else
            {
                GizmosElements.Clear();
            }
        }

        public static Mesh GenerateSectionMesh(float radius, float angle)
        {
            int segments = 10;
            Mesh mesh = new Mesh();

            int vertexCount = segments + 2;
            Vector3[] vertices = new Vector3[vertexCount];
            int[] triangles = new int[segments * 3];

            // Center vertex
            vertices[0] = Vector3.zero;

            // Calculate vertices
            float angleStep = angle / segments;
            for (int i = 0; i <= segments; i++)
            {
                float currentAngle = Mathf.Deg2Rad * (angleStep * i);
                vertices[i + 1] = new Vector3(Mathf.Cos(currentAngle) * radius, 0, Mathf.Sin(currentAngle) * radius);
            }

            // Calculate triangles
            for (int i = 0; i < segments; i++)
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
}