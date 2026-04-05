using LootCraft.Audio;
using LootCraft.Core;
using LootCraft.Data;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LootCraft.UI
{
    /// <summary>
    /// Экран инвентаря: кнопки ТЗ, сетка из префаба, драг, попап; PrimeTween для микро-анимаций.
    /// </summary>
    public class InventoryHudController : MonoBehaviour
    {
        private const float SlotPunchScale = 1.07f;
        private const float SlotPunchDuration = 0.1f;
        private const float GhostShowFromScale = 0.55f;
        private const float GhostShowDuration = 0.18f;

        [SerializeField] private InventoryGameplayService gameplayService;

        [Header("Кнопки ТЗ")]
        [SerializeField] private Button buttonShoot;
        [SerializeField] private Button buttonAddAmmo;
        [SerializeField] private Button buttonAddItem;
        [SerializeField] private Button buttonRemoveItem;
        [SerializeField] private Button buttonAddCoins;

        [Header("Текст")]
        [SerializeField] private TextMeshProUGUI coinsText;
        [SerializeField] private TextMeshProUGUI weightText;

        [Header("Сетка из префаба")]
        [Tooltip("Префаб с корневым InventorySlotView + Button на корне.")]
        [SerializeField] private InventorySlotView slotPrefab;

        [Tooltip("Content под ScrollRect с GridLayoutGroup (5 колонок).")]
        [SerializeField] private RectTransform slotsContent;

        [Tooltip("Тот же ScrollRect, что владеет slotsContent — для прокрутки при драге по пустым слотам.")]
        [SerializeField] private ScrollRect slotsScrollRect;

        [Tooltip("Перед спавном удалить всех детей slotsContent (чистый повторный Play).")]
        [SerializeField] private bool clearSlotsContentBeforeSpawn = true;

        [Header("Иконка замка при Refresh (можно дублировать в префабе)")]
        [SerializeField] private Sprite lockIconSprite;

        [Header("Драг предмета (фаза 6)")]
        [Tooltip("Canvas того же UI (для перевода экранных координат).")]
        [SerializeField] private Canvas dragCanvas;

        [Tooltip("Image-призрак под корнем Canvas; выключен в сцене до драга.")]
        [SerializeField] private Image dragGhostImage;

        [SerializeField] private Vector2 dragGhostSize = new Vector2(120f, 120f);

        [Header("Попап предмета (фаза 6)")]
        [SerializeField] private ItemDetailPopupView itemDetailPopup;

        private InventorySlotView[] _spawnedSlots;
        private int _dragFromIndex = -1;
        private bool _dragActive;
        private float _lastWeightShown = float.NaN;

        /// <summary>True между успешным TryBeginSlotDrag и завершением OnSlotEndDrag.</summary>
        public bool IsItemDragActive => _dragActive;

        private void OnDestroy()
        {
            if (gameplayService != null)
            {
                gameplayService.OnCoinsChanged -= OnEconomyChanged;
                gameplayService.OnInventoryChanged -= OnInventoryChanged;
                UnsubscribePresentation();
            }

            UnwireButtons();
        }

        private void Start()
        {
            if (gameplayService == null)
            {
                Debug.LogError("InventoryHudController: не назначен InventoryGameplayService.", this);
                return;
            }

            if (slotPrefab == null || slotsContent == null)
            {
                Debug.LogError(
                    "InventoryHudController: назначьте Slot Prefab и Slots Content (родитель сетки).",
                    this);
                return;
            }

            if (dragGhostImage != null && dragCanvas == null)
                Debug.LogWarning(
                    "InventoryHudController: для драга назначьте Drag Canvas (тот же Canvas, что и сетка).",
                    this);

            SpawnSlots();
            WireButtons();
            gameplayService.OnCoinsChanged += OnEconomyChanged;
            gameplayService.OnInventoryChanged += OnInventoryChanged;
            SubscribePresentation();
            RefreshAll();
        }

        private void SpawnSlots()
        {
            int total = gameplayService.State.TotalSlots;

            if (clearSlotsContentBeforeSpawn)
            {
                for (int c = slotsContent.childCount - 1; c >= 0; c--)
                    Destroy(slotsContent.GetChild(c).gameObject);
            }

            _spawnedSlots = new InventorySlotView[total];
            for (int i = 0; i < total; i++)
            {
                var inst = Instantiate(slotPrefab, slotsContent);
                inst.gameObject.name = $"Slot_{i}";
                inst.Setup(i, this, slotsScrollRect);
                _spawnedSlots[i] = inst;
            }
        }

        private void WireButtons()
        {
            if (buttonShoot != null)
                buttonShoot.onClick.AddListener(OnShoot);
            if (buttonAddAmmo != null)
                buttonAddAmmo.onClick.AddListener(OnAddAmmo);
            if (buttonAddItem != null)
                buttonAddItem.onClick.AddListener(OnAddItem);
            if (buttonRemoveItem != null)
                buttonRemoveItem.onClick.AddListener(OnRemove);
            if (buttonAddCoins != null)
                buttonAddCoins.onClick.AddListener(OnAddCoins);
        }

        private void UnwireButtons()
        {
            if (buttonShoot != null)
                buttonShoot.onClick.RemoveListener(OnShoot);
            if (buttonAddAmmo != null)
                buttonAddAmmo.onClick.RemoveListener(OnAddAmmo);
            if (buttonAddItem != null)
                buttonAddItem.onClick.RemoveListener(OnAddItem);
            if (buttonRemoveItem != null)
                buttonRemoveItem.onClick.RemoveListener(OnRemove);
            if (buttonAddCoins != null)
                buttonAddCoins.onClick.RemoveListener(OnAddCoins);
        }

        private void SubscribePresentation()
        {
            gameplayService.OnCoinsGranted += OnCoinsGrantedFx;
            gameplayService.OnEquipmentPlaced += OnEquipmentPlacedFx;
            gameplayService.OnAmmoPrimarySlot += OnAmmoPlacedFx;
            gameplayService.OnShootPerformed += OnShootFx;
            gameplayService.OnRandomSlotCleared += OnRandomDeleteFx;
            gameplayService.OnSlotUnlockPurchased += OnSlotUnlockFx;
            gameplayService.OnDragDropApplied += OnDragDropFx;
        }

        private void UnsubscribePresentation()
        {
            gameplayService.OnCoinsGranted -= OnCoinsGrantedFx;
            gameplayService.OnEquipmentPlaced -= OnEquipmentPlacedFx;
            gameplayService.OnAmmoPrimarySlot -= OnAmmoPlacedFx;
            gameplayService.OnShootPerformed -= OnShootFx;
            gameplayService.OnRandomSlotCleared -= OnRandomDeleteFx;
            gameplayService.OnSlotUnlockPurchased -= OnSlotUnlockFx;
            gameplayService.OnDragDropApplied -= OnDragDropFx;
        }

        private void OnCoinsGrantedFx()
        {
            SoundManager.PlaySound(SoundManager.SoundType.Coin);
            if (coinsText != null)
            {
                var rt = coinsText.rectTransform;
                Tween.StopAll(rt);
                Tween.Scale(rt, SlotPunchScale, SlotPunchDuration, Ease.OutQuad, cycles: 2, cycleMode: CycleMode.Yoyo);
            }
        }

        private void OnEquipmentPlacedFx(int slot)
        {
            SoundManager.PlaySound(SoundManager.SoundType.ItemAdd);
            PunchSlot(slot);
        }

        private void OnAmmoPlacedFx(int slot)
        {
            SoundManager.PlaySound(SoundManager.SoundType.AmmoAdd);
            PunchSlot(slot);
        }

        private void OnShootFx()
        {
            SoundManager.PlaySound(SoundManager.SoundType.Shoot);
        }

        private void OnRandomDeleteFx(int slot)
        {
            SoundManager.PlaySound(SoundManager.SoundType.DeleteItem);
            PunchSlot(slot);
        }

        private void OnSlotUnlockFx(int slot)
        {
            SoundManager.PlaySound(SoundManager.SoundType.SlotUnlock);
            PunchSlot(slot);
        }

        private void OnDragDropFx(int from, int to)
        {
            SoundManager.PlaySound(SoundManager.SoundType.DragDrop);
            PunchSlot(to);
        }

        private bool TryGetSlotTransform(int index, out Transform t)
        {
            t = null;
            if (_spawnedSlots == null || index < 0 || index >= _spawnedSlots.Length)
                return false;
            var view = _spawnedSlots[index];
            if (view == null)
                return false;
            t = view.transform;
            return true;
        }

        private void PunchSlot(int index)
        {
            if (!TryGetSlotTransform(index, out var tr))
                return;
            var rt = (RectTransform)tr;
            Tween.StopAll(rt);
            Tween.Scale(rt, SlotPunchScale, SlotPunchDuration, Ease.OutQuad, cycles: 2, cycleMode: CycleMode.Yoyo);
        }

        private void OnShoot()
        {
            SoundManager.PlaySound(SoundManager.SoundType.Button);
            PunchButton(buttonShoot);
            gameplayService?.ShootRandomWeapon();
        }

        private void OnAddAmmo()
        {
            SoundManager.PlaySound(SoundManager.SoundType.Button);
            PunchButton(buttonAddAmmo);
            gameplayService?.AddRandomAmmo();
        }

        private void OnAddItem()
        {
            SoundManager.PlaySound(SoundManager.SoundType.Button);
            PunchButton(buttonAddItem);
            gameplayService?.AddRandomEquipmentItem();
        }

        private void OnRemove()
        {
            SoundManager.PlaySound(SoundManager.SoundType.Button);
            PunchButton(buttonRemoveItem);
            gameplayService?.DeleteRandomOccupiedSlot();
        }

        private void OnAddCoins()
        {
            SoundManager.PlaySound(SoundManager.SoundType.Button);
            PunchButton(buttonAddCoins);
            gameplayService?.AddCoinsRandom();
        }

        private static void PunchButton(Button btn)
        {
            if (btn == null)
                return;
            var rt = btn.transform as RectTransform;
            if (rt == null)
                return;
            Tween.StopAll(rt);
            Tween.Scale(rt, SlotPunchScale, SlotPunchDuration, Ease.OutQuad, cycles: 2, cycleMode: CycleMode.Yoyo);
        }

        private void OnEconomyChanged() => RefreshHeader();

        private void OnInventoryChanged() => RefreshAll();

        /// <summary>
        /// Начало драга: только непустой открытый слот. Возвращает false, если перетаскивание не начинается.
        /// </summary>
        public bool TryBeginSlotDrag(int slotIndex, out Sprite icon)
        {
            icon = null;
            if (gameplayService == null || _spawnedSlots == null)
                return false;

            var state = gameplayService.State;
            if (!state.IsSlotUnlocked(slotIndex))
                return false;
            if (state.GetSlot(slotIndex).IsEmpty)
                return false;

            var view = _spawnedSlots[slotIndex];
            if (view != null && view.iconImage != null)
                icon = view.iconImage.sprite;

            _dragFromIndex = slotIndex;
            _dragActive = true;

            if (itemDetailPopup != null)
                itemDetailPopup.Hide();

            if (dragGhostImage != null)
            {
                var ghostRt = dragGhostImage.rectTransform;
                Tween.StopAll(ghostRt);
                dragGhostImage.raycastTarget = false;
                dragGhostImage.sprite = icon;
                dragGhostImage.preserveAspect = true;
                dragGhostImage.enabled = icon != null;
                ghostRt.sizeDelta = dragGhostSize;
                ghostRt.localScale = Vector3.one * GhostShowFromScale;
                ghostRt.gameObject.SetActive(true);
                Tween.Scale(ghostRt, Vector3.one, GhostShowDuration, Ease.OutBack);
            }

            return true;
        }

        public void OnSlotDrag(PointerEventData eventData)
        {
            if (!_dragActive || dragGhostImage == null || dragCanvas == null)
                return;

            var canvasRt = (RectTransform)dragCanvas.transform;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRt,
                    eventData.position,
                    GetCanvasCamera(),
                    out var local))
                dragGhostImage.rectTransform.localPosition = local;
        }

        public void OnSlotEndDrag(PointerEventData eventData)
        {
            if (!_dragActive)
                return;

            _dragActive = false;
            int target = FindSlotIndexUnderScreenPoint(eventData.position);
            if (target >= 0 && _dragFromIndex >= 0 && gameplayService != null)
                gameplayService.TryApplyDragBetweenSlots(_dragFromIndex, target);

            _dragFromIndex = -1;

            if (dragGhostImage != null)
            {
                var ghostRt = dragGhostImage.rectTransform;
                Tween.StopAll(ghostRt);
                ghostRt.localScale = Vector3.one;
                dragGhostImage.gameObject.SetActive(false);
                dragGhostImage.sprite = null;
            }
        }

        private int FindSlotIndexUnderScreenPoint(Vector2 screenPoint)
        {
            if (_spawnedSlots == null)
                return -1;

            var cam = GetCanvasCamera();
            for (int i = _spawnedSlots.Length - 1; i >= 0; i--)
            {
                var view = _spawnedSlots[i];
                if (view == null)
                    continue;
                var rt = (RectTransform)view.transform;
                if (RectTransformUtility.RectangleContainsScreenPoint(rt, screenPoint, cam))
                    return i;
            }

            return -1;
        }

        private Camera GetCanvasCamera()
        {
            if (dragCanvas != null && dragCanvas.renderMode == RenderMode.ScreenSpaceCamera)
                return dragCanvas.worldCamera;
            return null;
        }

        /// <summary>
        /// Клик по слоту: замок — покупка; пустой — закрыть попап; с предметом — карточка.
        /// </summary>
        public void OnSlotClicked(int slotIndex)
        {
            if (gameplayService == null)
                return;

            var state = gameplayService.State;
            if (!state.IsSlotUnlocked(slotIndex))
            {
                gameplayService.TryUnlockSlotAtIndex(slotIndex);
                if (itemDetailPopup != null)
                    itemDetailPopup.Hide();
                return;
            }

            var entry = state.GetSlot(slotIndex);
            if (entry.IsEmpty)
            {
                if (itemDetailPopup != null)
                    itemDetailPopup.Hide();
                return;
            }

            var def = gameplayService.Registry != null
                ? gameplayService.Registry.GetById(entry.ItemId)
                : null;
            if (itemDetailPopup != null)
                itemDetailPopup.Show(def, entry.Count);
        }

        public void RefreshAll()
        {
            RefreshHeader();
            RefreshSlots();
        }

        private void RefreshHeader()
        {
            if (gameplayService == null)
                return;

            if (coinsText != null)
                coinsText.text = $"Монеты: {gameplayService.State.Coins}";

            if (weightText != null)
            {
                float w = gameplayService.GetTotalWeight();
                weightText.text = $"Вес: {w:0.###}";
                if (!float.IsNaN(_lastWeightShown) && Mathf.Abs(w - _lastWeightShown) > 0.0001f)
                {
                    var wrt = weightText.rectTransform;
                    Tween.StopAll(wrt);
                    Tween.Scale(wrt, SlotPunchScale, SlotPunchDuration, Ease.OutQuad, cycles: 2, cycleMode: CycleMode.Yoyo);
                }

                _lastWeightShown = w;
            }
        }

        private void RefreshSlots()
        {
            if (_spawnedSlots == null || gameplayService == null)
                return;

            var state = gameplayService.State;
            var cfg = gameplayService.Config;
            var reg = gameplayService.Registry;
            for (int i = 0; i < _spawnedSlots.Length; i++)
            {
                if (_spawnedSlots[i] != null)
                    _spawnedSlots[i].Refresh(state, cfg, reg, lockIconSprite);
            }
        }
    }
}
