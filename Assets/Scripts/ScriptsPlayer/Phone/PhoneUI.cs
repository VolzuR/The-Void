using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum PhoneAppType { Company, Calculator, Map }

[System.Serializable]
public class PhoneAppEntry
{
    public string appName = "Приложение";
    public PhoneAppType type = PhoneAppType.Company;
    public Sprite icon;                      // необязательно
    public Color color = new Color(0.25f, 0.5f, 0.9f, 1f);
    public GameObject screenPrefab;          // необязательно: свой экран вместо сгенерированного
    public bool landscape = false;           // для своих экранов: показывать телефон горизонтально
}

public class PhoneUI : MonoBehaviour
{
    [Header("Ссылки")]
    public PlayerInventory inventory;
    public Transform player;
    public KeyCode openKey = KeyCode.Tab;
    public bool pauseGameWhileOpen = false;
    public int sortingOrder = 1000;         

    [Header("Внешний вид")]
    public Font font;
    public Sprite wallpaper;
    public Sprite phoneFrame;
    public Color frameColor = new Color(0.05f, 0.05f, 0.06f, 1f);
    public Color screenColor = new Color(0.1f, 0.12f, 0.18f, 1f);
    public Vector2 phoneSize = new Vector2(400f, 780f);
    public Vector2 landscapeSize = new Vector2(900f, 460f);
    public float rotateSpeed = 2400f;        // скорость "поворота" в пикселях в секунду
    public float bezel = 14f;

    [Header("Приложения")]
    public string companyName = "Почтовая служба";
    public List<PhoneAppEntry> apps = new List<PhoneAppEntry>();

    [Header("Карта")]
    public Sprite mapImage;
    public bool autoMapBounds = true;
    public Vector2 mapCenter = Vector2.zero;
    public float mapSize = 200f;
    public bool mapShowLabels = true;

    GameObject canvasRoot;
    RectTransform frameRect, homeView, appHost, appRoot;
    Text timeText, dayText;
    string lastTime = "", lastDay = "";
    PhoneAppView currentView;
    bool open;
    bool landscape;

    void Reset() { FillDefaults(); }

    void FillDefaults()
    {
        apps = new List<PhoneAppEntry>
        {
            new PhoneAppEntry { appName = "Работа", type = PhoneAppType.Company, color = new Color(0.2f, 0.6f, 0.35f, 1f) },
            new PhoneAppEntry { appName = "Калькулятор", type = PhoneAppType.Calculator, color = new Color(0.9f, 0.5f, 0.15f, 1f) },
            new PhoneAppEntry { appName = "Карта", type = PhoneAppType.Map, color = new Color(0.25f, 0.5f, 0.9f, 1f), landscape = true }
        };
    }

    void Awake()
    {
        if (!inventory) inventory = GetComponent<PlayerInventory>();
        if (!player) player = transform;
        if (apps == null || apps.Count == 0) FillDefaults();
        BuildUI();
        canvasRoot.SetActive(false);
    }

