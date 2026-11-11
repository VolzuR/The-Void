using System;
using System.Collections;
using UnityEngine;

public class DayManager : MonoBehaviour
{
    public static DayManager Instance { get; private set; }
    public static event Action<int> NewDay;      // номер нового дня

    [Header("Ссылки")]
    public PlayerInventory inventory;
    public Transform player;
    public Transform wakePoint;                  // где просыпается игрок (у кровати)
    public Light sun;                            // Directional Light

    [Header("Время")]
    public int day = 1;
    public float dayLengthMinutes = 15f;         // реальных минут на весь день
    public float startHour = 7f;
    public float endHour = 27f;                  // 27 = 03:00 следующих суток, тут игрок вырубается
    public float timeOfDay = 7f;

    [Header("Свет")]
    public float maxSunIntensity = 1.2f;
    public Color dayAmbient = new Color(0.55f, 0.6f, 0.65f);
    public Color nightAmbient = new Color(0.06f, 0.07f, 0.12f);
    public float daySkyExposure = 1.3f;
    public float nightSkyExposure = 0.05f;

    [Header("Прочее")]
    public int penaltyPerMissedPackage = 0;      // штраф за недоставленную посылку
    public bool showClock = true;
    public float fadeTime = 1f;

    bool sleeping;
    float fade;
    string caption = "", subtitle = "";
    Material skyInstance;
    GUIStyle clockStyle, bigStyle, smallStyle;

    void Awake()
    {
        Instance = this;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;

        // копия скайбокса, чтобы не менять файл материала
        if (RenderSettings.skybox)
        {
            skyInstance = new Material(RenderSettings.skybox);
            RenderSettings.skybox = skyInstance;
        }
    }

    void Start()
    {
        if (!sun) sun = RenderSettings.sun;
        if (!sun)
        {
            foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) { sun = l; break; }
        }

        timeOfDay = startHour;
        UpdateLighting();
        NewDay?.Invoke(day);                     // первый день: привозим посылки
    }

    void Update()
    {
        if (!sleeping)
        {
            timeOfDay += (endHour - startHour) / (dayLengthMinutes * 60f) * Time.deltaTime;
            if (timeOfDay >= endHour) StartCoroutine(SleepRoutine(true));
        }
        UpdateLighting();
    }

    public void Sleep()
    {
        if (!sleeping) StartCoroutine(SleepRoutine(false));
    }

    IEnumerator SleepRoutine(bool forced)
    {
        sleeping = true;
        PlayerController.inputLocked = true;
        caption = forced ? "Ты вырубился от усталости..." : "Спокойной ночи...";
        subtitle = "";
        yield return Fade(1f);

        // убираем недоставленные посылки (и в мире, и в инвентаре)
        Package[] all = FindObjectsByType<Package>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int missed = all.Length;
        foreach (Package p in all) if (p) Destroy(p.gameObject);
        if (inventory) inventory.ClearAll();
        if (missed > 0 && penaltyPerMissedPackage > 0 && MoneyManager.Instance != null)
            MoneyManager.Instance.Add(-missed * penaltyPerMissedPackage);

        day++;
        timeOfDay = startHour;
        MovePlayerToBed();

        caption = "День " + day;
        subtitle = missed > 0 ? "Не доставлено вчера: " + missed : "";
        NewDay?.Invoke(day);

        yield return new WaitForSecondsRealtime(1.5f);
        yield return Fade(0f);

        caption = "";
        subtitle = "";
        PlayerController.inputLocked = false;
        sleeping = false;
    }

    IEnumerator Fade(float target)
    {
        while (!Mathf.Approximately(fade, target))
        {
            fade = Mathf.MoveTowards(fade, target, Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeTime));
            yield return null;
        }
    }

    void MovePlayerToBed()
    {
        if (!player || !wakePoint) return;
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc) cc.enabled = false;
        player.SetPositionAndRotation(wakePoint.position, wakePoint.rotation);
        if (cc) cc.enabled = true;
    }

    void UpdateLighting()
    {
        float h = timeOfDay % 24f;
        float elev = Mathf.Sin((h - 6f) / 24f * Mathf.PI * 2f);   // 1 в полдень, -1 в полночь
        float daylight = Mathf.Clamp01(elev * 2f + 0.15f);

        if (sun)
        {
            sun.transform.rotation = Quaternion.Euler((h - 6f) / 24f * 360f, 170f, 0f);
            sun.intensity = maxSunIntensity * Mathf.Clamp01(elev * 3f);
        }

        RenderSettings.ambientLight = Color.Lerp(nightAmbient, dayAmbient, daylight);

        if (skyInstance && skyInstance.HasProperty("_Exposure"))
            skyInstance.SetFloat("_Exposure", Mathf.Lerp(nightSkyExposure, daySkyExposure, daylight));
    }

    public string TimeString()
    {
        float h = timeOfDay % 24f;
        int hh = (int)h;
        int mm = (int)((h - hh) * 60f);
        return hh.ToString("00") + ":" + mm.ToString("00");
    }

    void OnGUI()
    {
        if (clockStyle == null)
        {
            clockStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight, fontSize = 16 };
            clockStyle.normal.textColor = Color.white;
            bigStyle = new GUIStyle(clockStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 36, fontStyle = FontStyle.Bold };
            smallStyle = new GUIStyle(clockStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 20 };
        }

        GUI.depth = -50;
        if (showClock)
            GUI.Label(new Rect(Screen.width - 260, 10, 250, 25), "День " + day + "   " + TimeString(), clockStyle);

        if (fade > 0.001f)
        {
            GUI.depth = -200;
            GUI.color = new Color(0, 0, 0, fade);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = new Color(1, 1, 1, fade);
            GUI.Label(new Rect(0, Screen.height / 2 - 40, Screen.width, 60), caption, bigStyle);
            GUI.Label(new Rect(0, Screen.height / 2 + 20, Screen.width, 40), subtitle, smallStyle);
            GUI.color = Color.white;
        }
    }
}