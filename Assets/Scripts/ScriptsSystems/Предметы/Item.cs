using UnityEngine;

public class Item : MonoBehaviour, IInteractable
{
    public string itemName = "Предмет";
    [TextArea(2, 5)] public string description = "";
    public float weight = 1f;            // кг

    Rigidbody rb;

    Rigidbody Body
    {
        get
        {
            if (!rb)
            {
                rb = GetComponent<Rigidbody>();
                if (!rb) rb = gameObject.AddComponent<Rigidbody>();
            }
            return rb;
        }
    }

    // что показывать игроку
    public virtual string Title => itemName;
    public virtual string Details => description;
    public virtual string Prompt => "[E] Взять: " + Title + " (" + weight.ToString("0.#") + " кг)";

    void Awake()
    {
        Body.mass = Mathf.Max(0.1f, weight);
        Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    public void SetWeight(float w)
    {
        weight = w;
        Body.mass = Mathf.Max(0.1f, w);
    }

    public void Interact(PlayerInventory player)
    {
        player.TryAdd(this);
    }

    // в инвентарь: гасим физику и выключаем объект
    public void Stow(Transform parent)
    {
        Body.linearVelocity = Vector3.zero;
        Body.angularVelocity = Vector3.zero;
        transform.SetParent(parent, false);
        gameObject.SetActive(false);
    }

    // выбросить в мир с физикой
    public void Release(Vector3 position, Quaternion rotation, Vector3 impulse)
    {
        transform.SetParent(null, true);
        transform.SetPositionAndRotation(position, rotation);
        gameObject.SetActive(true);

        Body.linearVelocity = Vector3.zero;
        Body.angularVelocity = Vector3.zero;
        Body.AddForce(impulse, ForceMode.VelocityChange);
        Body.AddTorque(Random.insideUnitSphere * 3f, ForceMode.VelocityChange);
    }

    // копия только для вида (рука, превью инвентаря)
    public GameObject CreateVisualCopy(Transform parent)
    {
        GameObject g = Instantiate(gameObject, parent);
        g.SetActive(false);
        foreach (Item i in g.GetComponentsInChildren<Item>(true)) DestroyImmediate(i);
        foreach (Rigidbody r in g.GetComponentsInChildren<Rigidbody>(true)) DestroyImmediate(r);
        foreach (Collider c in g.GetComponentsInChildren<Collider>(true)) DestroyImmediate(c);
        return g;
    }
}