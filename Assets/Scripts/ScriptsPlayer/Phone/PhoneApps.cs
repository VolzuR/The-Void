using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public abstract class PhoneAppView
{
    protected PhoneUI phone;
    public abstract void Build(RectTransform root, PhoneUI phone, PhoneAppEntry entry);
    public virtual bool Landscape => false;
    public virtual void Tick() { }
}

// ---------------- Приложение фирмы ----------------
public class CompanyAppView : PhoneAppView
{
    Text money, list;
    float timer;
    StringBuilder sb = new StringBuilder();

    public override void Build(RectTransform root, PhoneUI phone, PhoneAppEntry entry)
    {
        this.phone = phone;
        RectTransform body = PhoneKit.MakeAppFrame(root, phone.companyName, entry.color);

        money = PhoneKit.MakeLabel(body, "", 26, Color.white, TextAnchor.MiddleLeft);
        RectTransform mr = money.rectTransform;
        mr.anchorMin = new Vector2(0f, 1f);
        mr.anchorMax = new Vector2(1f, 1f);
        mr.pivot = new Vector2(0.5f, 1f);
        mr.sizeDelta = new Vector2(-32f, 50f);
        mr.anchoredPosition = new Vector2(0f, -8f);

        list = PhoneKit.MakeLabel(body, "", 20, Color.white, TextAnchor.UpperLeft);
        PhoneKit.Stretch(list.rectTransform, 16f, 64f, 16f, 8f);
        list.lineSpacing = 1.2f;

        Refresh();
    }

    public override void Tick()
    {
        timer += Time.unscaledDeltaTime;
        if (timer >= 0.5f) { timer = 0f; Refresh(); }
    }

    void Refresh()
    {
        money.text = "Баланс: " + (MoneyManager.Instance ? MoneyManager.Instance.money : 0);

        PlayerInventory inv = phone.inventory;
        List<Package> all = new List<Package>(Object.FindObjectsByType<Package>(FindObjectsInactive.Include));
        all.Sort((a, b) =>
        {
            bool ia = inv != null && inv.items.Contains(a);
            bool ib = inv != null && inv.items.Contains(b);
            if (ia != ib) return ia ? -1 : 1;
            return string.Compare(a.destinationId, b.destinationId, System.StringComparison.Ordinal);
        });

        sb.Length = 0;
        sb.Append("Осталось доставить: ").Append(all.Count).Append("\n\n");
        sb.Append("<color=#9AA0B0>Адрес   ·   Награда   ·   Вес   ·   Где</color>\n");

        foreach (Package p in all)
        {
            bool mine = inv != null && inv.items.Contains(p);
            sb.Append(p.destinationId).Append("   ·   ")
              .Append(p.reward).Append("   ·   ")
              .Append(p.weight.ToString("0.#")).Append(" кг   ·   ")
              .Append(mine ? "<color=#7CFC7C>у тебя</color>" : "<color=#F2A050>ждёт</color>")
              .Append("\n");
        }

        if (all.Count == 0) sb.Append("\nВсе посылки доставлены. Можно идти спать.");
        list.text = sb.ToString();
    }
}

// ---------------- Калькулятор ----------------
public class CalculatorAppView : PhoneAppView
{
    static readonly System.Globalization.CultureInfo Inv = System.Globalization.CultureInfo.InvariantCulture;
    static readonly string[] Keys =
    {
        "C", "<", "%", "÷",
        "7", "8", "9", "×",
        "4", "5", "6", "-",
        "1", "2", "3", "+",
        "±", "0", ".", "="
    };

    Text exprText, display;
    double acc;
    string op;
    string entryStr = "0";
    bool fresh, error;

