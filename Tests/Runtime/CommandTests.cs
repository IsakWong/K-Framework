// Author: K-Framework Tests
// Date: 2026/05/11
// Command / CommandQueue 纯 C# 测试集

using NUnit.Framework;

namespace KFramework
{
    /// <summary>
    /// Command 和 CommandQueue 的纯 C# 测试集。
    /// </summary>
    [TestFixture]
    public class CommandTests
    {
        // ─── 测试 Command 实现 ───

        private class SuccessCommand : Command
        {
            public int ExecuteCount;
            public override ExecuteResult Execute()
            {
                ExecuteCount++;
                return ExecuteResult.Success;
            }
        }

        private class ContinueCommand : Command
        {
            public int ExecuteCount;
            public int MaxExecutions = 3;
            public override ExecuteResult Execute()
            {
                ExecuteCount++;
                if (ExecuteCount >= MaxExecutions)
                    return ExecuteResult.Success;
                return ExecuteResult.Continue;
            }
        }

        private class FailCommand : Command
        {
            public override ExecuteResult Execute()
            {
                return ExecuteResult.Fail;
            }
        }

        // ─── CommandQueue 基础 ───

        [Test]
        public void CommandQueue_Push_AddsToQueue()
        {
            var queue = new CommandQueue();
            var cmd = queue.Push<SuccessCommand>();

            Assert.IsNotNull(cmd);
            Assert.AreEqual(1, queue.Queue.Count);
            Assert.AreSame(cmd, queue.Queue.First.Value);
        }

        [Test]
        public void CommandQueue_ProcessOnce_Success_RemovesCommand()
        {
            var queue = new CommandQueue();
            var cmd = queue.Push<SuccessCommand>();

            queue.ProcessOnce();

            Assert.AreEqual(1, cmd.ExecuteCount);
            Assert.AreEqual(0, queue.Queue.Count, "Success 后应移除");
        }

        [Test]
        public void CommandQueue_ProcessOnce_Continue_KeepsCommand()
        {
            var queue = new CommandQueue();
            var cmd = queue.Push<ContinueCommand>();

            // 第 1 次：Continue
            queue.ProcessOnce();
            Assert.AreEqual(1, cmd.ExecuteCount);
            Assert.AreEqual(1, queue.Queue.Count, "Continue 不应移除");

            // 第 2 次：Continue
            queue.ProcessOnce();
            Assert.AreEqual(2, cmd.ExecuteCount);
            Assert.AreEqual(1, queue.Queue.Count);

            // 第 3 次：Success -> 移除
            queue.ProcessOnce();
            Assert.AreEqual(3, cmd.ExecuteCount);
            Assert.AreEqual(0, queue.Queue.Count);
        }

        [Test]
        public void CommandQueue_ProcessOnce_Fail_RemovesCommand()
        {
            var queue = new CommandQueue();
            queue.Push<FailCommand>();

            // Fail 不应抛异常
            Assert.DoesNotThrow(() => queue.ProcessOnce());

            Assert.AreEqual(0, queue.Queue.Count, "Fail 后应移除");
        }

        [Test]
        public void CommandQueue_ProcessOnce_EmptyQueue_DoesNotThrow()
        {
            var queue = new CommandQueue();
            Assert.DoesNotThrow(() => queue.ProcessOnce());
        }

        // ─── CommandQueue 批量处理 ───

        [Test]
        public void CommandQueue_ProcessUntilEmpty_ProcessesAll()
        {
            var queue = new CommandQueue();
            var cmd1 = queue.Push<SuccessCommand>();
            var cmd2 = queue.Push<SuccessCommand>();
            var cmd3 = queue.Push<SuccessCommand>();

            queue.ProcessUntilEmpty();

            Assert.AreEqual(0, queue.Queue.Count);
            Assert.AreEqual(1, cmd1.ExecuteCount);
            Assert.AreEqual(1, cmd2.ExecuteCount);
            Assert.AreEqual(1, cmd3.ExecuteCount);
        }

        [Test]
        public void CommandQueue_MixedResults_ProcessesCorrectly()
        {
            var queue = new CommandQueue();
            var cmd = queue.Push<ContinueCommand>();
            var cmd2 = queue.Push<SuccessCommand>();

            // 因为 ContinueCommand 在 SuccessCommand 前面
            // 且队列处理每次只处理第一个元素
            queue.ProcessOnce(); // ContinueCommand: 1 -> Continue
            Assert.AreEqual(2, queue.Queue.Count);

            queue.ProcessOnce(); // ContinueCommand: 2 -> Continue
            Assert.AreEqual(2, queue.Queue.Count);

            queue.ProcessOnce(); // ContinueCommand: 3 -> Success, 移除
            Assert.AreEqual(1, queue.Queue.Count, "ContinueCommand 移除后只剩 SuccessCommand");

            queue.ProcessOnce(); // SuccessCommand -> Success, 移除
            Assert.AreEqual(0, queue.Queue.Count);
            Assert.AreEqual(1, cmd2.ExecuteCount);
        }

        // ─── PushCmd ───

        [Test]
        public void CommandQueue_PushCmd_AddsPreCreatedCommand()
        {
            var queue = new CommandQueue();
            var cmd = new SuccessCommand();
            var returned = queue.PushCmd(cmd);

            Assert.AreSame(cmd, returned);
            Assert.AreEqual(1, queue.Queue.Count);
        }

        // ─── Command 子类 ───

        [Test]
        public void Command_DefaultPriority_IsZero()
        {
            var cmd = new SuccessCommand();
            Assert.AreEqual(0, cmd.Priority);
        }

        [Test]
        public void Command_Priority_CanBeSet()
        {
            var cmd = new SuccessCommand { Priority = 10 };
            Assert.AreEqual(10, cmd.Priority);
        }
    }
}
