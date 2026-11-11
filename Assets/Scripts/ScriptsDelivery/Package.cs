using UnityEngine;

public class Package : MonoBehaviour, IInteractable
{
    public string destinationId = "House_A";
    public int reward = 50;
    public float mass = 2f;

    Rigidbody rb;

    Rigidbody Body
    {
        get
        {
            if (!rb)
            {
                rb = GetComponent<Rigidbody>();
                if (!rb) { rb = gameObject.AddComponent<Rigidbody>(); rb.mass = mass; }
            }
            return rb;
        }
    }

    public string Prompt => "[E] Взять посылку (для " + destinationId + ")";

    void Awake()
    {
        Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // чтобы не проваливалась в пол
    }

    public void Interact(PlayerInventory player)
    {
        player.AddPackage(this);
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

    // копия только для вида (рука, инвентарь): без логики, физики и коллайдеров
    public GameObject CreateVisualCopy(Transform parent)
    {
        GameObject g = Instantiate(gameObject, parent);
        g.SetActive(false);
        foreach (Package p in g.GetComponentsInChildren<Package>(true)) DestroyImmediate(p);
        foreach (Rigidbody r in g.GetComponentsInChildren<Rigidbody>(true)) DestroyImmediate(r);
        foreach (Collider c in g.GetComponentsInChildren<Collider>(true)) DestroyImmediate(c);
        return g;
    }
}