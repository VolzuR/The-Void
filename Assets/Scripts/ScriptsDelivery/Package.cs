using UnityEngine;

public class Package : MonoBehaviour, IInteractable
{
    public string destinationId = "House_A";
    public int reward = 50;

    public string Prompt => "[E] Взять посылку (для " + destinationId + ")";

    public void Interact(PlayerInventory player)
    {
        player.AddPackage(this);
        gameObject.SetActive(false); // убираем из мира, но объект сохраняется в инвентаре
    }
}