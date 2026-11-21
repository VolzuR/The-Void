using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class PhoneKit
{
    public static Font DefaultFont;

    public static Font LoadDefaultFont()
    {
        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f == null) f = Font.CreateDynamicFontFromOSFont("Arial", 16);
        return f;
    }

    public static RectTransform MakeRect(string name, Transform parent)
    {
        GameObject g = new GameObject(name, typeof(RectTransform));
        g.transform.SetParent(parent, false);
        return g.GetComponent<RectTransform>();
    }

    public static void Stretch(RectTransform r, float left = 0f, float top = 0f, float right = 0f, float bottom = 0f)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.offsetMin = new Vector2(left, bottom);
        r.offsetMax = new Vector2(-right, -top);
    }

    public static Image MakePanel(Transform parent, string name, Color color)
    {
        RectTransform r = MakeRect(name, parent);
        Image img = r.gameObject.AddComponent<Image>();
        img.color = color;
        return img;
    }

    // текст сразу растянут на весь родитель
    public static Text MakeLabel(Transform parent, string text, int size, Color color, TextAnchor align)
    {
        RectTransform r = MakeRect("Text", parent);
        Stretch(r);
        Text t = r.gameObject.AddComponent<Text>();
        t.font = DefaultFont;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    public static Button MakeButton(Transform parent, string text, int size, Color bg, Color fg, UnityAction onClick)
    {
        Image img = MakePanel(parent, "Button_" + text, bg);
        Button b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        if (onClick != null) b.onClick.AddListener(onClick);
        MakeLabel(img.transform, text, size, fg, TextAnchor.MiddleCenter);
        return b;
    }

    // шапка приложения; возвращает область под шапкой
    public static RectTransform MakeAppFrame(RectTransform root, string title, Color accent)
    {
        Image header = MakePanel(root, "Header", accent);
        RectTransform hr = header.rectTransform;
        hr.anchorMin = new Vector2(0f, 1f);
        hr.anchorMax = new Vector2(1f, 1f);
        hr.pivot = new Vector2(0.5f, 1f);
        hr.sizeDelta = new Vector2(0f, 48f);
        hr.anchoredPosition = Vector2.zero;
        MakeLabel(hr, title, 22, Color.white, TextAnchor.MiddleCenter);

        RectTransform body = MakeRect("Body", root);
        Stretch(body, 0f, 48f, 0f, 0f);
        return body;
    }
}