    void BuildUI()
    {
        PhoneKit.DefaultFont = font ? font : PhoneKit.LoadDefaultFont();

        if (FindAnyObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        canvasRoot = new GameObject("PhoneCanvas");
        Canvas canvas = canvasRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        canvas.pixelPerfect = true;                        // чёткий текст
        CanvasScaler scaler = canvasRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;                    // масштаб по высоте экрана
        canvasRoot.AddComponent<GraphicRaycaster>();

        Image dim = PhoneKit.MakePanel(canvasRoot.transform, "Dim", new Color(0f, 0f, 0f, 0.45f));
        PhoneKit.Stretch(dim.rectTransform);

        Image frame = PhoneKit.MakePanel(canvasRoot.transform, "Phone", frameColor);
        if (phoneFrame) { frame.sprite = phoneFrame; frame.color = Color.white; }
        frameRect = frame.rectTransform;
        frameRect.anchorMin = frameRect.anchorMax = frameRect.pivot = new Vector2(0.5f, 0.5f);
        frameRect.sizeDelta = phoneSize;
        frameRect.anchoredPosition = Vector2.zero;

        Image screen = PhoneKit.MakePanel(frame.transform, "Screen", screenColor);
        PhoneKit.Stretch(screen.rectTransform, bezel, bezel, bezel, bezel);
        if (wallpaper) { screen.sprite = wallpaper; screen.color = Color.white; }

        // строка состояния
        Image sb = PhoneKit.MakePanel(screen.transform, "StatusBar", new Color(0f, 0f, 0f, 0.35f));
        RectTransform sbr = sb.rectTransform;
        sbr.anchorMin = new Vector2(0f, 1f);
        sbr.anchorMax = new Vector2(1f, 1f);
        sbr.pivot = new Vector2(0.5f, 1f);
        sbr.sizeDelta = new Vector2(0f, 44f);
        sbr.anchoredPosition = Vector2.zero;
        timeText = PhoneKit.MakeLabel(sbr, "--:--", 22, Color.white, TextAnchor.MiddleLeft);
        PhoneKit.Stretch(timeText.rectTransform, 16f, 0f, 16f, 0f);
        dayText = PhoneKit.MakeLabel(sbr, "", 20, new Color(0.85f, 0.85f, 0.85f), TextAnchor.MiddleRight);
        PhoneKit.Stretch(dayText.rectTransform, 16f, 0f, 16f, 0f);

        // нижняя панель
        Image nav = PhoneKit.MakePanel(screen.transform, "NavBar", new Color(0f, 0f, 0f, 0.35f));
        RectTransform nr = nav.rectTransform;
        nr.anchorMin = new Vector2(0f, 0f);
        nr.anchorMax = new Vector2(1f, 0f);
        nr.pivot = new Vector2(0.5f, 0f);
        nr.sizeDelta = new Vector2(0f, 60f);
        nr.anchoredPosition = Vector2.zero;
        Button home = PhoneKit.MakeButton(nr, "ДОМОЙ", 20, new Color(1f, 1f, 1f, 0.15f), Color.white, GoHome);
        RectTransform hr = home.GetComponent<RectTransform>();
        hr.anchorMin = hr.anchorMax = hr.pivot = new Vector2(0.5f, 0.5f);
        hr.sizeDelta = new Vector2(160f, 42f);
        hr.anchoredPosition = Vector2.zero;

        RectTransform content = PhoneKit.MakeRect("Content", screen.transform);
        PhoneKit.Stretch(content, 0f, 44f, 0f, 60f);

        // главный экран
        homeView = PhoneKit.MakeRect("Home", content);
        PhoneKit.Stretch(homeView);
        GridLayoutGroup grid = homeView.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(100f, 112f);
        grid.spacing = new Vector2(14f, 14f);
        grid.padding = new RectOffset(22, 22, 30, 0);
        grid.childAlignment = TextAnchor.UpperLeft;

        foreach (PhoneAppEntry e in apps)
        {
            PhoneAppEntry entry = e;
            RectTransform cell = PhoneKit.MakeRect("App_" + entry.appName, homeView);

            Image tile = PhoneKit.MakePanel(cell, "Tile", entry.color);
            RectTransform tr = tile.rectTransform;
            tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 1f);
            tr.pivot = new Vector2(0.5f, 1f);
            tr.sizeDelta = new Vector2(76f, 76f);
            tr.anchoredPosition = Vector2.zero;

            if (entry.icon)
            {
                tile.sprite = entry.icon;
                tile.color = Color.white;
                tile.preserveAspect = true;
            }
            else
            {
                string letter = string.IsNullOrEmpty(entry.appName) ? "?" : entry.appName.Substring(0, 1).ToUpper();
                PhoneKit.MakeLabel(tile.transform, letter, 40, Color.white, TextAnchor.MiddleCenter);
            }

            Button btn = tile.gameObject.AddComponent<Button>();
            btn.targetGraphic = tile;
            btn.onClick.AddListener(() => OpenApp(entry));

            Text lab = PhoneKit.MakeLabel(cell, entry.appName, 16, Color.white, TextAnchor.UpperCenter);
            RectTransform lr = lab.rectTransform;
            lr.anchorMin = new Vector2(0f, 0f);
            lr.anchorMax = new Vector2(1f, 0f);
            lr.pivot = new Vector2(0.5f, 0f);
            lr.sizeDelta = new Vector2(0f, 30f);
            lr.anchoredPosition = Vector2.zero;
            lab.gameObject.AddComponent<Shadow>();
        }

        appHost = PhoneKit.MakeRect("AppHost", content);
        PhoneKit.Stretch(appHost);
        appHost.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!open)
        {
            if (Input.GetKeyDown(openKey) && !PlayerController.inputLocked) Open();
            return;
        }

