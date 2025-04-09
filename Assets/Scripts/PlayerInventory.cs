using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro; // For TextMeshPro UI elements

 // Example usage script for player
public class PlayerInventory : MonoBehaviour
{
    public InventorySystem inventory;
    
    // Example of how to pick up an item
    public void PickupItem(string itemName, int quantity = 1)
    {
        if (ItemDatabase.Instance != null)
        {
            InventoryItem item = ItemDatabase.Instance.GetItem(itemName, quantity);
            if (item != null)
            {
                inventory.AddItem(item);
            }
        }
    }
    
    // Used for testing
    void Update()
    {
        // Test item pickup with F key
        if (Input.GetKeyDown(KeyCode.F))
        {
            string[] itemTypes = { "Sword", "Potion", "Coin", "Armor", "Scroll" };
            string randomItem = itemTypes[Random.Range(0, itemTypes.Length)];
            PickupItem(randomItem, Random.Range(1, 5));
        }
        
        // Drop selected item with G key
        if (Input.GetKeyDown(KeyCode.G) && inventory != null)
        {
            InventoryItem selectedItem = inventory.GetSelectedItem();
            if (selectedItem != null)
            {
                // Implement dropping logic here
                Debug.Log("Dropping item: " + selectedItem.itemName);
                
                // Remove from inventory
                inventory.RemoveItem(slotIndex: 0);
            }
        }
    }
}