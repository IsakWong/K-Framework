using System;

public class KCommand
{
    public KCommandQueue mQueue;

    public virtual void OnExecute()
    {

    }
}

public class ActionCommand
{
    private Action<ActionCommand> onExecuteAction;

    public ActionCommand(
        Action<ActionCommand> executeAction = null)
    {
        this.onExecuteAction = executeAction;
    }
    public virtual void OnExecute()
    {
        onExecuteAction ? .Invoke(this);
    }
}