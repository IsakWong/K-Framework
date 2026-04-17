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
            EnhancedLog.Debug("Command", $"{cmd.ToString()} Pushed");
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
                EnhancedLog.Debug("Command", $"{first.Value.ToString()} Execute");
            }

            processingResult = first.Value.Execute();
            if (processingResult == ExecuteResult.Success || processingResult == ExecuteResult.Fail)
            {
                if (LogCommand)
                {
                    EnhancedLog.Debug("Command", $"{first.Value.ToString()} Removed");
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
}