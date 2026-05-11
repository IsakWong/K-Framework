// Author: K-Framework Tests
// Date: 2026/05/11
// FSM StateMachine 纯 C# 测试集

using NUnit.Framework;
using FSM;

namespace KFramework
{
    /// <summary>
    /// FSM StateMachine（层次化有限状态机）的纯 C# 测试集。
    /// 使用 string 作为 StateId 和 Event 类型。
    /// </summary>
    [TestFixture]
    public class StateMachineTests
    {
        // ─── 基础状态转换 ───

        [Test]
        public void FSM_Init_EntersStartState()
        {
            bool entered = false;
            var fsm = new StateMachine();

            fsm.AddState("Idle", new State(onEnter: _ => entered = true));
            fsm.SetStartState("Idle");
            fsm.Init();

            Assert.IsTrue(entered);
            Assert.AreEqual("Idle", fsm.ActiveStateName);
        }

        [Test]
        public void FSM_NoStartState_Init_ThrowsException()
        {
            var fsm = new StateMachine();
            fsm.AddState("A", new State());

            Assert.Throws<System.InvalidOperationException>(() => fsm.Init());
        }

        [Test]
        public void FSM_AddFirstState_AutoSetsAsStart()
        {
            bool entered = false;
            var fsm = new StateMachine();

            fsm.AddState("First", new State(onEnter: _ => entered = true));
            fsm.Init();

            Assert.IsTrue(entered);
            Assert.AreEqual("First", fsm.ActiveStateName);
        }

        // ─── 状态 OnEnter / OnExit / OnLogic ───

        [Test]
        public void FSM_OnEnter_OnExit_OnLogic_AreCalled()
        {
            var log = new System.Collections.Generic.List<string>();
            var fsm = new StateMachine();

            var stateA = new State(
                onEnter: _ => log.Add("enter:A"),
                onLogic: _ => log.Add("logic:A"),
                onExit: _ => log.Add("exit:A"));
            var stateB = new State(
                onEnter: _ => log.Add("enter:B"),
                onLogic: _ => log.Add("logic:B"));

            fsm.AddState("A", stateA);
            fsm.AddState("B", stateB);
            fsm.SetStartState("A");
            fsm.Init();

            // enter:A 已调用
            Assert.AreEqual("enter:A", log[0]);

            fsm.OnLogic();
            Assert.Contains("logic:A", log.ToArray());

            fsm.RequestStateChange("B");
            Assert.Contains("exit:A", log.ToArray());
            Assert.Contains("enter:B", log.ToArray());
        }

        // ─── Transition（条件转换） ───

        [Test]
        public void FSM_Transition_ConditionTrue_SwitchesState()
        {
            var fsm = new StateMachine();
            fsm.AddState("A", new State());
            fsm.AddState("B", new State());

            fsm.AddTransition(new Transition("A", "B", t => true));
            fsm.Init();

            Assert.AreEqual("A", fsm.ActiveStateName);

            fsm.OnLogic(); // 检查转换并执行 onLogic

            Assert.AreEqual("B", fsm.ActiveStateName);
        }

        [Test]
        public void FSM_Transition_ConditionFalse_StaysInState()
        {
            var fsm = new StateMachine();
            fsm.AddState("A", new State());
            fsm.AddState("B", new State());

            fsm.AddTransition(new Transition("A", "B", t => false));
            fsm.Init();

            fsm.OnLogic();

            Assert.AreEqual("A", fsm.ActiveStateName, "条件为 false 不应转换");
        }

        [Test]
        public void FSM_Transition_NoCondition_AlwaysSwitches()
        {
            var fsm = new StateMachine();
            fsm.AddState("A", new State());
            fsm.AddState("B", new State());
            fsm.AddState("C", new State());

            fsm.AddTransition(new Transition("A", "B")); // 无条件 = 总是转换
            fsm.AddTransition(new Transition("B", "C"));
            fsm.Init();

            fsm.OnLogic();
            Assert.AreEqual("B", fsm.ActiveStateName);

            fsm.OnLogic();
            Assert.AreEqual("C", fsm.ActiveStateName);
        }

        // ─── TransitionFromAny ───

        [Test]
        public void FSM_TransitionFromAny_SwitchesFromAnyState()
        {
            var fsm = new StateMachine();
            fsm.AddState("A", new State());
            fsm.AddState("B", new State());
            fsm.AddState("C", new State());

            fsm.AddTransitionFromAny(new Transition("", "C", t => true));
            fsm.Init();

            fsm.OnLogic();
            Assert.AreEqual("C", fsm.ActiveStateName);
        }

        // ─── Trigger 事件驱动转换 ───

