using UnityEngine;

public class DeliveryPoint : MonoBehaviour, IInteractable
{
    public string destinationId = "House_A";

    public string Prompt => "[E] Отдать посылку";

    public void Interact(PlayerInventory player)
    {
        Package p = player.FindFor(destinationId);
        if (p == null)
        {
            Debug.Log("У тебя нет посылки для " + destinationId);
            return;
        }

        player.Remove(p);
        MoneyManager.Instance.Add(p.reward);
        Destroy(p.gameObject);
    }
}