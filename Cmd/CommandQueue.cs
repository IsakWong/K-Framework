using System.Collections.Generic;

public class KCommandQueue
{
    private Queue<KCommand> mQueue = new Queue<KCommand>();
    
    public T Push<T>() where T : KCommand, new()
    {
        T cmd = new T();
        cmd.mQueue = this;
        mQueue.Enqueue(cmd);
        return cmd;
    }

    public void Execute()
    {
        while (mQueue.Count > 0)
        {
            var cmd = mQueue.Dequeue();
            cmd.OnExecute();
        }
    }

}