        [Test]
        public void FSM_Trigger_ActivatesTransition()
        {
            var fsm = new StateMachine<string, string>();
            fsm.AddState("A", new State<string, string>());
            fsm.AddState("B", new State<string, string>());

            fsm.AddTriggerTransition("go", new Transition<string>("A", "B"));
            fsm.Init();

            Assert.AreEqual("A", fsm.ActiveStateName);

            fsm.Trigger("go");
            Assert.AreEqual("B", fsm.ActiveStateName);
        }

        [Test]
        public void FSM_Trigger_ConditionFalse_DoesNotSwitch()
        {
            var fsm = new StateMachine<string, string>();
            fsm.AddState("A", new State<string, string>());
            fsm.AddState("B", new State<string, string>());

            fsm.AddTriggerTransition("fail", new Transition<string>("A", "B", t => false));
            fsm.Init();

            fsm.Trigger("fail");
            Assert.AreEqual("A", fsm.ActiveStateName);
        }

        [Test]
        public void FSM_TriggerFromAny_SwitchesFromAnyState()
        {
            var fsm = new StateMachine<string, string>();
            fsm.AddState("A", new State<string, string>());
            fsm.AddState("B", new State<string, string>());

            fsm.AddTriggerTransitionFromAny("global", new Transition<string>("", "B"));
            fsm.Init();

            fsm.Trigger("global");
            Assert.AreEqual("B", fsm.ActiveStateName);
        }

        // ─── TwoWay Transition ───

        [Test]
        public void FSM_TwoWayTransition_ConditionTrue_GoesForward()
        {
            var fsm = new StateMachine();
            fsm.AddState("A", new State());
            fsm.AddState("B", new State());

            fsm.AddTwoWayTransition(new Transition("A", "B", t => true));
            fsm.Init();

            Assert.AreEqual("A", fsm.ActiveStateName);

            fsm.OnLogic(); // 条件 true → A -> B
            Assert.AreEqual("B", fsm.ActiveStateName);

            fsm.OnLogic(); // 反向的 condition 是反转的 → ReverseTransition 取反 → true → B -> A
            Assert.AreEqual("A", fsm.ActiveStateName);
        }

        // ─── needsExitTime ───

        [Test]
        public void FSM_NeedsExitTime_CanExitTrue_AllowsTransition()
        {
            var fsm = new StateMachine();
            var stateA = new State<string, string>(
                canExit: s => true,
                needsExitTime: true);
            fsm.AddState("A", stateA);
            fsm.AddState("B", new State<string, string>());
            fsm.AddTransition(new Transition<string>("A", "B"));

            fsm.Init();
            Assert.AreEqual("A", fsm.ActiveStateName);

            // needsExitTime=true 且 canExit=true → OnLogic 中 canExit 返回 true
            // Transition 为无条件 → ShouldTransition 返回 true
            // 触发 RequestStateChange → needsExitTime=true → 调用 OnExitRequest
            // OnExitRequest 调用 canExit → true → fsm.StateCanExit → ChangeState
            fsm.OnLogic();
            Assert.AreEqual("B", fsm.ActiveStateName);
        }

        [Test]
        public void FSM_NeedsExitTime_CanExitFalse_BlocksTransition()
        {
            bool canExitCalled = false;
            var fsm = new StateMachine();
            var stateA = new State<string, string>(
                canExit: s => { canExitCalled = true; return false; },
                needsExitTime: true);
            fsm.AddState("A", stateA);
            fsm.AddState("B", new State<string, string>());
            fsm.AddTransition(new Transition<string>("A", "B"));

            fsm.Init();

            fsm.OnLogic();
            Assert.IsTrue(canExitCalled);
            Assert.AreEqual("A", fsm.ActiveStateName, "canExit=false 不应转换");

            // 后续调用 StateCanExit 应该完成挂起的转换
            fsm.StateCanExit();
            Assert.AreEqual("B", fsm.ActiveStateName, "StateCanExit 应完成挂起转换");
        }

        [Test]
        public void FSM_ForceInstantly_IgnoresNeedsExitTime()
        {
            var fsm = new StateMachine();
            var stateA = new State<string, string>(
                canExit: s => false, // canExit 返回 false
                needsExitTime: true);
            fsm.AddState("A", stateA);
            fsm.AddState("B", new State<string, string>());

            // forceInstantly=true
            fsm.AddTransition(new Transition<string>("A", "B", null, forceInstantly: true));

            fsm.Init();

            fsm.OnLogic();
            Assert.AreEqual("B", fsm.ActiveStateName, "forceInstantly 应跳过 needsExitTime");
        }

        // ─── 层次化 FSM (FSM 作为 State) ───

