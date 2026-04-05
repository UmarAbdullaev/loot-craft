namespace LootCraft.Core
{
    /// <summary>
    /// Содержимое одного слота в рантайме (изменяется геймплеем).
    /// </summary>
    public class SlotEntry
    {
        public string ItemId = "";
        public int Count;

        public bool IsEmpty => string.IsNullOrEmpty(ItemId) || Count <= 0;

        public void Clear()
        {
            ItemId = "";
            Count = 0;
        }

        public void Set(string itemId, int count)
        {
            ItemId = string.IsNullOrEmpty(itemId) ? "" : itemId;
            Count = ItemId.Length == 0 ? 0 : count;
        }
    }
}