    public override void Build(RectTransform root, PhoneUI phone, PhoneAppEntry entry)
    {
        this.phone = phone;
        RectTransform body = PhoneKit.MakeAppFrame(root, "Калькулятор", entry.color);

        Image disp = PhoneKit.MakePanel(body, "Display", new Color(0f, 0f, 0f, 0.45f));
        RectTransform dr = disp.rectTransform;
        dr.anchorMin = new Vector2(0f, 1f);
        dr.anchorMax = new Vector2(1f, 1f);
        dr.pivot = new Vector2(0.5f, 1f);
        dr.sizeDelta = new Vector2(0f, 110f);
        dr.anchoredPosition = Vector2.zero;

        exprText = PhoneKit.MakeLabel(dr, "", 20, new Color(0.7f, 0.7f, 0.7f), TextAnchor.UpperRight);
        PhoneKit.Stretch(exprText.rectTransform, 12f, 8f, 12f, 60f);
        display = PhoneKit.MakeLabel(dr, "0", 44, Color.white, TextAnchor.MiddleRight);
        PhoneKit.Stretch(display.rectTransform, 12f, 40f, 12f, 4f);

        RectTransform grid = PhoneKit.MakeRect("Grid", body);
        PhoneKit.Stretch(grid, 0f, 118f, 0f, 0f);
        GridLayoutGroup g = grid.gameObject.AddComponent<GridLayoutGroup>();
        g.cellSize = new Vector2(81f, 80f);
        g.spacing = new Vector2(8f, 8f);
        g.padding = new RectOffset(12, 12, 8, 8);
        g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        g.constraintCount = 4;

        foreach (string k in Keys)
        {
            string key = k;
            bool isOp = key == "÷" || key == "×" || key == "-" || key == "+" || key == "=";
            bool isFn = key == "C" || key == "<" || key == "%" || key == "±";
            Color bg = isOp ? entry.color : (isFn ? new Color(0.35f, 0.35f, 0.4f) : new Color(0.2f, 0.2f, 0.24f));
            PhoneKit.MakeButton(grid, key, 30, bg, Color.white, () => Press(key));
        }
        Refresh();
    }

    void Press(string k)
    {
        if (error) Clear();

        switch (k)
        {
            case "C": Clear(); break;
            case "<": Backspace(); break;
            case "%": entryStr = Format(Parse(entryStr) / 100.0); fresh = false; break;
            case "±": Negate(); break;
            case "+": case "-": case "×": case "÷": Operator(k); break;
            case "=": EqualsPressed(); break;
            case ".": Dot(); break;
            default: Digit(k); break;
        }
        Refresh();
    }

    void Digit(string d)
    {
        if (fresh) { entryStr = d; fresh = false; }
        else if (entryStr == "0") entryStr = d;
        else if (entryStr == "-0") entryStr = "-" + d;
        else if (entryStr.Length < 14) entryStr += d;
    }

    void Dot()
    {
        if (fresh) { entryStr = "0."; fresh = false; }
        else if (!entryStr.Contains(".")) entryStr += ".";
    }

    void Operator(string o)
    {
        if (op != null && !fresh) Evaluate();
        else if (op == null) acc = Parse(entryStr);
        if (error) return;
        op = o;
        fresh = true;
    }

    void EqualsPressed()
    {
        if (op == null) return;
        Evaluate();
        if (error) return;
        op = null;
        fresh = true;
    }

    void Evaluate()
    {
        double b = Parse(entryStr);
        switch (op)
        {
            case "+": acc += b; break;
            case "-": acc -= b; break;
            case "×": acc *= b; break;
            case "÷":
                if (b == 0) { Fail(); return; }
                acc /= b;
                break;
        }
        entryStr = Format(acc);
    }

    void Fail() { error = true; op = null; acc = 0; entryStr = "Ошибка"; }

    void Negate()
    {
        if (entryStr == "0") return;
        entryStr = entryStr.StartsWith("-") ? entryStr.Substring(1) : "-" + entryStr;
    }

    void Backspace()
    {
        if (fresh) return;
        entryStr = entryStr.Length > 1 ? entryStr.Substring(0, entryStr.Length - 1) : "0";
        if (entryStr == "-") entryStr = "0";
    }

    void Clear()
    {
        acc = 0; op = null; entryStr = "0"; fresh = false; error = false;
    }

    void Refresh()
    {
        display.text = entryStr;
        exprText.text = op != null ? Format(acc) + " " + op : "";
    }

    static double Parse(string s)
    {
        double v;
        return double.TryParse(s, System.Globalization.NumberStyles.Float, Inv, out v) ? v : 0.0;
    }

