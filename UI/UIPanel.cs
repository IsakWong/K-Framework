using System;
using UnityEngine;

namespace K1.UI
{
    public class UIPanel : MonoBehaviour
    {
        public UIAnimBase mPanelAnim;
        public bool Visible = false;

        public void Awake()
        {
            UIManager.Instance.RegisterPanel(this);
            mPanelAnim = this.GetComponent<UIAnimBase>();
            if (mPanelAnim == null)
                mPanelAnim = gameObject.AddComponent<UIAnimFadeMove>();
        }

        public virtual void ShowPanel(float delta, float duration)
        {
            gameObject.SetActive(true);
            Visible = true;
            mPanelAnim.Show(delta, duration);
        }

        public virtual void HidePanel(float delta, bool deactivePanel = false)
        {
            Visible = false;
            mPanelAnim.Hide(delta);
        }
    }
}