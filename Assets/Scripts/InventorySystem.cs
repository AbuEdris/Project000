using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro; // For TextMeshPro UI elements
public class InventorySystem : MonoBehaviour
{
    [Header("Inventory Setup")]
    [SerializeField] private int maxSlots = 4;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform slotsParent;
    
    [Header("UI Elements")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI tooltipText;
    [SerializeField] private Image tooltipImage;
    [SerializeField] private TextMeshProUGUI tooltipRarity;
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject highlightEffect;
    [SerializeField] private GameObject itemAddedEffect;
    [SerializeField] private AudioSource inventoryAudio;
    [SerializeField] private AudioClip itemPickupSound;
    [SerializeField] private AudioClip itemDropSound;
    [SerializeField] private AudioClip itemSwapSound;
    
    // Color mappings for rarity levels
    private Dictionary<ItemRarity, Color> rarityColors = new Dictionary<ItemRarity, Color>
    {
        { ItemRarity.Common, new Color(0.8f, 0.8f, 0.8f) },
        { ItemRarity.Uncommon, new Color(0.0f, 0.8f, 0.0f) },
        { ItemRarity.Rare, new Color(0.0f, 0.5f, 1.0f) },
        { ItemRarity.Epic, new Color(0.8f, 0.0f, 0.8f) },
        { ItemRarity.Legendary, new Color(1.0f, 0.5f, 0.0f) }
    };
    
    // List to store all slots
    private List<InventorySlot> slots = new List<InventorySlot>();
    
    // Currently selected slot index
    private int selectedSlotIndex = -1;
    
    // Dragging system
    private InventoryItem draggedItem;
    private int dragOriginIndex = -1;
    
    void Start()
    {
        InitializeInventory();
    }
    
    void Update()
    {
        // Handle numeric key input for quick selection
        for (int i = 0; i < maxSlots && i < 10; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
            {
                SelectSlot(i);
            }
        }
        
        // Escape to cancel dragging
        if (Input.GetKeyDown(KeyCode.Escape) && draggedItem != null)
        {
            CancelDrag();
        }
    }
    
    void InitializeInventory()
    {
        // Clear existing slots if any
        foreach (Transform child in slotsParent)
        {
            Destroy(child.gameObject);
        }
        slots.Clear();
        
        // Create inventory slots
        for (int i = 0; i < maxSlots; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotsParent);
            InventorySlot slot = slotObj.GetComponent<InventorySlot>();
            if (slot != null)
            {
                slot.SetIndex(i);
                int slotIndex = i; // Local var for closure
                
                // Setup button click event
                Button slotButton = slotObj.GetComponent<Button>();
                if (slotButton != null)
                {
                    slotButton.onClick.AddListener(() => OnSlotClicked(slotIndex));
                }
                
                slots.Add(slot);
            }
        }
        
        // Hide tooltip initially
        tooltipPanel.SetActive(false);
        
        // Start with first slot selected
        if (slots.Count > 0)
        {
            SelectSlot(0);
        }
    }
    
