using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public List<Item> items = new List<Item>();
    public int selectedIndex = -1;

    [Header("Вес")]
    public float maxWeight = 12f;        // кг

    [Header("Бросок")]
    public Transform dropOrigin;
    public float throwForce = 4f;
    public KeyCode dropKey = KeyCode.G;

    public event Action OnChanged;

    public string Message { get; private set; }
    public float MessageUntil { get; private set; }

    public float TotalWeight
    {
        get
        {
            float w = 0f;
            foreach (Item i in items) if (i) w += i.weight;
            return w;
        }
    }

    public Item Held
    {
        get
        {
            if (selectedIndex >= 0 && selectedIndex < items.Count) return items[selectedIndex];
            return null;
        }
    }

    void Update()
    {
        if (PlayerController.inputLocked) return;
        if (Input.GetKeyDown(dropKey)) DropHeld();
    }

    public bool TryAdd(Item it)
    {
        float free = maxWeight - TotalWeight;
        if (it.weight > free + 0.001f)
        {
            Say("Слишком тяжело: " + it.itemName + " весит " + it.weight.ToString("0.#")
                + " кг, свободно " + Mathf.Max(0f, free).ToString("0.#") + " кг");
            return false;
        }

        items.Add(it);
        it.Stow(transform);
        selectedIndex = items.Count - 1;
        Say("Взял: " + it.Title);
        OnChanged?.Invoke();
        return true;
    }

    public void Remove(Item it)
    {
        items.Remove(it);
        selectedIndex = Mathf.Min(selectedIndex, items.Count - 1);
        OnChanged?.Invoke();
    }

    public void DropHeld()
    {
        Item it = Held;
        if (it == null) return;

        Transform o = dropOrigin ? dropOrigin : (Camera.main ? Camera.main.transform : transform);
        Vector3 pos = o.position + o.forward * 0.9f - o.up * 0.2f;

        if (Physics.Raycast(o.position, o.forward, out RaycastHit h, 0.9f, ~0, QueryTriggerInteraction.Ignore)
            && !h.collider.transform.IsChildOf(transform))
            pos = o.position + o.forward * Mathf.Max(0.2f, h.distance - 0.3f) - o.up * 0.2f;

        items.Remove(it);
        selectedIndex = Mathf.Min(selectedIndex, items.Count - 1);
        it.Release(pos, o.rotation, o.forward * throwForce);

        Say("Выбросил: " + it.Title);
        OnChanged?.Invoke();
    }

    // новый день: убираем недоставленные посылки, инструменты остаются
    public int ClearPackages()
    {
        int n = 0;
        foreach (Item i in items)
        {
            if (i == null) continue;
            if (i is Package) { Destroy(i.gameObject); n++; }
        }
        items.RemoveAll(i => i == null || i is Package);
        selectedIndex = items.Count > 0 ? Mathf.Clamp(selectedIndex, 0, items.Count - 1) : -1;
        OnChanged?.Invoke();
        return n;
    }

    public void Select(int step)
    {
        if (items.Count == 0) return;
        selectedIndex = (selectedIndex + step + items.Count) % items.Count;
        OnChanged?.Invoke();
    }

    public void Say(string text)
    {
        Message = text;
        MessageUntil = Time.time + 2.5f;
    }
}