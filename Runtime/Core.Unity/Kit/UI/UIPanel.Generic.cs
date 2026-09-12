using System;

/// <summary>
/// 支持声明式渲染（UI = f(State)）的泛型 UIPanel 抽象基类。
/// 自动在 OnOpen / OnClose 生命周期中托管 ViewModel 的订阅与退订，并暴露声明式 Render 钩子。
/// </summary>
/// <typeparam name="TViewModel">ViewModel 类型</typeparam>
/// <typeparam name="TState">不可变状态快照类型</typeparam>
public abstract class UIPanel<TViewModel, TState> : UIPanel
    where TViewModel : class, IUIViewModel<TState>
{
    /// <summary>当前绑定的 ViewModel</summary>
    public TViewModel ViewModel { get; protected set; }

    /// <summary>当前不可变状态快照（只读快捷访问）</summary>
    public TState CurrentState => ViewModel != null ? ViewModel.CurrentState : default;

    /// <summary>
    /// 注入/更换 ViewModel。若面板已处于打开状态，自动重新挂接监听并触发首屏渲染。
    /// </summary>
    public virtual void SetViewModel(TViewModel viewModel)
    {
        if (ViewModel == viewModel) return;

        if (ViewModel != null && Visible)
        {
            ViewModel.OnStateChanged -= OnViewModelStateChanged;
        }

        ViewModel = viewModel;

        if (ViewModel != null && Visible)
        {
            ViewModel.OnStateChanged += OnViewModelStateChanged;
            Render(default, ViewModel.CurrentState);
        }
    }

    protected override void OnOpen()
    {
        base.OnOpen();
        if (ViewModel != null)
        {
            ViewModel.OnStateChanged += OnViewModelStateChanged;
            Render(default, ViewModel.CurrentState);
        }
    }

    protected override void OnClose()
    {
        base.OnClose();
        if (ViewModel != null)
        {
            ViewModel.OnStateChanged -= OnViewModelStateChanged;
        }
    }

    private void OnViewModelStateChanged(TState prevState, TState currentState)
    {
        Render(prevState, currentState);
    }

    /// <summary>
    /// 声明式渲染核心（UI = f(State)）：根据新旧状态快照差异单向映射 UI 属性与驱动平滑过渡。
    /// </summary>
    protected abstract void Render(TState prevState, TState currentState);
}
