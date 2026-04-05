# loot-craft

## Сохранение

- Формат: **JSON** в `Application.persistentDataPath`, файл **`game_save.json`**.
- Сохраняются монеты, содержимое слотов и число разблокированных слотов.
- В редакторе сброс: **Tools → Inventory → Clear Save File (Editor)**.

## Конфигурация (ScriptableObjects)

| Ассет | Назначение |
|-------|------------|
| `InventoryConfig` | Число слотов (50), стартовые открытые (15), стоимости разблокировки |
| `ItemRegistry` | Список всех `ItemDefinition` |
| `ItemDefinition` | Один тип предмета: id, вид, вес, стак, урон/защита, патроны для оружия |

Правки в инспекторе сразу влияют на логику после перезапуска сцены (сохранённое состояние подхватывается из JSON).

## Структура кода (кратко)

- `Assets/Scripts/Core/` — игровая логика и состояние инвентаря (`InventoryGameplayService`, `InventoryRuntimeState`)
- `Assets/Scripts/Data/` — ScriptableObjects
- `Assets/Scripts/Persistence/` — загрузка/сохранение JSON
- `Assets/Scripts/UI/` — HUD, слоты, драг, попап предмета
- `Assets/Scripts/Audio/` — `SoundManager` (клипы и объект в сцене настраиваются вручную)

## Затраченное время

| Дата | Интервал | Длительность |
|------|----------|----------------|
| 3 апреля 2026 | 19:30–21:30 | 2 ч |
| 4 апреля 2026 | 07:00–08:30 | 1 ч 30 мин |

**Итого: 3 ч 30 мин** (3,5 ч).