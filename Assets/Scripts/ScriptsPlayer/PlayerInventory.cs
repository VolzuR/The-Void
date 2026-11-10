using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public List<Package> packages = new List<Package>();
    public int selectedIndex = -1;       // -1 = инвентарь пуст

    public event Action OnChanged;

    // короткие сообщения игроку
    public string Message { get; private set; }
    public float MessageUntil { get; private set; }

    // предмет, который сейчас "в руках"
    public Package Held
    {
        get
        {
            if (selectedIndex >= 0 && selectedIndex < packages.Count) return packages[selectedIndex];
            return null;
        }
    }

    public void AddPackage(Package p)
    {
        packages.Add(p);
        p.Stow(transform);                    // прячем посылку в инвентарь
        selectedIndex = packages.Count - 1;   // новая сразу становится выбранной
        Say("Взял посылку для " + p.destinationId);
        OnChanged?.Invoke();
    }

    public void Remove(Package p)
    {
        packages.Remove(p);
        selectedIndex = Mathf.Min(selectedIndex, packages.Count - 1);
        OnChanged?.Invoke();
    }

    public void Select(int step)
    {
        if (packages.Count == 0) return;
        selectedIndex = (selectedIndex + step + packages.Count) % packages.Count;
        OnChanged?.Invoke();
    }

    public void Say(string text)
    {
        Message = text;
        MessageUntil = Time.time + 2.5f;
    }
}