        if (DayManager.Instance != null && DayManager.Instance.IsSleeping)
        {
            Close(false);
            return;
        }

        if (Input.GetKeyDown(openKey)) { Close(true); return; }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentView != null || appRoot != null) GoHome();
            else Close(true);
            return;
        }

        UpdateStatus();

        // плавный "поворот" телефона
        Vector2 target = landscape ? landscapeSize : phoneSize;
        frameRect.sizeDelta = Vector2.MoveTowards(frameRect.sizeDelta, target, rotateSpeed * Time.unscaledDeltaTime);

        if (currentView != null) currentView.Tick();
    }

    void Open()
    {
        open = true;
        landscape = false;
        frameRect.sizeDelta = phoneSize;
        PlayerController.inputLocked = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (pauseGameWhileOpen) Time.timeScale = 0f;
        canvasRoot.SetActive(true);
        GoHome();
        UpdateStatus();
    }

    void Close(bool releaseInput)
    {
        open = false;
        canvasRoot.SetActive(false);
        CloseApp();
        landscape = false;
        frameRect.sizeDelta = phoneSize;
        if (pauseGameWhileOpen) Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (releaseInput) PlayerController.inputLocked = false;
    }

    void UpdateStatus()
    {
        DayManager dm = DayManager.Instance;
        string t = dm ? dm.TimeString() : "--:--";
        string d = dm ? "День " + dm.day : "";
        if (t != lastTime) { lastTime = t; timeText.text = t; }
        if (d != lastDay) { lastDay = d; dayText.text = d; }
    }

    public void GoHome()
    {
        CloseApp();
    }

    void CloseApp()
    {
        currentView = null;
        landscape = false;
        if (appRoot) { Destroy(appRoot.gameObject); appRoot = null; }
        appHost.gameObject.SetActive(false);
        homeView.gameObject.SetActive(true);
    }

    void OpenApp(PhoneAppEntry e)
    {
        CloseApp();
        homeView.gameObject.SetActive(false);
        appHost.gameObject.SetActive(true);

        appRoot = PhoneKit.MakeRect("App", appHost);
        PhoneKit.Stretch(appRoot);

        if (e.screenPrefab)
        {
            GameObject g = Instantiate(e.screenPrefab, appRoot);
            RectTransform gr = g.GetComponent<RectTransform>();
            if (gr) PhoneKit.Stretch(gr);
            landscape = e.landscape;
            return;
        }

        Image bg = PhoneKit.MakePanel(appRoot, "Bg", new Color(0.08f, 0.09f, 0.12f, 0.96f));
        PhoneKit.Stretch(bg.rectTransform);

        switch (e.type)
        {
            case PhoneAppType.Company: currentView = new CompanyAppView(); break;
            case PhoneAppType.Calculator: currentView = new CalculatorAppView(); break;
            case PhoneAppType.Map: currentView = new MapAppView(); break;
        }
        if (currentView != null) currentView.Build(appRoot, this, e);

        landscape = e.landscape || (currentView != null && currentView.Landscape);
    }

    void OnDestroy()
    {
        if (open)
        {
            PlayerController.inputLocked = false;
            if (pauseGameWhileOpen) Time.timeScale = 1f;
        }
        if (canvasRoot) Destroy(canvasRoot);
    }
}