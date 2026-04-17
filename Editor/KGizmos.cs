using System;
using System.Collections.Generic;
using Framework.Foundation;
using UnityEditor;
using UnityEngine;

public class DrawGizmosElement
{
    public Action OnGizmos;
    public Matrix4x4 World;
    public Color GizmosColor;
    public float Time;
};