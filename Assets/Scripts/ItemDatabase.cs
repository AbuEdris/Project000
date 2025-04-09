// Example Item Database - Create a separate script with this to easily spawn items
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro; // For TextMeshPro UI elements
public class ItemDatabase : MonoBehaviour
{
    // Singleton pattern
    public static ItemDatabase Instance { get; private set; }
    
    [Header("Item Icons")]
    public Sprite swordIcon;
    public Sprite potionIcon;
    public Sprite coinIcon;
    public Sprite armorIcon;
    public Sprite scrollIcon;
    
    private Dictionary<string, InventoryItem> itemTemplates = new Dictionary<string, InventoryItem>();
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeItems();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializeItems()
    {
        // Initialize item templates
        itemTemplates.Add("Sword", new InventoryItem(
            "Steel Sword", 
            swordIcon, 
            "A sharp steel sword that deals moderate damage.", 
            ItemRarity.Uncommon, 
            false
        ));
        
        itemTemplates.Add("Potion", new InventoryItem(
            "Health Potion", 
            potionIcon, 
            "Restores 50 health points when consumed.", 
            ItemRarity.Common, 
            true, 
            10
        ));
        
        itemTemplates.Add("Coin", new InventoryItem(
            "Gold Coin", 
            coinIcon, 
            "Standard currency accepted by all merchants.", 
            ItemRarity.Common, 
            true, 
            999
        ));
        
        itemTemplates.Add("Armor", new InventoryItem(
            "Plate Armor", 
            armorIcon, 
            "Heavy armor that provides excellent protection.", 
            ItemRarity.Rare, 
            false
        ));
        
        itemTemplates.Add("Scroll", new InventoryItem(
            "Magic Scroll", 
            scrollIcon, 
            "Contains a powerful spell that can be cast once.", 
            ItemRarity.Epic, 
            true, 
            5
        ));
    }
    
    public InventoryItem GetItem(string itemName, int quantity = 1)
    {
        if (itemTemplates.ContainsKey(itemName))
        {
            InventoryItem item = itemTemplates[itemName].Clone();
            item.quantity = quantity;
            return item;
        }
        
        Debug.LogWarning("Item not found in database: " + itemName);
        return null;
    }
}