    static string Format(double d)
    {
        if (double.IsNaN(d) || double.IsInfinity(d)) return "Ошибка";
        if (System.Math.Abs(d) >= 1e12) return d.ToString("0.###E+0", Inv);
        string s = System.Math.Round(d, 10).ToString("0.##########", Inv);
        return s == "-0" ? "0" : s;
    }
}

// ---------------- Карта ----------------
public class MapAppView : PhoneAppView
{
    class Marker
    {
        public RectTransform rect;
        public Image img;
        public Text label;
        public Vector3 world;
        public DeliveryPoint house;
        public float baseSize;
    }

    // метки игрока живут, пока запущена игра
    static readonly List<Vector3> Pins = new List<Vector3>();
    const int MaxPins = 20;
    const float MinZoom = 1f, MaxZoom = 8f;

    static readonly Color Grey = new Color(0.6f, 0.6f, 0.6f, 1f);
    static readonly Color Yellow = new Color(1f, 0.85f, 0.2f, 1f);
    static readonly Color Orange = new Color(0.95f, 0.5f, 0.2f, 1f);
    static readonly Color Cyan = new Color(0.2f, 0.9f, 1f, 1f);
    static readonly Color Purple = new Color(0.6f, 0.6f, 1f, 1f);
    static readonly Color Red = new Color(1f, 0.3f, 0.3f, 1f);

    RectTransform viewport, content, layerHouses, layerPins, layerPlayer;
    Text infoText;
    List<Marker> markers = new List<Marker>();
    List<Marker> houses = new List<Marker>();
    List<Marker> pinMarkers = new List<Marker>();
    Marker playerMarker, selected;
    float cx, cz, side = 100f;
    float zoom = 1f;
    float timer;

    public override bool Landscape => true;

