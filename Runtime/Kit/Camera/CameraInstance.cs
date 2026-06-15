using System;
using System.Collections.Generic;
using DG.Tweening;

using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Camera))]
public class CameraInstance : InstanceBehaviour<CameraInstance>
{
    public Transform Target; // 目标对象的 Transform
    public Camera BehaviourInstance;

    [SerializeField] private float fadeSpeed = 2f; // 透明度变化速度
    [SerializeField] private float transparentAlpha = 0.3f; // 透明时的 Alpha 值

    private readonly float originalAlpha = 1f; // 原始 Alpha 值
    private readonly float raycastDistanceBuffer = 0.1f; // 射线检测距离缓冲
    private readonly LayerMask layerMask; // 可自定义 LayerMask，当前检测所有层

    // 存储当前和上一次检测到的 MeshRenderer 列表
    private readonly HashSet<Renderer> currentObstacles = new();
    private readonly HashSet<Renderer> previousObstacles = new();

    protected override void OnReplace()
    {
        base.OnReplace();
        BehaviourInstance.fieldOfView = GetComponent<Camera>().fieldOfView;
        BehaviourInstance.clearFlags = GetComponent<Camera>().clearFlags;
        BehaviourInstance.backgroundColor = GetComponent<Camera>().backgroundColor;
    }

    private void Update()
    {
        if (BehaviourInstance == null)
        {
            Debug.LogWarning("Main Camera 或 Target 未设置！");
            return;
        }

        if (Target == null)
        {
            return;
        }
        currentObstacles.Clear();

        // 获取摄像机到目标的方向和距离
        var direction = Target.position - BehaviourInstance.transform.position;
        var distance = direction.magnitude;

        // 进行射线检测
        var ray = new Ray(BehaviourInstance.transform.position, direction.normalized); // 可自定义 LayerMask
        {
            var hits = Physics.RaycastAll(ray, distance + raycastDistanceBuffer, LayerMask.GetMask("EnvUnit"), QueryTriggerInteraction.Collide);
            foreach (var hit in hits)
            {
                // 获取碰撞对象的 MeshRenderer
                var env = hit.collider.GetComponent<UnitBase>();
                // if (env)
                // {
                //     if(env.fracturedObject)
                //         currentObstacles.Add(env.fracturedObject.SingleMeshObject.GetComponent<Renderer>());
                //     foreach (var renderer in env.Renderers)
                //     {
                //         if (renderer != null && hit.collider.transform != Target)
                //         {
                //             currentObstacles.Add(renderer);
                //         }                
                //     }                
                // }
            }
        }
        {
            var hits = Physics.RaycastAll(ray, distance + raycastDistanceBuffer, LayerMask.GetMask("Wall"), QueryTriggerInteraction.Collide);
            foreach (var hit in hits)
            {
                // 获取碰撞对象的 MeshRenderer
                
                var rendererer = hit.collider.GetComponentsInChildren<Renderer>();
                foreach (var renderer in rendererer)
                {
                    if (renderer != null && hit.collider.transform != Target)
                    {
                        currentObstacles.Add(renderer);
                    }                
                } 
            }
        }

        

        // 处理新检测到的障碍物（变为透明）
        foreach (var renderer in currentObstacles)
        {
            SetMaterialTransparent(renderer, true);
        }

        // 处理不再是障碍物的对象（恢复不透明）
        foreach (var renderer in previousObstacles)
        {
            if (!currentObstacles.Contains(renderer))
            {
                SetMaterialTransparent(renderer, false);
            }
        }

        // 更新 previousObstacles 为当前帧的障碍物
        previousObstacles.Clear();
        foreach (var renderer in currentObstacles)
        {
            previousObstacles.Add(renderer);
        }
    }

    

      // 保存材质原始状态
    private class MaterialState
    {
        public int SrcBlend, DstBlend, ZWrite, RenderQueue;
        public bool AlphaBlendOn, AlphaTestOn;
        public Color OriginalColor;
        public MaterialState(Material m)
        {
            SrcBlend = m.GetInt("_SrcBlend");
            DstBlend = m.GetInt("_DstBlend");
            ZWrite = m.GetInt("_ZWrite");
            RenderQueue = m.renderQueue;
            AlphaBlendOn = m.IsKeywordEnabled("_ALPHABLEND_ON");
            AlphaTestOn = m.IsKeywordEnabled("_ALPHATEST_ON");
            OriginalColor = m.color;
        }
    }

private Dictionary<Material, MaterialState> materialStates = new();
private Dictionary<Material, Sequence> materialSequences = new();

private void SetMaterialTransparent(Renderer renderer, bool transparent)
{
    foreach (var material in renderer.materials)
    {
        // 停止之前的动画
        if (materialSequences.TryGetValue(material, out var seq)) seq.Kill();

        if (transparent)
        {
            // 保存原始状态
            if (!materialStates.ContainsKey(material))
                materialStates[material] = new MaterialState(material);

            // 设置为透明
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.renderQueue = 3000;
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");

            var newColor = new Color(material.color.r, material.color.g, material.color.b, transparentAlpha);
            var sequence = DOTween.Sequence();
            sequence.Append(material.DOColor(newColor, 0.5f));
            materialSequences[material] = sequence;
            sequence.Play();
        }
        else if (materialStates.ContainsKey(material))
        {
            // 恢复原始状态
            var state = materialStates[material];
            var sequence = DOTween.Sequence();
            sequence.Append(material.DOColor(state.OriginalColor, 0.5f));
            sequence.AppendCallback(() =>
            {
                material.SetInt("_SrcBlend", state.SrcBlend);
                material.SetInt("_DstBlend", state.DstBlend);
                material.SetInt("_ZWrite", state.ZWrite);
                material.renderQueue = state.RenderQueue;
                if (state.AlphaBlendOn) material.EnableKeyword("_ALPHABLEND_ON");
                else material.DisableKeyword("_ALPHABLEND_ON");
                if (state.AlphaTestOn) material.EnableKeyword("_ALPHATEST_ON");
                else material.DisableKeyword("_ALPHATEST_ON");
                materialStates.Remove(material);
            });
            materialSequences[material] = sequence;
            sequence.Play();
        }
    }
}
}