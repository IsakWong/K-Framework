using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Framework.UI
{
    [RequireComponent(typeof(Button))]
    public class ButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Color HoverColor;
        Button btn;
        private ColorBlock oldColor;
        private ColorBlock newColor;

        void Start()
        {
            btn = gameObject.GetComponent<Button>();
            oldColor = btn.colors;
            newColor = btn.colors;
            newColor.normalColor = HoverColor;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            btn.colors = newColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            btn.colors = oldColor;
        }
    }
}