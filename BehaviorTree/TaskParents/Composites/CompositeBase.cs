using System;

namespace CleverCrow.Fluid.BTs.TaskParents.Composites {
    public abstract class CompositeBase : TaskParentBase {
        public int ChildIndex { get; protected set; }

        public Action startLogic;
        public Action onLogic;
        public Action exitLogic;
        protected bool _start = false;
        
        public override void End () {
            if (ChildIndex < Children.Count) {
                Children[ChildIndex].End();
            }
        }
        
        public override void Reset () {
            ChildIndex = 0;

            base.Reset();
        }
    }
}