using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    public PlayerInventory inventory;
    public KeyCode openKey = KeyCode.I;
    public float rotateSpeed = 40f;      // градусов в секунду
    [Header("Arrows")]
    public Texture2D leftArrowTexture;
    public Texture2D rightArrowTexture;
    public float arrowSize = 90f;
    public Color arrowColor = Color.white;
    public Color arrowHoverColor = new Color(0.95f, 0.8f, 0.2f, 1f);
    public bool showHeldHint = true;     // подсказка "в руках" в углу

    bool open;
    GUIStyle label, big, arrow;

    // 3D-превью
    Transform stage, pivot;
    Camera previewCam;
    RenderTexture rt;
    GameObject previewObj;
    Package shownPackage;

    void Awake()
    {
        if (!inventory) inventory = GetComponent<PlayerInventory>();
        SetupPreview();
    }

    void SetupPreview()
    {
        // сцена для превью строится далеко от игрового мира
        GameObject root = new GameObject("InventoryPreviewStage");
        root.transform.position = new Vector3(0, -500, 0);
        stage = root.transform;

        GameObject pivotGo = new GameObject("Pivot");
        pivotGo.transform.SetParent(stage, false);
        pivot = pivotGo.transform;

        rt = new RenderTexture(512, 512, 16);

        GameObject camGo = new GameObject("PreviewCamera");
        camGo.transform.SetParent(stage, false);
        camGo.transform.localPosition = new Vector3(0, 0, -2.5f);
        previewCam = camGo.AddComponent<Camera>();
        previewCam.clearFlags = CameraClearFlags.SolidColor;
        previewCam.backgroundColor = new Color(0.04f, 0.04f, 0.04f, 1f);
        previewCam.nearClipPlane = 0.1f;
        previewCam.farClipPlane = 10f;
        previewCam.fieldOfView = 30f;
        previewCam.targetTexture = rt;
        previewCam.enabled = false;

        GameObject lightGo = new GameObject("PreviewLight");
        lightGo.transform.SetParent(stage, false);
        lightGo.transform.localPosition = new Vector3(1.5f, 1.5f, -2f);
        Light l = lightGo.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = 12f;
        l.intensity = 3f;
    }

    void Update()
    {
        if (!open)
        {
            if (Input.GetKeyDown(openKey)) Open();
            return;
        }

        if (Input.GetKeyDown(openKey) || Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }
        if (Input.GetKeyDown(KeyCode.RightArrow)) inventory.Select(1);
        if (Input.GetKeyDown(KeyCode.LeftArrow)) inventory.Select(-1);

        if (inventory.Held != shownPackage) ShowItem(inventory.Held);

        if (pivot) pivot.Rotate(0, rotateSpeed * Time.unscaledDeltaTime, 0, Space.World);
    }

    void Open()
    {
        open = true;
        PlayerController.inputLocked = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        previewCam.enabled = true;
        ShowItem(inventory.Held);
    }

    void Close()
    {
        open = false;
        PlayerController.inputLocked = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        previewCam.enabled = false;
        ShowItem(null);
    }

    void ShowItem(Package p)
    {
        if (previewObj) Destroy(previewObj);
        previewObj = null;
        shownPackage = p;
        if (p == null) return;

        pivot.localRotation = Quaternion.identity;

        previewObj = p.CreateVisualCopy(pivot);
        previewObj.SetActive(true);

        Transform t = previewObj.transform;
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;
        t.localScale = p.transform.lossyScale;

        Renderer[] rs = previewObj.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return;

        // подгоняем размер под экран превью
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        float size = Mathf.Max(b.size.x, b.size.y, b.size.z);
        if (size > 0.0001f) t.localScale *= 1.2f / size;

        // центрируем, чтобы вращалось вокруг середины
        b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        t.position += pivot.position - b.center;
    }

    void EnsureStyles()
    {
        if (label != null) return;

        label = new GUIStyle(GUI.skin.label);
        label.alignment = TextAnchor.MiddleCenter;
        label.fontSize = 18;
        label.normal.textColor = Color.white;
        label.wordWrap = true;

        big = new GUIStyle(label);
        big.fontSize = 28;
        big.fontStyle = FontStyle.Bold;

        arrow = new GUIStyle(label);
        arrow.fontSize = 64;
        arrow.fontStyle = FontStyle.Bold;
    }
    bool DrawArrow(Rect r, Texture2D tex, string fallback)
    {
        bool hover = r.Contains(Event.current.mousePosition);
        GUI.color = hover ? arrowHoverColor : arrowColor;
        if (tex) GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit, true);
        else GUI.Label(r, fallback, arrow);
        GUI.color = Color.white;
        return GUI.Button(r, GUIContent.none, GUIStyle.none);
    }
    void OnGUI()
    {
        if (!inventory) return;
        EnsureStyles();
        GUI.depth = -100;

        // короткие сообщения
        if (Time.time < inventory.MessageUntil && !open)
            GUI.Label(new Rect(Screen.width / 2 - 300, Screen.height / 2 + 50, 600, 30), inventory.Message, label);

        if (!open)
        {
            if (showHeldHint && inventory.Held != null)
                GUI.Label(new Rect(10, Screen.height - 40, 500, 30),
                    "В руках: посылка → " + inventory.Held.destinationId + "   [I] инвентарь", label);
            return;
        }

        // тёмный фон
        GUI.color = new Color(0, 0, 0, 0.9f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float cx = Screen.width / 2f;
        float cy = Screen.height / 2f;

        GUI.Label(new Rect(cx - 200, 30, 400, 40), "ИНВЕНТАРЬ", big);

        int n = inventory.packages.Count;
        Package held = inventory.Held;

        if (held == null)
        {
            GUI.Label(new Rect(cx - 200, cy - 20, 400, 40), "Пусто", big);
        }
        else
        {
            float s = Mathf.Min(Screen.height * 0.5f, 420f);
            GUI.DrawTexture(new Rect(cx - s / 2, cy - s / 2 - 20, s, s), rt, ScaleMode.ScaleToFit, false);

            // стрелки на экране
            float a = arrowSize;
            Rect leftR = new Rect(cx - s / 2 - a - 40, cy - a / 2 - 20, a, a);
            Rect rightR = new Rect(cx + s / 2 + 40, cy - a / 2 - 20, a, a);
            if (DrawArrow(leftR, leftArrowTexture, "<")) inventory.Select(-1);
            if (DrawArrow(rightR, rightArrowTexture, ">")) inventory.Select(1);
            if (GUI.Button(new Rect(cx - s / 2 - a - 40, cy - a / 2 - 20, a, a), "<", arrow)) inventory.Select(-1);
            if (GUI.Button(new Rect(cx + s / 2 + 40, cy - a / 2 - 20, a, a), ">", arrow)) inventory.Select(1);
            GUI.Label(new Rect(cx - 250, cy + s / 2 - 5, 500, 30), "Посылка → " + held.destinationId, big);
            GUI.Label(new Rect(cx - 250, cy + s / 2 + 30, 500, 30),
                "Награда: " + held.reward + "     (" + (inventory.selectedIndex + 1) + " / " + n + ")", label);
        }

        GUI.Label(new Rect(cx - 300, Screen.height - 50, 600, 30), "← →  выбрать      I / Esc  закрыть", label);
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        PlayerController.inputLocked = false;
        if (rt) rt.Release();
        if (stage) Destroy(stage.gameObject);
    }
}