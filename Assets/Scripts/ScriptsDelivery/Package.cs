using UnityEngine;

public class Package : MonoBehaviour, IInteractable
{
    public string destinationId = "House_A";
    public int reward = 50;

    public string Prompt => "[E] Взять посылку (для " + destinationId + ")";

    public void Interact(PlayerInventory player)
    {
        player.AddPackage(this);
    }

    // убираем посылку из мира в инвентарь (объект сохраняется, но выключен)
    public void Stow(Transform parent)
    {
        transform.SetParent(parent, false);
        gameObject.SetActive(false);
    }
}