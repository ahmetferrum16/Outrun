// Assets/Scripts/Items/SingleUseItem.cs
using UnityEngine;

[CreateAssetMenu(fileName = "SUI_", menuName = "SingleUse/Item")]
public class SingleUseItem : ScriptableObject
{
    public enum ItemType { Chocolate, Nuke, WallBlock, Food, GoldFood }

    [Header("Info")]
    public string displayName;
    public Sprite icon;
    public ItemType type;

    [Header("Optional Params")]
    public int intParam;      // örn: healAmount
    public float floatParam;  // örn: radius, power vs.

    // SingleUseItem sýnýfýnýn içine EKLE:
    public GameObject pickupPrefab; // Bu item yere düþerken kullanýlacak prefab

}
