using UnityEngine;

namespace LootCraft.UI
{
    /// <summary>
    /// Fits this <see cref="RectTransform"/> to <see cref="Screen.safeArea"/> using normalized anchors,
    /// so content clears notches, punch-holes, and home-indicator areas on phones.
    /// Updates on enable and when this rect’s dimensions change; call <see cref="ForceApply"/> after rotation if needed.
    /// Place on the root panel under <b>Canvas</b> that should be inset (stretch parent should match full screen).
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class RectTransformSafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rect;
        private Rect _lastSafeArea;
        private int _lastScreenW = -1;
        private int _lastScreenH = -1;

        private void Awake()
        {
            CacheRect();
        }

        private void OnEnable()
        {
            CacheRect();
            ForceApply();
        }

        private void OnRectTransformDimensionsChange()
        {
            ApplyIfChanged();
        }

        private void CacheRect()
        {
            _rect = transform as RectTransform;
        }

        /// <summary>Forces a refresh (e.g. after orientation change on some devices).</summary>
        public void ForceApply()
        {
            _lastScreenW = -1;
            _lastScreenH = -1;
            _lastSafeArea = default;
            ApplyIfChanged();
        }

        private void ApplyIfChanged()
        {
            if (_rect == null)
                CacheRect();
            if (_rect == null)
                return;

            Rect sa = Screen.safeArea;
            int w = Screen.width;
            int h = Screen.height;
            if (w <= 0 || h <= 0)
                return;

            if (sa == _lastSafeArea && w == _lastScreenW && h == _lastScreenH)
                return;

            _lastSafeArea = sa;
            _lastScreenW = w;
            _lastScreenH = h;

            Vector2 anchorMin = new Vector2(sa.xMin / w, sa.yMin / h);
            Vector2 anchorMax = new Vector2(sa.xMax / w, sa.yMax / h);

            _rect.anchorMin = anchorMin;
            _rect.anchorMax = anchorMax;
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }
    }
}
