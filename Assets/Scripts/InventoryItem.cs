using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro; // For TextMeshPro UI elements
[System.Serializable]
public class InventoryItem
{
    public string itemName;
    public Sprite icon;
    public int quantity;
    public string description;
    public bool isStackable = true;
    public int maxStackSize = 99;
    public ItemRarity rarity = ItemRarity.Common;
    public GameObject itemPrefab; // Reference to the actual item in the world
    
    // Constructor for creating items
    public InventoryItem(string name, Sprite itemIcon, string desc, ItemRarity itemRarity = ItemRarity.Common, bool stackable = true, int maxStack = 99)
    {
        itemName = name;
        icon = itemIcon;
        quantity = 1;
        description = desc;
        isStackable = stackable;
        maxStackSize = maxStack;
        rarity = itemRarity;
    }
    
    // Clone method for creating copies
    public InventoryItem Clone()
    {
        InventoryItem clone = new InventoryItem(itemName, icon, description, rarity, isStackable, maxStackSize);
        clone.quantity = quantity;
        clone.itemPrefab = itemPrefab;
        return clone;
    }
}

// Rarity levels for items
public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}