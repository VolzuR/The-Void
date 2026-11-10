using UnityEngine;

public class DebugHUD : MonoBehaviour
{
    public Interactor interactor;
    public PlayerInventory inventory;
    public bool show = true;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1)) show = !show;
    }

    void OnGUI()
    {
        // прицел-точка всегда виден
        GUI.DrawTexture(new Rect(Screen.width / 2 - 2, Screen.height / 2 - 2, 4, 4), Texture2D.whiteTexture);

        if (!show) return;

        if (MoneyManager.Instance != null)
            GUI.Label(new Rect(10, 10, 400, 25), "Деньги: " + MoneyManager.Instance.money);

        GUI.Label(new Rect(10, 30, 400, 25), "Посылок с собой: " + inventory.packages.Count);

        if (interactor != null && interactor.Current != null)
            GUI.Label(new Rect(Screen.width / 2 - 150, Screen.height / 2 + 20, 400, 25), interactor.Current.Prompt);
    }
}