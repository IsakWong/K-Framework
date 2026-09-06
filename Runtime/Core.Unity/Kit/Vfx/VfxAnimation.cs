using UnityEngine;


[RequireComponent(typeof(Animation))]
class VfxAnimation : VfxAnim
{
    private Animation compAnimation;
    public AnimationClip clipSpawn;
    public AnimationClip clipDie;
    private void Awake()
    {
        compAnimation = GetComponent<Animation>();
        compAnimation.AddClip(clipSpawn, "Spawn");
        compAnimation.AddClip(clipDie, "Die");
        compAnimation.playAutomatically = false;
    }
    public override void Spawn()
    {
        base.Spawn();
        
        compAnimation.Play("Spawn");
    }

    public override void Die()
    {
        base.Die();
        compAnimation.Play("Die");
    }
}