    public override void Build(RectTransform root, PhoneUI phone, PhoneAppEntry entry)
    {
        this.phone = phone;
        if (EventSystem.current) EventSystem.current.pixelDragThreshold = 3;

        RectTransform body = PhoneKit.MakeAppFrame(root, entry.appName, entry.color);

        // слева: квадратная область карты
        RectTransform holder = PhoneKit.MakeRect("MapHolder", body);
        holder.anchorMin = new Vector2(0f, 0f);
        holder.anchorMax = new Vector2(0.5f, 1f);
        holder.offsetMin = new Vector2(10f, 10f);
        holder.offsetMax = new Vector2(-6f, -10f);

        Image vpImg = PhoneKit.MakePanel(holder, "Viewport", new Color(0.07f, 0.09f, 0.07f, 1f));
        viewport = vpImg.rectTransform;
        PhoneKit.Stretch(viewport);
        AspectRatioFitter fit = viewport.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = 1f;
        viewport.gameObject.AddComponent<RectMask2D>();

        PhoneMapInput input = viewport.gameObject.AddComponent<PhoneMapInput>();
        input.Dragged = OnDragged;
        input.Scrolled = OnScrolled;
        input.Clicked = OnClicked;

        // фон карты (двигается и масштабируется), сюда можно подставить картинку
        Image contentImg = PhoneKit.MakePanel(viewport, "Content", new Color(0.12f, 0.16f, 0.12f, 1f));
        content = contentImg.rectTransform;
        PhoneKit.Stretch(content);
        if (phone.mapImage) { contentImg.sprite = phone.mapImage; contentImg.color = Color.white; }

        // значки лежат отдельными слоями и не масштабируются, поэтому всегда чёткие
        layerHouses = MakeLayer("Houses");
        layerPins = MakeLayer("Pins");
        layerPlayer = MakeLayer("Player");

        DeliveryPoint[] hs = Object.FindObjectsByType<DeliveryPoint>();
        PackageSpawner[] posts = Object.FindObjectsByType<PackageSpawner>();
        ComputeBounds(hs, posts, phone.player);

        foreach (DeliveryPoint h in hs)
        {
            Marker m = AddMarker(layerHouses, "House_" + h.destinationId, h.transform.position, Grey, 14f, h.destinationId);
            m.house = h;
            houses.Add(m);
        }

        foreach (PackageSpawner s in posts)
            AddMarker(layerHouses, "PostOffice", s.transform.position, Purple, 18f, "Почта");

        foreach (Vector3 w in Pins) AddPinMarker(w);

        if (phone.player)
        {
            playerMarker = AddMarker(layerPlayer, "Player", phone.player.position, Cyan, 16f, null);
            Image bar = PhoneKit.MakePanel(playerMarker.rect, "Dir", Cyan);
            RectTransform br = bar.rectTransform;
            br.anchorMin = br.anchorMax = new Vector2(0.5f, 0.5f);
            br.pivot = new Vector2(0.5f, 0f);
            br.sizeDelta = new Vector2(4f, 14f);
            br.anchoredPosition = new Vector2(0f, 4f);
            bar.raycastTarget = false;
        }

        // справа: кнопки, информация, пояснения
        RectTransform right = PhoneKit.MakeRect("Right", body);
        right.anchorMin = new Vector2(0.52f, 0f);
        right.anchorMax = new Vector2(1f, 1f);
        right.offsetMin = new Vector2(4f, 10f);
        right.offsetMax = new Vector2(-12f, -10f);

        RectTransform row = PhoneKit.MakeRect("Buttons", right);
        row.anchorMin = new Vector2(0f, 1f);
        row.anchorMax = new Vector2(1f, 1f);
        row.pivot = new Vector2(0.5f, 1f);
        row.sizeDelta = new Vector2(0f, 44f);
        row.anchoredPosition = Vector2.zero;
        HorizontalLayoutGroup hl = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        hl.spacing = 8f;
        hl.childControlWidth = true;
        hl.childControlHeight = true;
        hl.childForceExpandWidth = true;
        hl.childForceExpandHeight = true;

        Color btn = new Color(1f, 1f, 1f, 0.15f);
        PhoneKit.MakeButton(row, "+", 26, btn, Color.white, () => ZoomAt(Vector2.zero, zoom * 1.5f, viewport.rect.width));
        PhoneKit.MakeButton(row, "-", 26, btn, Color.white, () => ZoomAt(Vector2.zero, zoom / 1.5f, viewport.rect.width));
        PhoneKit.MakeButton(row, "Я", 20, btn, Color.white, CenterOnPlayer);
        PhoneKit.MakeButton(row, "Метки ×", 17, btn, Color.white, ClearPins);

        infoText = PhoneKit.MakeLabel(right, "", 18, Color.white, TextAnchor.UpperLeft);
        RectTransform ir = infoText.rectTransform;
        ir.anchorMin = new Vector2(0f, 1f);
        ir.anchorMax = new Vector2(1f, 1f);
        ir.pivot = new Vector2(0.5f, 1f);
        ir.sizeDelta = new Vector2(0f, 110f);
        ir.anchoredPosition = new Vector2(0f, -54f);

        Text legend = PhoneKit.MakeLabel(right,
            "<color=#33E6FF>Голубой</color> — ты\n" +
            "<color=#FFD933>Жёлтый</color> — везёшь посылку для этого дома\n" +
            "<color=#F28033>Оранжевый</color> — посылка для дома ещё не у тебя\n" +
            "<color=#9999FF>Фиолетовый</color> — почта\n" +
            "<color=#FF5050>Красный ромб</color> — твоя метка\n\n" +
            "<color=#9AA0B0>ЛКМ — двигать карту или выбрать дом\n" +
            "Колесо — масштаб\n" +
            "ПКМ — поставить или убрать метку</color>",
            17, Color.white, TextAnchor.UpperLeft);
        RectTransform lr = legend.rectTransform;
        lr.offsetMin = new Vector2(0f, 0f);
        lr.offsetMax = new Vector2(0f, -172f);

        RefreshHouses();
    }

    RectTransform MakeLayer(string name)
    {
        RectTransform r = PhoneKit.MakeRect(name, viewport);
        PhoneKit.Stretch(r);
        return r;
    }

    void ComputeBounds(DeliveryPoint[] hs, PackageSpawner[] posts, Transform pl)
    {
        if (!phone.autoMapBounds)
        {
            cx = phone.mapCenter.x;
            cz = phone.mapCenter.y;
            side = Mathf.Max(10f, phone.mapSize);
            return;
        }

        float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
        System.Action<Vector3> add = p =>
        {
            minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
            minZ = Mathf.Min(minZ, p.z); maxZ = Mathf.Max(maxZ, p.z);
        };
        foreach (DeliveryPoint h in hs) add(h.transform.position);
        foreach (PackageSpawner s in posts) add(s.transform.position);
        if (pl) add(pl.position);

        if (minX == float.MaxValue) { cx = 0f; cz = 0f; side = 100f; return; }

        cx = (minX + maxX) * 0.5f;
        cz = (minZ + maxZ) * 0.5f;
        side = Mathf.Max(20f, Mathf.Max(maxX - minX, maxZ - minZ) * 1.2f);
    }

