using System.Collections.Generic;
using DG.Tweening;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
///
/// 
/// </summary>
public class VfxAnimFade : VfxAnim
{
    // Start is called before the first frame update

    [FormerlySerializedAs("mFadeLight")] public bool FadeLight = false;

    [FormerlySerializedAs("mFadeParticle")]
    public bool FadeParticle = false;

    [FormerlySerializedAs("mFadeMesh")] public bool FadeMesh = false;

    [FormerlySerializedAs("mFakdeSkinMesh")]
    public bool FadeSkinned = false;

    public bool ScaleMesh = false;
    public Light FadeLightObject;
    [FormerlySerializedAs("mFadeParent")] public GameObject AnimParent;

    [FormerlySerializedAs("SpawnWhenDieObjects")] [FormerlySerializedAs("mDieObjects")] public List<GameObject> ActiveWhenDie = new();

    public float CrossFadeDuration = 1.0f;
    public RandomAudioClip CorssFadeAudio;

    public TrailRenderer[] TrailRenderers;
    public SkinnedMeshRenderer[] SkinnedMeshRenderers;
    public MeshRenderer[] MeshRenderers;
    public ParticleSystem[] ParticleSystems;
    public GameObject[] DestroyOnDie;
    private void Awake()
    {
        foreach (var UPPER in ActiveWhenDie)
        {
            UPPER.SetActive(false);
        }

        if (AnimParent is null)
        {
            AnimParent = gameObject;
        }
    }

    public override void Spawn()
    {
        if (SpawnDuration > 0)
        {
            if (ScaleMesh)
            {
                var oldScale = gameObject.transform.localScale;
                gameObject.transform.localScale = Vector3.zero;
                gameObject.transform.DOScale(oldScale, SpawnDuration);
            }

            if (FadeLight)
            {
                FadeLightObject.DOIntensity(1.0f, SpawnDuration);
            }

            if (FadeMesh)
            {
                var meshs = AnimParent.GetComponentsInChildren<MeshRenderer>();
                foreach (var mesh in meshs)
                {
                    mesh.material.SetFloat("_Alpha", 0.0f);
                    var color = mesh.material.GetColor("_Color");
                    color.a = 0.0f;
                    mesh.material.SetColor("_Color", color);
                }

                StopAllMeshRenderer(1.0f);
            }
        }
    }

    public override void Die()
    {
        base.Die();
        foreach (var go in DestroyOnDie)
        {
            Destroy(go);
        }
        foreach (var ob in ActiveWhenDie)
        {
            ob.SetActive(true);
        }
        foreach (var lineRenderer in TrailRenderers)
        {
            lineRenderer.emitting = false;
        }
        if (CorssFadeAudio && CorssFadeAudio.soundEmitter)
        {
            CorssFadeAudio.soundEmitter.Stop(CrossFadeDuration);
        }

        if (DieDuration > 0)
        {
            if (FadeLight)
            {
                FadeLightObject.DOIntensity(0.0f, DieDuration);
            }

            if (FadeMesh)
            {
                StopAllMeshRenderer(0.0f);
            }

            if (FadeParticle)
            {
                StopAllParticle();
            }

            if (FadeSkinned)
            {
                StopAllSkinMeshRenderer(0.0f);
            }

            if (ScaleMesh)
            {
                gameObject.transform.DOScale(Vector3.zero, DieDuration);
            }
        }
    }

    protected void StopAllParticle()
    {
        var particles = AnimParent.GetComponentsInChildren<ParticleSystem>();
        foreach (var partcile in particles)
        {
            partcile.Stop();
        }
    }

    protected void StopAllLight(float target)
    {
        var meshs = AnimParent.GetComponentsInChildren<Light>();
        foreach (var mesh in meshs)
        {
        }
    }

    protected void StopAllMeshRenderer(float target)
    {
        var meshs = AnimParent.GetComponentsInChildren<MeshRenderer>();
        foreach (var mesh in meshs)
        {
            mesh.material.DOFloat(target, "_Alpha", DieDuration);
            var color = mesh.material.GetColor("_Color");
            color.a = target;
            mesh.material.DOColor(color, "_Color", DieDuration);
        }
    }

    protected void StopAllSkinMeshRenderer(float target)
    {
        var meshs = AnimParent.GetComponentsInChildren<SkinnedMeshRenderer>();

        foreach (var mesh in meshs)
        foreach (var mat in mesh.materials)
        {
            mat.DOFloat(target, "_Alpha", DieDuration);
            var color = mat.GetColor("_Color");
            color.a = target;
            mat.DOColor(color, "_Color", DieDuration);
        }
    }
}
#if UNITY_EDITOR
[CustomEditor(typeof(VfxAnimFade))]
class VfxAnimFadeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var myScript = (VfxAnimFade) target;
        if (GUILayout.Button("Colloect"))
        {
            VfxAnimFade anim = target as VfxAnimFade;
            if (anim.AnimParent)
            {
                anim.ParticleSystems = anim.AnimParent.GetComponentsInChildren<ParticleSystem>();
                anim.MeshRenderers = anim.AnimParent.GetComponentsInChildren<MeshRenderer>();
                anim.SkinnedMeshRenderers = anim.AnimParent.GetComponentsInChildren<SkinnedMeshRenderer>();
                serializedObject.ApplyModifiedProperties();
            }
        }

    }
}
#endif