using System.Collections.Generic;
using Mopsicus.InfiniteScroll;
using TMPro;
using UnityEngine;

namespace Framework.Debug
{
    public class DebugModule : KModule
    {
        private GameObject DebugPanel;
        private InfiniteScroll LogView;
        List<string> _log;

        public override void OnInit()
        {
            base.OnInit();
            return;
        }
    }
}