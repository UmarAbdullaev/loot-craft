using System.Text;
using LootCraft.Audio;
using LootCraft.Data;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LootCraft.UI
{
    /// <summary>
    /// Всплывающее окно с параметрами предмета. Родитель со скриптом остаётся активен; panelRoot — панель контента.
    /// Опционально: затемнение фона (dim) и масштаб панели (PrimeTween).
    /// </summary>
    public class ItemDetailPopupView : MonoBehaviour
    {
        [Header("Панель")]
        [Tooltip("Дочерняя панель с карточкой предмета.")]
        [SerializeField] private GameObject panelRoot;

        [Tooltip("Плавное появление текста/фона панели (на панели или её родителе).")]
        [SerializeField] private CanvasGroup panelCanvasGroup;

        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI bodyText;
        [SerializeField] private Button closeButton;

        [Header("Затемнение (опционально)")]
        [Tooltip("Включить полноэкранный dim под панелью.")]
        [SerializeField] private bool enableDim = true;

        [Tooltip("Корень затемнения: растянутый Image на весь экран, сидит в иерархии под панелью (ниже по sibling).")]
        [SerializeField] private GameObject dimRoot;

        [Tooltip("Canvas Group на dim для плавной смены прозрачности (рекомендуется).")]
        [SerializeField] private CanvasGroup dimCanvasGroup;

        [Range(0f, 1f)]
        [SerializeField] private float dimTargetAlpha = 0.55f;

        [Header("Анимация масштаба панели")]
        [SerializeField] private float panelShowFromScale = 0.82f;
        [SerializeField] private float panelShowDuration = 0.22f;
        [SerializeField] private float panelHideToScale = 0.92f;
        [SerializeField] private float panelHideDuration = 0.16f;

        private RectTransform PanelRect =>
            panelRoot != null ? panelRoot.transform as RectTransform : null;

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);

            if (panelRoot != null)
                panelRoot.SetActive(false);
            else
                Debug.LogWarning(
                    "ItemDetailPopupView: назначьте Panel Root (дочерний объект). Родитель со скриптом должен оставаться активным.",
                    this);

            if (panelCanvasGroup != null)
                panelCanvasGroup.alpha = 0f;

            if (dimRoot != null)
                dimRoot.SetActive(false);
            if (dimCanvasGroup != null)
                dimCanvasGroup.alpha = 0f;
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Hide);
        }

        public void Show(ItemDefinition def, int count)
        {
            if (panelRoot == null)
            {
                Debug.LogError("ItemDetailPopupView: не назначен Panel Root.", this);
                return;
            }

            if (def == null)
            {
                Hide();
                return;
            }

            if (titleText != null)
                titleText.text = def.ItemId;

            if (bodyText != null)
                bodyText.text = BuildBody(def, count);

            SoundManager.PlaySound(SoundManager.SoundType.PopupOpen);

            var prt = PanelRect;
            if (prt != null)
            {
                Tween.StopAll(prt);
                prt.localScale = Vector3.one * panelShowFromScale;
            }

            if (enableDim && dimRoot != null)
            {
                dimRoot.SetActive(true);
                if (dimCanvasGroup != null)
                {
                    Tween.StopAll(dimCanvasGroup);
                    dimCanvasGroup.alpha = 0f;
                    Tween.Alpha(dimCanvasGroup, dimTargetAlpha, panelShowDuration, Ease.OutQuad);
                }
            }

            panelRoot.SetActive(true);

            if (prt != null)
                Tween.Scale(prt, Vector3.one, panelShowDuration, Ease.OutBack);

            if (panelCanvasGroup != null)
            {
                Tween.StopAll(panelCanvasGroup);
                panelCanvasGroup.alpha = 0f;
                Tween.Alpha(panelCanvasGroup, 1f, panelShowDuration, Ease.OutQuad);
            }
        }

        public void Hide()
        {
            if (panelRoot == null)
                return;

            // Попап уже закрыт — не твиним неактивные объекты и не дублируем звук закрытия.
            if (!panelRoot.activeSelf)
            {
                StopPopupTweens();
                ApplyFullyHiddenLayout();
                return;
            }

            SoundManager.PlaySound(SoundManager.SoundType.PopupClose);

            var prt = PanelRect;
            if (prt == null)
            {
                ApplyFullyHiddenLayout();
                return;
            }

            StopPopupTweens();

            Tween.Scale(prt, panelHideToScale, panelHideDuration, Ease.InQuad)
                .OnComplete(panelRoot, _ => FinishHide());

            if (panelCanvasGroup != null && panelCanvasGroup.alpha > 0.001f)
                Tween.Alpha(panelCanvasGroup, 0f, panelHideDuration, Ease.InQuad);
            else if (panelCanvasGroup != null)
                panelCanvasGroup.alpha = 0f;

            if (enableDim && dimCanvasGroup != null)
            {
                if (dimRoot != null && dimRoot.activeSelf && dimCanvasGroup.alpha > 0.001f)
                    Tween.Alpha(dimCanvasGroup, 0f, panelHideDuration, Ease.InQuad);
                else
                    dimCanvasGroup.alpha = 0f;
            }
        }

        private void StopPopupTweens()
        {
            var prt = PanelRect;
            if (prt != null)
                Tween.StopAll(prt);
            if (panelCanvasGroup != null)
                Tween.StopAll(panelCanvasGroup);
            if (dimCanvasGroup != null)
                Tween.StopAll(dimCanvasGroup);
        }

        private void ApplyFullyHiddenLayout()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
                var rt = PanelRect;
                if (rt != null)
                    rt.localScale = Vector3.one;
            }

            if (panelCanvasGroup != null)
                panelCanvasGroup.alpha = 0f;

            if (dimRoot != null)
                dimRoot.SetActive(false);
            if (dimCanvasGroup != null)
                dimCanvasGroup.alpha = 0f;
        }

        private void FinishHide()
        {
            ApplyFullyHiddenLayout();
        }

        private static string BuildBody(ItemDefinition def, int count)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Количество: {count}");
            sb.AppendLine($"Тип: {KindRu(def.Kind)}");
            sb.AppendLine($"Вес (за ед.): {def.Weight:0.###}");
            sb.AppendLine($"Макс. стак: {def.MaxStack}");

            switch (def.Kind)
            {
                case ItemKind.Weapon:
                    sb.AppendLine($"Урон: {def.Damage}");
                    if (!string.IsNullOrEmpty(def.AmmoItemId))
                        sb.AppendLine($"Патроны: {def.AmmoItemId}");
                    break;
                case ItemKind.Head:
                case ItemKind.Torso:
                    sb.AppendLine($"Защита: {def.Protection}");
                    break;
            }

            return sb.ToString().TrimEnd();
        }

        private static string KindRu(ItemKind kind)
        {
            switch (kind)
            {
                case ItemKind.Weapon: return "Оружие";
                case ItemKind.Head: return "Голова";
                case ItemKind.Torso: return "Торс";
                case ItemKind.Ammo: return "Патроны";
                default: return kind.ToString();
            }
        }
    }
}
