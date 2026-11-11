using UnityEngine;

public class Bed : MonoBehaviour, IInteractable
{
    public string Prompt => "[E] Лечь спать (закончить день)";

    public void Interact(PlayerInventory player)
    {
        if (DayManager.Instance != null) DayManager.Instance.Sleep();
    }
}