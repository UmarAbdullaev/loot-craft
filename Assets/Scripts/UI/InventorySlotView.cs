using LootCraft.Core;
using LootCraft.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LootCraft.UI
{
    /// <summary>
    /// Один слот сетки: фон, иконка, число стака, блок замка с ценой.
    /// Ссылки задаются в Inspector или при сборке префаба — без GetComponent.
    /// Драг предмета; иначе события передаются в ScrollRect (прокрутка сетки).
    /// </summary>
    public class InventorySlotView : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler,
        IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [Header("Ссылки UI")]
        public Image backgroundImage;
        public Image iconImage;
        public TextMeshProUGUI stackText;
        public GameObject lockRoot;
        public Image lockImage;
        public TextMeshProUGUI costText;

        [Tooltip("Опционально: визуал кнопки; клик обрабатывает этот компонент, не onClick.")]
        public Button slotButton;

        private int _slotIndex;
        private InventoryHudController _hud;
        private ScrollRect _scrollRect;

        public void Setup(int slotIndex, InventoryHudController hud, ScrollRect scrollRect)
        {
            _slotIndex = slotIndex;
            _hud = hud;
            _scrollRect = scrollRect;
            if (slotButton != null)
                slotButton.onClick.RemoveAllListeners();
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (_scrollRect == null)
                return;
            ExecuteEvents.Execute(_scrollRect.gameObject, eventData, ExecuteEvents.initializePotentialDrag);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.dragging)
                return;
            _hud?.OnSlotClicked(_slotIndex);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_hud == null)
                return;
            if (_hud.TryBeginSlotDrag(_slotIndex, out _))
            {
                _hud.OnSlotDrag(eventData);
                return;
            }

            ForwardToScroll(ExecuteEvents.beginDragHandler, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_hud != null && _hud.IsItemDragActive)
            {
                _hud.OnSlotDrag(eventData);
                return;
            }

            ForwardToScroll(ExecuteEvents.dragHandler, eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_hud != null && _hud.IsItemDragActive)
            {
                _hud.OnSlotEndDrag(eventData);
                return;
            }

            ForwardToScroll(ExecuteEvents.endDragHandler, eventData);
        }

        private void ForwardToScroll<T>(ExecuteEvents.EventFunction<T> fn, PointerEventData eventData)
            where T : IEventSystemHandler
        {
            if (_scrollRect == null)
                return;
            ExecuteEvents.Execute(_scrollRect.gameObject, eventData, fn);
        }

        public void Refresh(InventoryRuntimeState state, InventoryConfig config, ItemRegistry registry,
            Sprite lockSprite)
        {
            if (state == null || config == null)
                return;

            bool unlocked = state.IsSlotUnlocked(_slotIndex);
            if (lockRoot != null)
                lockRoot.SetActive(!unlocked);

            if (!unlocked)
            {
                int cost = config.GetUnlockCoinCostForSlot(_slotIndex);
                if (costText != null)
                    costText.text = cost >= int.MaxValue / 2 ? "—" : cost.ToString();
                if (lockImage != null && lockSprite != null)
                    lockImage.sprite = lockSprite;
                if (iconImage != null)
                {
                    iconImage.sprite = null;
                    iconImage.enabled = false;
                }

                if (stackText != null)
                    stackText.text = "";
                return;
            }

            var entry = state.GetSlot(_slotIndex);
            if (entry.IsEmpty)
            {
                if (iconImage != null)
                {
                    iconImage.sprite = null;
                    iconImage.enabled = false;
                }

                if (stackText != null)
                    stackText.text = "";
            }
            else
            {
                var def = registry != null ? registry.GetById(entry.ItemId) : null;
                if (iconImage != null)
                {
                    iconImage.sprite = def != null ? def.Icon : null;
                    iconImage.enabled = iconImage.sprite != null;
                }

                if (stackText != null)
                    stackText.text = entry.Count > 1 ? entry.Count.ToString() : "";
            }
        }
    }
}
