using System.Collections.Generic;

public class CommandQueue
{
    public readonly LinkedList<ICommand> Queue = new();
    public bool LogCommand = false;
    private ExecuteResult processingResult;

    public T Push<T>() where T : ICommand, new()
    {
        var cmd = new T();
        cmd.Enqueue(this);
        return cmd;
    }

    public ICommand PushCmd(ICommand cmd)
    {
        if (LogCommand)
        {
            Log($"{cmd.ToString()} Pushed");
        }

        cmd.Enqueue(this);
        return cmd;
    }

    public void ProcessOnce()
    {
        if (Queue.Count > 0)
        {
            var first = Queue.First;

            if (LogCommand)
            {
                Log($"{first.Value.ToString()} Execute");
            }

            processingResult = first.Value.Execute();
            if (processingResult == ExecuteResult.Success || processingResult == ExecuteResult.Fail)
            {
                if (LogCommand)
                {
                    Log($"{first.Value.ToString()} Removed");
                }

                Queue.Remove(first);
            }
        }
    }

    public void ProcessUntilEmpty()
    {
        while (Queue.Count > 0)
        {
            ProcessOnce();
        }
    }

    /// <summary>命令日志经 ILogService 输出，日志服务未注册时静默跳过。</summary>
    private static void Log(string message)
    {
        ServiceLocator.GetOrDefault<ILogService>()?.Debug("Command", message);
    }
}