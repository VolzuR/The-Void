using UnityEngine;

public class Interactor : MonoBehaviour
{
    public float range = 3f;
    public PlayerInventory inventory;
    public IInteractable Current { get; private set; }

    void Update()
    {
        Current = null;
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, range))
            Current = hit.collider.GetComponentInParent<IInteractable>();

        if (Current != null && Input.GetKeyDown(KeyCode.E))
            Current.Interact(inventory);
    }
}