        [Test]
        public void FSM_Hierarchical_ChildFSM_SwitchesInternally()
        {
            var parentFsm = new StateMachine();
            var childFsm = new StateMachine<string, string, string>(needsExitTime: false);

            childFsm.AddState("SubA", new State<string, string, string>());
            childFsm.AddState("SubB", new State<string, string, string>());
            childFsm.AddTransition(new Transition<string>("SubA", "SubB"));
            childFsm.SetStartState("SubA");

            parentFsm.AddState("Child", childFsm);
            parentFsm.Init();

            Assert.AreEqual("SubA", childFsm.ActiveStateName);

            parentFsm.OnLogic(); // 驱动 childFsm 的 OnLogic
            Assert.AreEqual("SubB", childFsm.ActiveStateName);
        }

        [Test]
        public void FSM_Hierarchical_ChildTrigger_PropagatesCorrectly()
        {
            var parent = new StateMachine<string, string, string>();
            var child = new StateMachine<string, string, string>(needsExitTime: false);

            child.AddState("SubA", new State<string, string, string>());
            child.AddState("SubB", new State<string, string, string>());
            child.AddTriggerTransition("swap", new Transition<string>("SubA", "SubB"));
            child.SetStartState("SubA");

            parent.AddState("Child", child);
            parent.AddState("Other", new State<string, string, string>());
            parent.AddTransition(new Transition<string>("Child", "Other"));
            parent.Init();

            // Trigger 会传递给 child
            parent.Trigger("swap");
            Assert.AreEqual("SubB", child.ActiveStateName);
        }

        // ─── GetState ───

        [Test]
        public void FSM_GetState_ReturnsState()
        {
            var fsm = new StateMachine();
            var state = new State();
            fsm.AddState("Test", state);

            var result = fsm.GetState("Test");
            Assert.AreSame(state, result);
        }

        [Test]
        public void FSM_GetState_NotExist_ThrowsException()
        {
            var fsm = new StateMachine();
            fsm.AddState("A", new State());
            fsm.Init();

            Assert.Throws<FSM.Exceptions.StateNotFoundException<string>>(
                () => fsm.GetState("NotExist"));
        }

        // ─── GetStates ───

        [Test]
        public void FSM_GetStates_ReturnsAllStates()
        {
            var fsm = new StateMachine();
            fsm.AddState("A", new State());
            fsm.AddState("B", new State());
            fsm.AddState("C", new State());

            var states = fsm.GetStates();
            Assert.AreEqual(3, states.Count);
        }

        // ─── RequestStateChange ───

        [Test]
        public void FSM_RequestStateChange_SwitchesImmediately()
        {
            var fsm = new StateMachine();
            fsm.AddState("A", new State());
            fsm.AddState("B", new State());
            fsm.Init();

            fsm.RequestStateChange("B");
            Assert.AreEqual("B", fsm.ActiveStateName);
        }

        [Test]
        public void FSM_RequestStateChange_NonExistent_ThrowsException()
        {
            var fsm = new StateMachine();
            fsm.AddState("A", new State());
            fsm.Init();

            Assert.Throws<FSM.Exceptions.StateNotFoundException<string>>(
                () => fsm.RequestStateChange("Ghost"));
        }

        // ─── OnExit clean up ───

        [Test]
        public void FSM_OnExit_ClearsActiveState()
        {
            var fsm = new StateMachine();
            bool exited = false;
            fsm.AddState("A", new State(onExit: _ => exited = true));
            fsm.Init();

            fsm.OnExit();
            Assert.IsTrue(exited);
        }

        // ─── 完整工作流 ───

        [Test]
        public void FSM_CompleteWorkflow_StatesAndTransitions()
        {
            var log = new System.Collections.Generic.List<string>();

            var idleState = new State(
                onEnter: _ => log.Add("idle:enter"),
                onExit: _ => log.Add("idle:exit"));
            var runState = new State(
                onEnter: _ => log.Add("run:enter"),
                onExit: _ => log.Add("run:exit"));
            var jumpState = new State(
                onEnter: _ => log.Add("jump:enter"),
                onExit: _ => log.Add("jump:exit"));

            var fsm = new StateMachine();
            fsm.AddState("Idle", idleState);
            fsm.AddState("Run", runState);
            fsm.AddState("Jump", jumpState);

            // Idle -> Run（无条件）
            fsm.AddTransition(new Transition("Idle", "Run"));
            // Run -> Jump（无条件）
            fsm.AddTransition(new Transition("Run", "Jump"));

            fsm.Init();

            // Idle -> Run
            fsm.OnLogic();
            Assert.AreEqual("Run", fsm.ActiveStateName);
            Assert.Contains("idle:exit", log.ToArray());
            Assert.Contains("run:enter", log.ToArray());

            // Run -> Jump
            fsm.OnLogic();
            Assert.AreEqual("Jump", fsm.ActiveStateName);
            Assert.Contains("run:exit", log.ToArray());
            Assert.Contains("jump:enter", log.ToArray());
        }
    }
}
