
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro; // For TextMeshPro UI elements
[System.Serializable]
public class InventorySlot : MonoBehaviour
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private GameObject selectionHighlight;
    [SerializeField] private GameObject emptySlotOverlay;
    [SerializeField] private Image borderImage;
    [SerializeField] private Image backgroundImage;
    
    private InventoryItem item;
    private int slotIndex;
    private bool isSelected = false;
    
    private Color normalColor = new Color(1, 1, 1, 0.5f);
    private Color selectedColor = new Color(1, 1, 1, 1f);
    private Color draggingColor = new Color(0.8f, 0.8f, 0.8f, 0.3f);
    
    void Start()
    {
        UpdateUI();
    }
    
    public void SetIndex(int index)
    {
        slotIndex = index;
    }
    
    public void SetItem(InventoryItem newItem)
    {
        item = newItem;
        UpdateUI();
    }
    
    public InventoryItem GetItem()
    {
        return item;
    }
    
    public bool HasItem()
    {
        return item != null;
    }
    
    public void ClearSlot()
    {
        item = null;
        UpdateUI();
    }
    
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        
        if (selectionHighlight != null)
            selectionHighlight.SetActive(selected);
        
        // Update border color
        if (borderImage != null)
        {
            borderImage.color = selected ? selectedColor : normalColor;
        }
        
        // Make slot more visible when selected
        if (backgroundImage != null)
        {
            backgroundImage.color = selected ? new Color(0.3f, 0.3f, 0.3f, 0.9f) : new Color(0.2f, 0.2f, 0.2f, 0.7f);
        }
    }
    
    public void SetDragging(bool isDragging)
    {
        if (itemIcon != null)
        {
            itemIcon.color = isDragging ? draggingColor : Color.white;
        }
    }
    
    public void UpdateUI()
    {
        if (item != null)
        {
            // Show item
            itemIcon.gameObject.SetActive(true);
            itemIcon.sprite = item.icon;
            
            // Set rarity border color if we have one
            if (borderImage != null)
            {
                Dictionary<ItemRarity, Color> rarityBorders = new Dictionary<ItemRarity, Color>
                {
                    { ItemRarity.Common, new Color(0.8f, 0.8f, 0.8f) },
                    { ItemRarity.Uncommon, new Color(0.0f, 0.8f, 0.0f) },
                    { ItemRarity.Rare, new Color(0.0f, 0.5f, 1.0f) },
                    { ItemRarity.Epic, new Color(0.8f, 0.0f, 0.8f) },
                    { ItemRarity.Legendary, new Color(1.0f, 0.5f, 0.0f) }
                };
                
                Color rarityColor = rarityBorders.ContainsKey(item.rarity) ? 
                                    rarityBorders[item.rarity] : rarityBorders[ItemRarity.Common];
                                    
                borderImage.color = isSelected ? 
                                    Color.Lerp(rarityColor, Color.white, 0.5f) : 
                                    rarityColor;
            }
            
            // Show quantity if stackable
            if (item.isStackable && item.quantity > 1)
            {
                quantityText.gameObject.SetActive(true);
                quantityText.text = item.quantity.ToString();
            }
            else
            {
                quantityText.gameObject.SetActive(false);
            }
            
            // Hide empty overlay
            if (emptySlotOverlay != null)
                emptySlotOverlay.SetActive(false);
        }
        else
        {
            // No item
            itemIcon.gameObject.SetActive(false);
            quantityText.gameObject.SetActive(false);
            
            // Show empty overlay
            if (emptySlotOverlay != null)
                emptySlotOverlay.SetActive(true);
            
            // Reset border color
            if (borderImage != null)
            {
                borderImage.color = isSelected ? selectedColor : normalColor;
            }
        }
    }
}