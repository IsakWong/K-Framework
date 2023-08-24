using System.Collections.Generic;

public class CommandQueue
{
    public readonly LinkedList<ICommand> Queue = new LinkedList<ICommand>();
    private ExecuteResult processingResult;

    public T Push<T>() where T : ICommand, new()
    {
        T cmd = new T();
        cmd.Enqueue(this);
        return cmd;
    }

    public ICommand PushCmd(ICommand cmd)
    {
        cmd.Enqueue(this);
        return cmd;
    }

    public void ProcessOnce()
    {
        if (Queue.Count > 0)
        {
            var first = Queue.First;
            processingResult = first.Value.Execute();
            if (processingResult == ExecuteResult.Success || processingResult == ExecuteResult.Fail)
                Queue.Remove(first);
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