    // ---------- координаты ----------

    Vector2 WorldToView(Vector3 w, float S)
    {
        Vector2 n = new Vector2((w.x - cx) / side, (w.z - cz) / side);   // от центра карты, в долях стороны
        return content.anchoredPosition + n * S * zoom;
    }

    Vector3 ViewToWorld(Vector2 v, float S)
    {
        Vector2 n = (v - content.anchoredPosition) / (S * zoom);
        return new Vector3(n.x * side + cx, 0f, n.y * side + cz);
    }

    void ClampPan(float S)
    {
        float max = (zoom - 1f) * S * 0.5f;
        Vector2 p = content.anchoredPosition;
        p.x = Mathf.Clamp(p.x, -max, max);
        p.y = Mathf.Clamp(p.y, -max, max);
        content.anchoredPosition = p;
    }

    void ZoomAt(Vector2 viewPoint, float newZoom, float S)
    {
        if (S < 2f) return;
        newZoom = Mathf.Clamp(newZoom, MinZoom, MaxZoom);
        Vector2 cp = (viewPoint - content.anchoredPosition) / zoom;
        zoom = newZoom;
        content.localScale = new Vector3(zoom, zoom, 1f);
        content.anchoredPosition = viewPoint - cp * zoom;
        ClampPan(S);
    }

    void CenterOnPlayer()
    {
        float S = viewport.rect.width;
        if (!phone.player || S < 2f) return;
        if (zoom < 2.5f) ZoomAt(Vector2.zero, 2.5f, S);
        Vector2 n = new Vector2((phone.player.position.x - cx) / side, (phone.player.position.z - cz) / side);
        content.anchoredPosition = -n * S * zoom;
        ClampPan(S);
    }

    // ---------- значки ----------

    Marker AddMarker(RectTransform layer, string name, Vector3 world, Color color, float size, string label, float rotZ = 0f)
    {
        Marker m = new Marker();
        m.world = world;
        m.baseSize = size;
        m.img = PhoneKit.MakePanel(layer, name, color);
        m.img.raycastTarget = false;       // клики считаем сами по координатам
        m.rect = m.img.rectTransform;
        m.rect.anchorMin = m.rect.anchorMax = new Vector2(0.5f, 0.5f);
        m.rect.pivot = new Vector2(0.5f, 0.5f);
        m.rect.sizeDelta = new Vector2(size, size);
        m.rect.localRotation = Quaternion.Euler(0f, 0f, rotZ);

        if (!string.IsNullOrEmpty(label))
        {
            m.label = PhoneKit.MakeLabel(m.rect, label, 14, Color.white, TextAnchor.MiddleCenter);
            RectTransform lr = m.label.rectTransform;
            lr.anchorMin = lr.anchorMax = new Vector2(0.5f, 0.5f);
            lr.pivot = new Vector2(0.5f, 0.5f);
            lr.sizeDelta = new Vector2(110f, 18f);
            lr.anchoredPosition = new Vector2(0f, size * 0.5f + 11f);
            m.label.gameObject.AddComponent<Shadow>();
        }

        markers.Add(m);
        return m;
    }

    void AddPinMarker(Vector3 world)
    {
        pinMarkers.Add(AddMarker(layerPins, "Pin", world, Red, 14f, null, 45f));
    }

    void RemovePin(Marker pin)
    {
        int i = pinMarkers.IndexOf(pin);
        if (i < 0) return;
        Pins.RemoveAt(i);
        pinMarkers.RemoveAt(i);
        markers.Remove(pin);
        Object.Destroy(pin.rect.gameObject);
    }

    void ClearPins()
    {
        foreach (Marker m in pinMarkers)
        {
            markers.Remove(m);
            Object.Destroy(m.rect.gameObject);
        }
        pinMarkers.Clear();
        Pins.Clear();
    }

