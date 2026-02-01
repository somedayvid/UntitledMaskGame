using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Cards/Card Catalog")]
public class CardCatalog : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public int cardId;                 // 你已有 Card.ID
        public string displayName;         // UI显示名称
        public CardEffect effect;          // 你已有 enum
        public int chiCost = 1;            // 成本
        [TextArea] public string desc;     // 简短说明
        public Sprite art;                 // 可空
        public bool isConfusing = false;   // 你说的“先记下来，回头再说”
    }

    public List<Entry> entries = new List<Entry>();

    public Entry FindById(int id)
    {
        return entries.Find(e => e != null && e.cardId == id);
    }
}
