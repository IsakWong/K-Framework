#if UNITY_EDITOR
using System.Collections.Generic;
using CleverCrow.Fluid.BTs.Trees.Editors;
using FSM;
using UnityEngine;

namespace K1.Editor
{
    public class StateMachinePrinter<TStateID, TEvent>
    {
        private const float SCROLL_PADDING = 40;

        private readonly VisualState<TStateID> _root;
        private readonly Rect _containerSize;

        private Vector2 _scrollPosition;

        public static StatusIcons StatusIcons { get; private set; }
        public static GuiStyleCollection SharedStyles { get; private set; }

        public Dictionary<TStateID, VisualState<TStateID>> nodes;

        public StateMachinePrinter(StateMachine<TStateID, TStateID, TEvent> tree, Vector2 windowSize)
        {
            StatusIcons = new StatusIcons();
            SharedStyles = new GuiStyleCollection();

            var container = new GraphContainerVertical();
            container.SetGlobalPosition(SCROLL_PADDING, SCROLL_PADDING);
            var startState = tree.GetState(tree.GetStartState());
            _root = new VisualState<TStateID>(startState, container);
            container.CenterAlignChildren();
            if (tree != null)
            {
                var states = tree.GetStates();
                var transitions = tree.TriggerTransitionsFromAny;
                foreach (var it in transitions)
                {
                    foreach (var trans in it.Value)
                    {
                        if (!nodes.ContainsKey(trans.to))
                        {
                            var childContainer = new GraphContainerHorizontal();
                            var toState = tree.GetState(trans.to);
                            var childState = new VisualState<TStateID>(toState, childContainer);
                            container.AddBox(childContainer);
                        }
                    }
                }
            }


            _containerSize = new Rect(0, 0,
                container.Width + SCROLL_PADDING * 2,
                container.Height + SCROLL_PADDING * 2);

            CenterScrollView(windowSize, container);
        }

        private void CenterScrollView(Vector2 windowSize, GraphContainerVertical container)
        {
            var scrollOverflow = container.Width + SCROLL_PADDING * 2 - windowSize.x;
            var centerViewPosition = scrollOverflow / 2;
            _scrollPosition.x = centerViewPosition;
        }

        public void Print(Vector2 windowSize)
        {
            _scrollPosition = GUI.BeginScrollView(
                new Rect(0, 0, windowSize.x, windowSize.y),
                _scrollPosition,
                _containerSize);
            _root.Print();
            GUI.EndScrollView();
        }

        public void Unbind()
        {
            _root.RecursiveTaskUnbind();
        }
    }
}
#endif