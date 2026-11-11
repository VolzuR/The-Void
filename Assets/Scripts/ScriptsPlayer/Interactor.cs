using System;
using UnityEngine;

public class Interactor : MonoBehaviour
{
    public float range = 3.5f;
    public float radius = 0.35f;            // толщина луча
    public PlayerInventory inventory;
    public bool debugLog = false;
    public IInteractable Current { get; private set; }

    void Update()
    {
        Current = null;
        if (PlayerController.inputLocked) return;

        Current = FindTarget();

        if (Current != null && Input.GetKeyDown(KeyCode.E))
            Current.Interact(inventory);
    }

    IInteractable FindTarget()
    {
        Vector3 origin = transform.position;
        Vector3 dir = transform.forward;

        RaycastHit[] hits = Physics.SphereCastAll(origin, radius, dir, range, ~0, QueryTriggerInteraction.Ignore);

        IInteractable best = null;
        float bestAngle = float.MaxValue;

        foreach (RaycastHit hit in hits)
        {
            Collider col = hit.collider;
            if (col.transform.IsChildOf(inventory.transform)) continue;

            IInteractable target = col.GetComponentInParent<IInteractable>();
            if (target == null) continue;

            // не сквозь стены
            if (!IsVisible(origin, col.bounds.ClosestPoint(origin), target)) continue;

            // выбираем тот, что ближе к центру экрана
            float angle = Vector3.Angle(dir, col.bounds.center - origin);
            if (debugLog) Debug.Log("Кандидат: " + col.name + " угол " + angle.ToString("0"));

            if (angle < bestAngle) { bestAngle = angle; best = target; }
        }
        return best;
    }

    bool IsVisible(Vector3 from, Vector3 to, IInteractable target)
    {
        Vector3 v = to - from;
        float d = v.magnitude;
        if (d < 0.01f) return true;

        RaycastHit[] hs = Physics.RaycastAll(from, v / d, d + 0.05f, ~0, QueryTriggerInteraction.Ignore);
        Array.Sort(hs, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit h in hs)
        {
            if (h.collider.transform.IsChildOf(inventory.transform)) continue;
            return h.collider.GetComponentInParent<IInteractable>() == target;
        }
        return true;
    }
}