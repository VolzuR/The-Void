using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public List<Package> packages = new List<Package>();
    public int selectedIndex = -1;

    [Header("Бросок")]
    public Transform dropOrigin;          // камера; если пусто, берётся Main Camera
    public float throwForce = 4f;
    public KeyCode dropKey = KeyCode.G;

    public event Action OnChanged;

    public string Message { get; private set; }
    public float MessageUntil { get; private set; }

    public Package Held
    {
        get
        {
            if (selectedIndex >= 0 && selectedIndex < packages.Count) return packages[selectedIndex];
            return null;
        }
    }

    void Update()
    {
        if (PlayerController.inputLocked) return;
        if (Input.GetKeyDown(dropKey)) DropHeld();
    }

    public void AddPackage(Package p)
    {
        packages.Add(p);
        p.Stow(transform);
        selectedIndex = packages.Count - 1;
        Say("Взял посылку для " + p.destinationId);
        OnChanged?.Invoke();
    }

    public void Remove(Package p)
    {
        packages.Remove(p);
        selectedIndex = Mathf.Min(selectedIndex, packages.Count - 1);
        OnChanged?.Invoke();
    }

    public void DropHeld()
    {
        Package p = Held;
        if (p == null) return;

        Transform o = dropOrigin ? dropOrigin : (Camera.main ? Camera.main.transform : transform);
        Vector3 pos = o.position + o.forward * 0.9f - o.up * 0.2f;

        // если перед лицом стена, кладём ближе
        if (Physics.Raycast(o.position, o.forward, out RaycastHit h, 0.9f, ~0, QueryTriggerInteraction.Ignore)
            && !h.collider.transform.IsChildOf(transform))
            pos = o.position + o.forward * Mathf.Max(0.2f, h.distance - 0.3f) - o.up * 0.2f;

        packages.Remove(p);
        selectedIndex = Mathf.Min(selectedIndex, packages.Count - 1);
        p.Release(pos, o.rotation, o.forward * throwForce);

        Say("Выбросил посылку");
        OnChanged?.Invoke();
    }

    // новый день: убираем всё, что осталось
    public int ClearAll()
    {
        int n = packages.Count;
        foreach (Package p in packages) if (p) Destroy(p.gameObject);
        packages.Clear();
        selectedIndex = -1;
        OnChanged?.Invoke();
        return n;
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