    Marker FindNearest(List<Marker> list, Vector2 viewPoint, float maxDist, float S)
    {
        Marker best = null;
        float bestD = maxDist;
        foreach (Marker m in list)
        {
            float d = (WorldToView(m.world, S) - viewPoint).magnitude;
            if (d < bestD) { bestD = d; best = m; }
        }
        return best;
    }

    // ---------- ввод ----------

    void OnDragged(PointerEventData e)
    {
        float S = viewport.rect.width;
        Vector2 now, prev;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, e.position, null, out now)) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, e.position - e.delta, null, out prev);
        content.anchoredPosition += now - prev;
        ClampPan(S);
    }

    void OnScrolled(PointerEventData e)
    {
        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, e.position, null, out local)) return;
        ZoomAt(local, zoom * Mathf.Pow(1.2f, e.scrollDelta.y), viewport.rect.width);
    }

    void OnClicked(PointerEventData e)
    {
        float S = viewport.rect.width;
        Vector2 local;
        if (S < 2f || !RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, e.position, null, out local)) return;

        if (e.button == PointerEventData.InputButton.Left)
        {
            selected = FindNearest(houses, local, 18f, S);
            RefreshHouses();
        }
        else if (e.button == PointerEventData.InputButton.Right)
        {
            Marker pin = FindNearest(pinMarkers, local, 16f, S);
            if (pin != null) RemovePin(pin);
            else if (Pins.Count < MaxPins)
            {
                Vector3 w = ViewToWorld(local, S);
                Pins.Add(w);
                AddPinMarker(w);
            }
        }
    }

    // ---------- обновление ----------

    public override void Tick()
    {
        float S = viewport.rect.width;
        if (S < 2f) return;

        if (phone.player && playerMarker != null)
        {
            playerMarker.world = phone.player.position;
            playerMarker.rect.localRotation = Quaternion.Euler(0f, 0f, -phone.player.eulerAngles.y);
        }

        bool showLabels = phone.mapShowLabels && zoom >= 1.6f;
        foreach (Marker m in markers)
        {
            m.rect.anchoredPosition = WorldToView(m.world, S);
            if (m.label != null) m.label.enabled = showLabels || m == selected;
        }

        timer += Time.unscaledDeltaTime;
        if (timer >= 0.5f) { timer = 0f; RefreshHouses(); }
    }

    // цвета домов и текст справа: жёлтый = везёшь, оранжевый = посылка ещё не у тебя
    void RefreshHouses()
    {
        HashSet<string> held = new HashSet<string>();
        HashSet<string> waiting = new HashSet<string>();
        PlayerInventory inv = phone.inventory;

        int hN = 0, wN = 0, hR = 0, wR = 0;
        string selId = selected != null ? selected.house.destinationId : null;

        foreach (Package p in Object.FindObjectsByType<Package>(FindObjectsInactive.Include))
        {
            bool mine = inv != null && inv.items.Contains(p);
            if (mine) held.Add(p.destinationId); else waiting.Add(p.destinationId);

            if (selId != null && p.destinationId == selId)
            {
                if (mine) { hN++; hR += p.reward; }
                else { wN++; wR += p.reward; }
            }
        }

        foreach (Marker m in houses)
        {
            if (m.house == null) continue;
            string id = m.house.destinationId;
            Color c = held.Contains(id) ? Yellow : (waiting.Contains(id) ? Orange : Grey);
            if (m == selected) c = Color.white;
            m.img.color = c;
            float s = m == selected ? m.baseSize + 6f : m.baseSize;
            m.rect.sizeDelta = new Vector2(s, s);
        }

        if (selected == null || selected.house == null)
        {
            infoText.text = "Выбери дом (ЛКМ), чтобы узнать, есть ли для него посылки.";
            return;
        }

        float dist = 0f;
        if (phone.player)
        {
            Vector3 a = phone.player.position, b = selected.world;
            dist = Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
        }

        infoText.text = "<b>" + selId + "</b>   " + Mathf.RoundToInt(dist) + " м\n" +
                        "У тебя: " + hN + (hN > 0 ? "  (награда " + hR + ")" : "") + "\n" +
                        "Ещё ждёт: " + wN + (wN > 0 ? "  (награда " + wR + ")" : "");
    }
}