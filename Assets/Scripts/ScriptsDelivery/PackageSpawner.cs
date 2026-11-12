using UnityEngine;

public class PackageSpawner : MonoBehaviour
{
    public Vector2 weightRange = new Vector2(1f, 5f);
    [Header("Что и где")]
    public Package[] prefabs;            // префабы коробок и пакетов
    public Transform[] spawnPoints;      // где "привозят" (если пусто, спавн вокруг этого объекта)
    public float spawnRadius = 1.5f;
    public float spawnHeight = 1.5f;

    [Header("Сколько в день")]
    public int baseCount = 3;
    public float growthPerDay = 1f;      // на сколько больше посылок с каждым днём
    public int randomSpread = 1;         // случайность +-
    public int minCount = 1;
    public int maxCount = 15;

    [Header("Награда")]
    public int baseReward = 20;
    public float rewardPerMeter = 0.5f;

    void OnEnable()  { DayManager.NewDay += OnNewDay; }
    void OnDisable() { DayManager.NewDay -= OnNewDay; }

    void OnNewDay(int day)
    {
        DeliveryPoint[] houses = FindObjectsByType<DeliveryPoint>();
        if (houses.Length == 0 || prefabs == null || prefabs.Length == 0)
        {
            Debug.LogWarning("PackageSpawner: нет домов (DeliveryPoint) или не назначены префабы");
            return;
        }

        int count = Mathf.RoundToInt(baseCount + (day - 1) * growthPerDay)
                    + Random.Range(-randomSpread, randomSpread + 1);
        count = Mathf.Clamp(count, minCount, maxCount);

        for (int i = 0; i < count; i++)
        {
            DeliveryPoint house = houses[Random.Range(0, houses.Length)];
            Transform sp = (spawnPoints != null && spawnPoints.Length > 0)
                ? spawnPoints[Random.Range(0, spawnPoints.Length)] : transform;

            Vector2 c = Random.insideUnitCircle * spawnRadius;
            Vector3 pos = sp.position + new Vector3(c.x, spawnHeight + (i % 5) * 0.5f, c.y);

            Package prefab = prefabs[Random.Range(0, prefabs.Length)];
            Package p = Instantiate(prefab, pos, Quaternion.Euler(0, Random.Range(0f, 360f), 0));

            p.destinationId = house.destinationId;
            p.SetWeight(Mathf.Round(Random.Range(weightRange.x, weightRange.y) * 2f) / 2f);
            float dist = Vector3.Distance(sp.position, house.transform.position);
            p.reward = Mathf.Max(5, Mathf.RoundToInt((baseReward + dist * rewardPerMeter) / 5f) * 5);
        }
    }
}