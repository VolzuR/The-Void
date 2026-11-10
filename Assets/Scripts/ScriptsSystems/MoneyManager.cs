using System;
using UnityEngine;

public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance { get; private set; }

    public int money = 100;
    public int minMoney = -1000;
    public int maxMoney = 8000;

    public event Action<int> OnMoneyChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Add(int amount)
    {
        money = Mathf.Clamp(money + amount, minMoney, maxMoney);
        OnMoneyChanged?.Invoke(money);
    }
}