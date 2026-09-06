using UnityEngine;

/// <summary>
///
/// 
/// </summary>
public class VfxAnimator : VfxAnim
{
    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    public override void Spawn()
    {
        if (_animator)
        {
            _animator.SetTrigger("Spawn");
        }
    }

    public override void Die()
    {
        base.Die();
        if (_animator)
        {
            _animator.SetTrigger("Die");
        }
    }
}