// Assets/Scripts/Items/QuickbarUI.cs
using UnityEngine;
using UnityEngine.UI;

public class QuickbarUI : MonoBehaviour
{
    [Header("Slot icon images (5 adet)")]
    public Image[] slotIcons = new Image[5];

    [Header("Highlight (opsiyonel)")]
    public Image[] slotFrames; // seçili slotu farklý renk/alpha ile göster

    [Header("Refs")]
    public SingleUseInventory inventory;

    void Awake()
    {
        if (!inventory) inventory = FindObjectOfType<SingleUseInventory>();
        if (inventory) inventory.OnChanged += Refresh;
    }

    void OnDestroy()
    {
        if (inventory) inventory.OnChanged -= Refresh;
    }

    void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (!inventory) return;

        var slots = inventory.Slots;
        for (int i = 0; i < slotIcons.Length; i++)
        {
            var hasItem = (i < slots.Length && slots[i] != null);
            slotIcons[i].sprite = hasItem ? slots[i].icon : null;
            slotIcons[i].enabled = hasItem;

            if (slotFrames != null && i < slotFrames.Length && slotFrames[i])
            {
                var c = slotFrames[i].color;
                c.a = (i == inventory.SelectedIndex) ? 1f : 0.25f;
                slotFrames[i].color = c;
            }
        }
    }
}
