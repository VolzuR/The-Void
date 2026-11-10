using UnityEngine;

public class DeliveryPoint : MonoBehaviour, IInteractable
{
    public string destinationId = "House_A";
    public string Prompt => "[E] Отдать посылку";

    public void Interact(PlayerInventory player)
    {
        Package held = player.Held;

        if (held == null)
        {
            player.Say("У тебя нет посылки. Открой инвентарь (I)");
            return;
        }

        if (held.destinationId != destinationId)
        {
            player.Say("Эта посылка не для этого дома. Выбери другую (I)");
            return;
        }

        int reward = held.reward;
        player.Remove(held);
        Destroy(held.gameObject);
        MoneyManager.Instance.Add(reward);
        player.Say("Доставлено! +" + reward);
    }
}