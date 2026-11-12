using UnityEngine;

public class DeliveryPoint : MonoBehaviour, IInteractable
{
    public string destinationId = "House_A";
    public string Prompt => "[E] Отдать посылку";

    public void Interact(PlayerInventory player)
    {
        Item held = player.Held;

        if (held == null)
        {
            player.Say("У тебя ничего нет в руках. Открой инвентарь (I)");
            return;
        }

        Package pkg = held as Package;
        if (pkg == null)
        {
            player.Say("Это не посылка");
            return;
        }

        if (pkg.destinationId != destinationId)
        {
            player.Say("Эта посылка не для этого дома. Выбери другую (I)");
            return;
        }

        int reward = pkg.reward;
        player.Remove(pkg);
        Destroy(pkg.gameObject);
        MoneyManager.Instance.Add(reward);
        player.Say("Доставлено! +" + reward);
    }
}