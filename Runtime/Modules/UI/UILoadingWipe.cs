using UnityEngine;
using UnityEngine.UI;

namespace KFramework
{
    /// <summary>固定全屏 Image 的像素横扫网格。只改变覆盖区域，不移动面板或其内容。</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class UILoadingWipe : BaseMeshEffect
    {
        private const int RowCount = 25;
        private const int HorizontalSteps = 120;
        private const float EdgeStagger = 0.12f;

        public float Coverage { get; private set; } = 1f;
        public bool Revealing { get; private set; }

        public void SetCoverage(float coverage, bool revealing)
        {
            Coverage = Mathf.Clamp01(coverage);
            Revealing = revealing;
            graphic.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper vertices)
        {
            if (!IsActive() || Coverage >= 1f)
                return;

            vertices.Clear();
            if (Coverage <= 0f)
                return;

            Rect rect = graphic.GetPixelAdjustedRect();
            float rowHeight = rect.height / RowCount;
            Color32 color = graphic.color;
            for (int row = 0; row < RowCount; row++)
            {
                // 每行使用固定延迟，避免播放中随机重排导致边缘闪烁。
                float delay = ((row * 73 + 19) % 101) / 100f * EdgeStagger;
                float amount = Mathf.Clamp01((Coverage - delay) / (1f - EdgeStagger));
                float width = Mathf.Ceil(amount * HorizontalSteps) / HorizontalSteps * rect.width;
                if (width <= 0f)
                    continue;

                float left = Revealing ? rect.xMax - width : rect.xMin;
                float right = left + width;
                float top = rect.yMax - row * rowHeight;
                float bottom = rect.yMax - (row + 1) * rowHeight;
                int start = vertices.currentVertCount;
                vertices.AddVert(new Vector3(left, bottom), color, Vector2.zero);
                vertices.AddVert(new Vector3(left, top), color, Vector2.zero);
                vertices.AddVert(new Vector3(right, top), color, Vector2.zero);
                vertices.AddVert(new Vector3(right, bottom), color, Vector2.zero);
                vertices.AddTriangle(start, start + 1, start + 2);
                vertices.AddTriangle(start + 2, start + 3, start);
            }
        }
    }
}
