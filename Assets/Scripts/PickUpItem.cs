using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Collider2D))]
public class PickupItem : MonoBehaviour
{
    [Header("Data")]
    public SingleUseItem item;
    [Header("Visual (opsiyonel)")]
    public SpriteRenderer spriteRenderer;
    public Image uiImage;
    [Header("Pickup")]
    [Min(0f)] public float promptDistance = 1.2f;
    public KeyCode pickupKey = KeyCode.E;

    private Transform player;
    private SingleUseInventory inv;

    void Awake()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p) player = p.transform;
        inv = FindObjectOfType<SingleUseInventory>();

        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
        if (!uiImage) uiImage = GetComponentInChildren<Image>(includeInactive: true);

        ApplyIcon();
    }

    void Update()
    {
        if (!player || !inv || item == null) return;
        float d = Vector2.Distance(player.position, transform.position);
        if (d <= promptDistance && Input.GetKeyDown(pickupKey))
        {
            if (inv.Add(item))
                Destroy(gameObject);
        }
    }

    public void SetItem(SingleUseItem newItem)
    {
        item = newItem;
        if (item) gameObject.name = $"Pickup_{item.name}";
        ApplyIcon();
    }

    private void ApplyIcon()
    {
        if (item == null) return;
        if (spriteRenderer)
        {
            spriteRenderer.sprite = item.icon;
            spriteRenderer.enabled = (item.icon != null);
        }
        if (uiImage)
        {
            uiImage.sprite = item.icon;
            uiImage.enabled = (item.icon != null);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, promptDistance);
    }
}