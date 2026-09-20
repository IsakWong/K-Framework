using System;

public enum ExecuteResult
{
    //不会移除队列
    Continue,

    //移除队列
    Success,

    //移除队列
    Fail
}

public interface ICommand
{
    //用于子类重写，某些Command可能会合并Command，并且判断优先级等插入行为
    public void Enqueue(CommandQueue queue);

    //Continue结果不会把Command移除队列，Success和Fail都会移除队列
    public ExecuteResult Execute();
}

public class Command : ICommand
{
    public int Priority = 0;

    /// <summary>
    /// 入队
    /// </summary>
    /// <param name="queue"></param>
    public virtual void Enqueue(CommandQueue queue)
    {
        queue.Queue.AddLast(this);
    }

    /// <summary>
    /// 执行
    /// </summary>
    /// <returns></returns>
    public virtual ExecuteResult Execute()
    {
        return ExecuteResult.Success;
    }
}