using System;
using UnityEngine;

public class SingleUseInventory : MonoBehaviour
{
    public const int SlotCount = 5;
    [SerializeField] private SingleUseItem[] slots = new SingleUseItem[SlotCount];
    [SerializeField] private int selectedIndex = 0;
    public Action OnChanged;
    public SingleUseItem[] Slots => slots;
    public int SelectedIndex => selectedIndex;

    [Header("Refs (optional)")]
    public PlayerMovement playerMovement;
    public PlayerStats playerStats;

    void Awake()
    {
        if (!playerMovement) playerMovement = FindObjectOfType<PlayerMovement>();
        if (!playerStats) playerStats = FindObjectOfType<PlayerStats>();

        if (slots == null || slots.Length != SlotCount)
            slots = new SingleUseItem[SlotCount];
        for (int i = 0; i < slots.Length; i++) slots[i] = null;
        selectedIndex = 0;
    }

    void Start() { OnChanged?.Invoke(); }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) Select(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) Select(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) Select(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) Select(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) Select(4);
        if (Input.GetKeyDown(KeyCode.F)) UseSelected();
    }

    public bool Add(SingleUseItem item)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                slots[i] = item;
                OnChanged?.Invoke();
                return true;
            }
        }
        return false;
    }

    public void Select(int idx)
    {
        idx = Mathf.Clamp(idx, 0, SlotCount - 1);
        if (selectedIndex != idx)
        {
            selectedIndex = idx;
            OnChanged?.Invoke();
        }
    }

    public void UseSelected()
    {
        var it = slots[selectedIndex];
        if (it == null) return;

        switch (it.type)
        {
            case SingleUseItem.ItemType.Chocolate:
                if (playerMovement) playerMovement.RefillStamina();
                break;
            case SingleUseItem.ItemType.Food:
                if (playerStats) playerStats.Heal(1);
                break;
            case SingleUseItem.ItemType.GoldFood:
                if (playerStats) playerStats.FullHeal();
                break;
            case SingleUseItem.ItemType.Nuke:
            case SingleUseItem.ItemType.WallBlock:
                // ileride eklenecek
                break;
        }

        slots[selectedIndex] = null;
        OnChanged?.Invoke();
    }
}