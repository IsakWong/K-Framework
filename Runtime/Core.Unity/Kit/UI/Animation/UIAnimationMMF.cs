using System.Threading;
using Cysharp.Threading.Tasks;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 基于 MMF_Player 的 UI 动画。
/// 对应 UIPanel 上的 OpenFx / CloseFx，也可手动创建并赋给 PanelAnimation。
/// 与 UIAnimationFade 同级，可按需替换。
/// </summary>
public class UIAnimationMMF : UIAnimation
{
    [LabelText("打开反馈")]
    public MMF_Player OpenPlayer { get; set; }

    [LabelText("关闭反馈")]
    public MMF_Player ClosePlayer { get; set; }

    public UIAnimationMMF() { }

    public UIAnimationMMF(MMF_Player openPlayer, MMF_Player closePlayer)
    {
        OpenPlayer = openPlayer;
        ClosePlayer = closePlayer;
    }

    public override async UniTask PlayOpenAsync(CanvasGroup canvasGroup, CancellationToken ct = default)
    {
        if (OpenPlayer == null) return;
        OpenPlayer.PlayFeedbacks();
        await UniTask.WaitUntil(() => !OpenPlayer.IsPlaying, cancellationToken: ct);
    }

    public override async UniTask PlayCloseAsync(CanvasGroup canvasGroup, CancellationToken ct = default)
    {
        if (ClosePlayer == null) return;
        ClosePlayer.PlayFeedbacks();
        await UniTask.WaitUntil(() => !ClosePlayer.IsPlaying, cancellationToken: ct);
    }
}
