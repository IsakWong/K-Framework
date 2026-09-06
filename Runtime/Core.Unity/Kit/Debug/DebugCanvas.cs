using System;
using TMPro;
using UnityEngine;

public class DebugCanvas : MonoBehaviour
{
    private TextMeshProUGUI Text;
    private void Start()
    {
        
        var go = new GameObject();
        var textTrans = go.AddComponent<RectTransform>();
        Text = go.AddComponent<TextMeshProUGUI>();
        textTrans.SetParent(this.transform, false);
        textTrans.localScale = Vector3.one;
        textTrans.anchorMin = new Vector2(0, 0);
        textTrans.anchorMax = new Vector2(1, 1);
        textTrans.sizeDelta = Vector2.zero;
        textTrans.anchoredPosition = Vector2.zero;
        Text.fontSize = 24;
        Text.text = text;
    }

    private string text;
    public void SetText(string content)
    {
        text = content;
        if(Text)
            Text.text = content;
    }
}