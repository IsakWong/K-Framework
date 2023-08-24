using K1.UI;
using UnityEngine;

public class UIModule : KModule
{
    // <color=#469df3></color>
    public Color32 color0 = new Color32(0x46, 0x9d, 0xf3, 255);

    // <color=#fe5252></color>
    public Color32 color1 = new Color32(4, 123, 255, 255);

    // <color=#ffa800></color>
    public Color32 color2 = new Color32(0xff, 0xa8, 0x00, 0xff);

    // <color=#ab7433></color>
    public Color32 color3 = new Color32(0xab, 0x74, 0x33, 255);

    // <color=#ff8920></color>
    public Color32 color4 = new Color32(0xff, 0x89, 0x20, 255);

    // <color=#ff1837></color>
    public Color32 color5 = new Color32(0xff, 0x18, 0x37, 0xff);

    public Canvas OverlayCanvas;

    protected void Awake()
    {
        base.Awake();
        for (int idx = 0; idx < OverlayCanvas.transform.childCount; idx++)
        {
            UIPanel p = OverlayCanvas.transform.GetChild(idx).GetComponent<UIPanel>();
            if (p != null)
                UIManager.Instance.AddUI(p);
        }

        UIManager.Instance.UIModule = this;
    }
}