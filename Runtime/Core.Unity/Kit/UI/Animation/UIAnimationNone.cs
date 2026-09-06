using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 空动画：无任何过渡效果，立即完成。
/// 用于临时抑制面板动画（如切换到 Companion 模式前瞬间关闭已有面板）。
/// </summary>
public class UIAnimationNone : UIAnimation
{
    [LabelText("动画时长")]
    public new float Duration = 0f;

    public override UniTask PlayOpenAsync(CanvasGroup canvasGroup, CancellationToken ct = default)
    {
        if (canvasGroup) canvasGroup.alpha = 1f;
        return UniTask.CompletedTask;
    }

    public override UniTask PlayCloseAsync(CanvasGroup canvasGroup, CancellationToken ct = default)
    {
        if (canvasGroup) canvasGroup.alpha = 0f;
        return UniTask.CompletedTask;
    }
}
