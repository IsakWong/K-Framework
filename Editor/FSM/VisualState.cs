#if UNITY_EDITOR

using System.Collections.Generic;
using CleverCrow.Fluid.BTs.Tasks;
using CleverCrow.Fluid.BTs.Trees.Editors;
using FSM;

namespace K1.Editor
{
    public class VisualState<TStateId>
    {
        private readonly List<VisualState<TStateId>> _children = new List<VisualState<TStateId>>();
        private readonly StateNodePrintController<TStateId> _printer;
        private bool _taskActive;

        public StateBase<TStateId> State { get; }
        public IReadOnlyList<VisualState<TStateId>> Children => _children;

        public IStateMachine<TStateId> Parent { get; set; }

        public float Width { get; } = 70;
        public float Height { get; } = 50;

        public IGraphBox Box { get; private set; }
        public IGraphBox Divider { get; private set; }
        public float DividerLeftOffset { get; private set; }
        public IGraphContainer ParentContainer;
        public IGraphContainer SelfContainer;

        public VisualState(StateBase<TStateId> state, IGraphContainer parentContainer)
        {
            State = state;
            BindTask();

            var container = new GraphContainerVertical();
            SelfContainer = container;
            AddBox(container);

            IStateMachine<TStateId> machine = state as IStateMachine<TStateId>;
            ParentContainer = parentContainer;
            parentContainer.AddBox(container);

            _printer = new StateNodePrintController<TStateId>(this);
        }

        private void BindTask()
        {
            //Task.EditorUtils.EventActive.AddListener(UpdateTaskActiveStatus);
        }

        public void RecursiveTaskUnbind()
        {
            //Task.EditorUtils.EventActive.RemoveListener(UpdateTaskActiveStatus);

            foreach (var child in _children)
            {
                child.RecursiveTaskUnbind();
            }
        }

        private void AddDivider(IGraphContainer parent, IGraphContainer children)
        {
            Divider = new GraphBox
            {
                SkipCentering = true,
            };
            if (children.ChildContainers.Count == 0)
                return;
            DividerLeftOffset = children.ChildContainers[0].Width / 2;
            var dividerRightOffset = children.ChildContainers[children.ChildContainers.Count - 1].Width / 2;
            var width = children.Width - DividerLeftOffset - dividerRightOffset;

            Divider.SetSize(width, 1);

            parent.AddBox(Divider);
        }

        private void AddBox(IGraphContainer parent)
        {
            Box = new GraphBox();
            Box.SetSize(Width, Height);
            Box.SetPadding(10, 10);
            parent.AddBox(Box);
        }

        public void Print()
        {
            _printer.Print(_taskActive);

            foreach (var child in _children)
            {
                child.Print();
            }
        }
    }
}
#endif