    public void AddItem(InventoryItem item)
    {
        // First try to stack with existing items
        if (item.isStackable)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                InventorySlot slot = slots[i];
                if (slot.HasItem() && slot.GetItem().itemName == item.itemName && slot.GetItem().quantity < slot.GetItem().maxStackSize)
                {
                    // Calculate how many we can add to this stack
                    int currentQty = slot.GetItem().quantity;
                    int maxAdd = slot.GetItem().maxStackSize - currentQty;
                    int addAmount = Mathf.Min(item.quantity, maxAdd);
                    
                    // Update quantity
                    slot.GetItem().quantity += addAmount;
                    slot.UpdateUI();
                    
                    // Play particle effect
                    ShowItemAddedEffect(i);
                    
                    // Play sound
                    if (inventoryAudio && itemPickupSound)
                    {
                        inventoryAudio.PlayOneShot(itemPickupSound);
                    }
                    
                    // Subtract from original
                    item.quantity -= addAmount;
                    
                    // If we added all, we're done
                    if (item.quantity <= 0)
                        return;
                }
            }
        }
        
        // Find first empty slot for remaining items
        for (int i = 0; i < slots.Count; i++)
        {
            if (!slots[i].HasItem())
            {
                // Clone the item before adding to inventory
                InventoryItem newItem = item.Clone();
                slots[i].SetItem(newItem);
                
                // Play particle effect
                ShowItemAddedEffect(i);
                
                // Play sound
                if (inventoryAudio && itemPickupSound)
                {
                    inventoryAudio.PlayOneShot(itemPickupSound);
                }
                
                return;
            }
        }
        
        // Inventory is full
        Debug.Log("Inventory is full! Couldn't add " + item.itemName);
    }
    
    public void RemoveItem(int slotIndex, int amount = 1)
    {
        if (slotIndex >= 0 && slotIndex < slots.Count && slots[slotIndex].HasItem())
        {
            InventoryItem item = slots[slotIndex].GetItem();
            item.quantity -= amount;
            
            if (item.quantity <= 0)
            {
                slots[slotIndex].ClearSlot();
            }
            else
            {
                slots[slotIndex].UpdateUI();
            }
            
            // Play sound
            if (inventoryAudio && itemDropSound)
            {
                inventoryAudio.PlayOneShot(itemDropSound);
            }
        }
    }
    
    public InventoryItem GetSelectedItem()
    {
        if (selectedSlotIndex >= 0 && selectedSlotIndex < slots.Count)
        {
            return slots[selectedSlotIndex].GetItem();
        }
        return null;
    }
    
    public void SelectSlot(int index)
    {
        // Deselect current slot
        if (selectedSlotIndex >= 0 && selectedSlotIndex < slots.Count)
        {
            slots[selectedSlotIndex].SetSelected(false);
        }
        
        // Select new slot
        selectedSlotIndex = index;
        
        if (selectedSlotIndex >= 0 && selectedSlotIndex < slots.Count)
        {
            slots[selectedSlotIndex].SetSelected(true);
            
            // If this slot has an item, show tooltip
            if (slots[selectedSlotIndex].HasItem())
            {
                ShowTooltip(slots[selectedSlotIndex].GetItem());
            }
            else
            {
                HideTooltip();
            }
        }
    }
    
    public void ShowTooltip(InventoryItem item)
    {
        if (item == null || tooltipPanel == null) return;
        
        tooltipText.text = "<b>" + item.itemName + "</b>";
        if (item.isStackable)
        {
            tooltipText.text += " (" + item.quantity + ")";
        }
        tooltipText.text += "\n\n" + item.description;
        
        tooltipImage.sprite = item.icon;
        tooltipRarity.text = item.rarity.ToString();
        tooltipRarity.color = rarityColors[item.rarity];
        
        tooltipPanel.SetActive(true);
    }
    
    public void HideTooltip()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }
    
    private void OnSlotClicked(int index)
    {
        if (draggedItem != null)
        {
            // We're dragging an item, so place it here
            DropItemIntoSlot(index);
        }
        else if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            // Shift+Click to split stack
            SplitStack(index);
        }
        else
        {
            // Standard click, select slot
            SelectSlot(index);
            
            // Start drag if slot has an item
            if (slots[index].HasItem())
            {
                BeginDrag(index);
            }
        }
    }
    
    private void BeginDrag(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < slots.Count && slots[slotIndex].HasItem())
        {
            draggedItem = slots[slotIndex].GetItem();
            dragOriginIndex = slotIndex;
            
            // Visual feedback for drag
            slots[slotIndex].SetDragging(true);
        }
    }
    
    private void DropItemIntoSlot(int targetIndex)
    {
        if (targetIndex < 0 || targetIndex >= slots.Count || draggedItem == null || dragOriginIndex < 0)
            return;
        
        InventorySlot targetSlot = slots[targetIndex];
        InventorySlot originSlot = slots[dragOriginIndex];
        
        // Reset dragging state
        originSlot.SetDragging(false);
        
        // If target slot is empty, just move the item
        if (!targetSlot.HasItem())
        {
            targetSlot.SetItem(draggedItem);
            originSlot.ClearSlot();
            
            // Play sound
            if (inventoryAudio && itemSwapSound)
            {
                inventoryAudio.PlayOneShot(itemSwapSound);
            }
        }
        // If target has the same item and both are stackable
        else if (targetSlot.GetItem().itemName == draggedItem.itemName && 
                 draggedItem.isStackable && targetSlot.GetItem().isStackable)
        {
            // Try to stack
            int availableSpace = targetSlot.GetItem().maxStackSize - targetSlot.GetItem().quantity;
            int amountToMove = Mathf.Min(availableSpace, draggedItem.quantity);
            
            if (amountToMove > 0)
            {
                targetSlot.GetItem().quantity += amountToMove;
                draggedItem.quantity -= amountToMove;
                
                if (draggedItem.quantity <= 0)
                {
                    // All items moved
                    originSlot.ClearSlot();
                }
                else
                {
                    // Some items left
                    originSlot.UpdateUI();
                }
                
                targetSlot.UpdateUI();
                
                // Play sound
                if (inventoryAudio && itemSwapSound)
                {
                    inventoryAudio.PlayOneShot(itemSwapSound);
                }
            }
        }
        // Different items or not stackable, so swap
        else
        {
            InventoryItem targetItem = targetSlot.GetItem();
            targetSlot.SetItem(draggedItem);
            originSlot.SetItem(targetItem);
            
            // Play sound
            if (inventoryAudio && itemSwapSound)
            {
                inventoryAudio.PlayOneShot(itemSwapSound);
            }
        }
        
        // End drag
        draggedItem = null;
        dragOriginIndex = -1;
        
        // Update tooltip if needed
        if (selectedSlotIndex == targetIndex && targetSlot.HasItem())
        {
            ShowTooltip(targetSlot.GetItem());
        }
        else if (selectedSlotIndex == dragOriginIndex && originSlot.HasItem())
        {
            ShowTooltip(originSlot.GetItem());
        }
    }
    
    private void CancelDrag()
    {
        if (dragOriginIndex >= 0 && dragOriginIndex < slots.Count)
        {
            slots[dragOriginIndex].SetDragging(false);
        }
        
        draggedItem = null;
        dragOriginIndex = -1;
    }
    
    private void SplitStack(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count || !slots[slotIndex].HasItem())
            return;
        
        InventoryItem sourceItem = slots[slotIndex].GetItem();
        
        // Need at least 2 items to split
        if (!sourceItem.isStackable || sourceItem.quantity < 2)
            return;
        
        // Find empty slot
        int emptySlot = -1;
        for (int i = 0; i < slots.Count; i++)
        {
            if (!slots[i].HasItem())
            {
                emptySlot = i;
                break;
            }
        }
        
        if (emptySlot == -1)
        {
            Debug.Log("No empty slot to split into!");
            return;
        }
        
        // Calculate split amount (half, rounded up)
        int amountToSplit = Mathf.CeilToInt(sourceItem.quantity / 2f);
        
        // Create new item with split amount
        InventoryItem newItem = sourceItem.Clone();
        newItem.quantity = amountToSplit;
        
        // Reduce original stack
        sourceItem.quantity -= amountToSplit;
        slots[slotIndex].UpdateUI();
        
        // Add new stack to empty slot
        slots[emptySlot].SetItem(newItem);
        
        // Play sound
        if (inventoryAudio && itemSwapSound)
        {
            inventoryAudio.PlayOneShot(itemSwapSound);
        }
    }
    
    private void ShowItemAddedEffect(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < slots.Count && itemAddedEffect != null)
        {
            // Instantiate effect at slot position
            RectTransform slotRect = slots[slotIndex].GetComponent<RectTransform>();
            if (slotRect)
            {
                GameObject effect = Instantiate(itemAddedEffect, slotRect.position, Quaternion.identity, slotsParent);
                Destroy(effect, 1.0f); // Destroy after animation
            }
        }
    }
}