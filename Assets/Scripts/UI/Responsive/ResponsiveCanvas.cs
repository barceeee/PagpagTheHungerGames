using UnityEngine;
using UnityEngine.UI;

namespace Pagpag.UI
{
    /// <summary>
    /// Keeps the 1920×1080 design rectangle fully on screen at any aspect ratio, and makes the UI larger
    /// on small physical screens (phones) so controls stay touchable.
    /// <list type="bullet">
    /// <item>Wider than 16:9: match height, so the extra width goes to stretching regions (the map grows).</item>
    /// <item>Narrower than 16:9: match width, so nothing is cut off at the sides.</item>
    /// <item>Compact screens (mobile, or under <see cref="compactMaxHeightInches"/> tall): reference resolution is
    /// reduced by <see cref="compactScale"/>, so everything renders that much bigger; regions that can't fit
    /// scale themselves back down (<see cref="FitInParent"/>).</item>
    /// </list>
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    [ExecuteAlways]
    public class ResponsiveCanvas : MonoBehaviour
    {
        public static readonly Vector2 DesignSize = new Vector2(1920, 1080);

        [Tooltip("Screens shorter than this (in inches) use the compact, larger-UI setting.")]
        [SerializeField] private float compactMaxHeightInches = 4.5f;
        [Tooltip("How much bigger the UI is drawn on compact screens.")]
        [SerializeField] private float compactScale = 1.3f;

        /// <summary>True when the current screen uses the compact (touch) setting.</summary>
        public static bool Compact { get; private set; }

        /// <summary>Forces compact on/off (developer screen captures); null = detect.</summary>
        public static bool? CompactOverride;

        private CanvasScaler _scaler;
        private Canvas _canvas;
        private Vector2Int _lastScreen;

        private void OnEnable()
        {
            _scaler = GetComponent<CanvasScaler>();
            _canvas = GetComponent<Canvas>();
            _lastScreen = Vector2Int.zero;
            Apply();
        }

        /// <summary>Pixel size the canvas is drawn at (the screen, or a render texture when captured).</summary>
        private Vector2Int PixelSize()
        {
            if (_canvas != null && _canvas.renderMode == RenderMode.ScreenSpaceCamera && _canvas.worldCamera != null)
                return new Vector2Int(_canvas.worldCamera.pixelWidth, _canvas.worldCamera.pixelHeight);
            return new Vector2Int(Screen.width, Screen.height);
        }

        private void Update()
        {
            if (PixelSize() != _lastScreen) Apply();
        }

        /// <summary>Re-applies every responsive layout component in the open scenes (used by screen captures).</summary>
        public static void RefreshAll()
        {
            foreach (var c in FindObjectsByType<ResponsiveCanvas>()) c.Apply();
            Canvas.ForceUpdateCanvases();
            for (var pass = 0; pass < 2; pass++)
            {
                foreach (var f in FindObjectsByType<FitInParent>()) f.Apply();
                foreach (var d in FindObjectsByType<DockBelow>()) d.Apply();
                Canvas.ForceUpdateCanvases();
            }
        }

        public static bool IsCompactScreen(float maxHeightInches)
        {
            if (Application.isMobilePlatform) return true;
            var dpi = Screen.dpi;
            return dpi > 0 && Screen.height / dpi < maxHeightInches && Application.isPlaying;
        }

        public void Apply()
        {
            if (_scaler == null) return;
            _lastScreen = PixelSize();
            var aspect = _lastScreen.y > 0 ? (float)_lastScreen.x / _lastScreen.y : DesignSize.x / DesignSize.y;
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            _scaler.matchWidthOrHeight = aspect >= DesignSize.x / DesignSize.y ? 1f : 0f;
            Compact = CompactOverride ?? IsCompactScreen(compactMaxHeightInches);
            _scaler.referenceResolution = Compact ? DesignSize / compactScale : DesignSize;
        }
    }
}
