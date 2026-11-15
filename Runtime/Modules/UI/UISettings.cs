using UnityEngine;

namespace KFramework
{
    /// <summary>
    /// UI 根 Canvas 设置。UIService 据此在运行时动态创建根 Canvas 与各层 Canvas，
    /// 项目不再需要在场景中摆放 Canvas 对象。
    /// 资产放在 ConfigService.ConfigPrefix 下，文件名与类型名一致；缺失时使用内置默认值。
    /// </summary>
    [CreateAssetMenu(fileName = "UISettings", menuName = "KFramework/UI 设置")]
    public class UISettings : ConfigObject
    {
        [SerializeField]
        [Tooltip("根 Canvas 渲染模式。Overlay 不依赖相机，跨场景切换最稳；ScreenSpaceCamera 由 ICameraService 自动绑定")]
        private RenderMode _renderMode = RenderMode.ScreenSpaceCamera;

        [SerializeField]
        [Tooltip("CanvasScaler 参考分辨率")]
        private Vector2 _referenceResolution = new(1920f, 1080f);

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("0 = 按宽度适配，1 = 按高度适配")]
        private float _matchWidthOrHeight = 1f;

        [SerializeField]
        [Tooltip("根 Canvas 与各层 Canvas 使用的排序层")]
        private string _sortingLayerName = "AboveUI";

        [SerializeField]
        [Tooltip("ScreenSpaceCamera 模式下的平面距离")]
        private float _planeDistance = 1f;

        public RenderMode CanvasRenderMode => _renderMode;
        public Vector2 ReferenceResolution => _referenceResolution;
        public float MatchWidthOrHeight => _matchWidthOrHeight;
        public string SortingLayerName => _sortingLayerName;
        public float PlaneDistance => _planeDistance;
    }
}
