using System;
using UnityEngine;

namespace KFramework
{
public class DrawGizmosElement
{
    public Action OnGizmos;
    public Matrix4x4 World;
    public Color GizmosColor;
    public float Time;
};
}
