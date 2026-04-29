using System.Threading;
using Cysharp.Threading.Tasks;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 基于 MMF_Player 的 UI 动画。
/// 对应 UIPanel 上的 ShowFx / HideFx，也可手动创建并赋给 PanelAnimation。
/// 与 UIAnimationFade 同级，可按需替换。
/// </summary>
public class UIAnimationMMF : UIAnimation
{
    [LabelText("显示反馈")]
    public MMF_Player ShowPlayer { get; set; }

    [LabelText("隐藏反馈")]
    public MMF_Player HidePlayer { get; set; }

    public UIAnimationMMF() { }

    public UIAnimationMMF(MMF_Player showPlayer, MMF_Player hidePlayer)
    {
        ShowPlayer = showPlayer;
        HidePlayer = hidePlayer;
    }

    public override async UniTask PlayShowAsync(CanvasGroup canvasGroup, CancellationToken ct = default)
    {
        if (ShowPlayer == null) return;
        ShowPlayer.PlayFeedbacks();
        await UniTask.WaitUntil(() => !ShowPlayer.IsPlaying, cancellationToken: ct);
    }

    public override async UniTask PlayHideAsync(CanvasGroup canvasGroup, CancellationToken ct = default)
    {
        if (HidePlayer == null) return;
        HidePlayer.PlayFeedbacks();
        await UniTask.WaitUntil(() => !HidePlayer.IsPlaying, cancellationToken: ct);
    }
}
