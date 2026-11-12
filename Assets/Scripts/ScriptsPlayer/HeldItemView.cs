using UnityEngine;

public class HeldItemView : MonoBehaviour
{
    public PlayerInventory inventory;
    public Transform handPoint;
    public float scale = 0.6f;

    GameObject visual;
    Item shown;

    void Awake()
    {
        if (!inventory) inventory = GetComponent<PlayerInventory>();
    }

    void OnEnable()  { if (inventory) inventory.OnChanged += Rebuild; }
    void OnDisable() { if (inventory) inventory.OnChanged -= Rebuild; }

    void Update()
    {
        if (visual) visual.SetActive(!PlayerController.inputLocked);
    }

    void Rebuild()
    {
        Item held = inventory.Held;
        if (held != null && held == shown && visual) return;

        if (visual) Destroy(visual);
        visual = null;
        shown = held;
        if (held == null || handPoint == null) return;

        visual = held.CreateVisualCopy(handPoint);
        visual.SetActive(true);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = held.transform.lossyScale * scale;
    }
}