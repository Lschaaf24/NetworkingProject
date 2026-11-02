using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class UIKit
{
    public static readonly Color Bg = new Color(0.08f, 0.09f, 0.12f, 1f);
    public static readonly Color Panel = new Color(0.14f, 0.16f, 0.21f, 1f);
    public static readonly Color Muted = new Color(0.22f, 0.25f, 0.32f, 1f);
    public static readonly Color Accent = new Color(0.23f, 0.52f, 0.95f, 1f);
    public static readonly Color Danger = new Color(0.80f, 0.28f, 0.28f, 1f);

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    public static void Fill(RectTransform rt, float inset = 0f)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset); rt.offsetMax = new Vector2(-inset, -inset);
    }

    public static RectTransform MakeScreen(Transform parent, string name, bool background)
    {
        var rt = NewRect(name, parent);
        Fill(rt);
        if (background) rt.gameObject.AddComponent<Image>().color = Bg;
        return rt;
    }

    // A panel that stacks its children vertically and grows to fit them.
    public static RectTransform Card(Transform parent, float width, Vector2 anchor, Vector2 offset)
    {
        var rt = NewRect("Card", parent);
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = offset;
        rt.sizeDelta = new Vector2(width, 100);
        rt.gameObject.AddComponent<Image>().color = Panel;
        var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(24, 24, 24, 24);
        v.spacing = 12;
        v.childControlWidth = true; v.childControlHeight = true;
        v.childForceExpandWidth = true; v.childForceExpandHeight = false;
        rt.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return rt;
    }
    public static RectTransform Card(Transform parent, float width) =>
        Card(parent, width, new Vector2(0.5f, 0.5f), Vector2.zero);

    // A plain vertical stack with no background (used for the player list).
    public static RectTransform VList(Transform parent, float spacing = 8)
    {
        var rt = NewRect("List", parent);
        var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
        v.spacing = spacing;
        v.childControlWidth = true; v.childControlHeight = true;
        v.childForceExpandWidth = true; v.childForceExpandHeight = false;
        rt.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return rt;
    }

    // A horizontal strip with a background; put a label and a button in it.
    public static RectTransform Row(Transform parent, float height = 56)
    {
        var rt = NewRect("Row", parent);
        rt.gameObject.AddComponent<Image>().color = Muted;
        var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.padding = new RectOffset(14, 10, 6, 6);
        h.spacing = 10;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = true; h.childControlHeight = true;
        h.childForceExpandWidth = false; h.childForceExpandHeight = false;
        rt.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
        return rt;
    }

    public static TextMeshProUGUI Label(Transform parent, string text, int size = 22,
        TextAlignmentOptions align = TextAlignmentOptions.Left, float height = 32)
    {
        var rt = NewRect("Label", parent);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.alignment = align; t.color = Color.white;
        t.raycastTarget = false;   // labels shouldn't swallow clicks
        rt.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
        return t;
    }

    public static Button MakeButton(Transform parent, string text, Color color, Action onClick,
        float height = 48, float width = -1)
    {
        var rt = NewRect("Button_" + text, parent);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;

        var btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        var cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.88f, 0.88f, 0.88f);
        cb.pressedColor = new Color(0.68f, 0.68f, 0.68f);
        btn.colors = cb;
        btn.onClick.AddListener(() => onClick());

        var le = rt.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        if (width > 0) le.preferredWidth = width;

        var label = Label(rt, text, 22, TextAlignmentOptions.Center, height);
        Fill(label.rectTransform);
        return btn;
    }

    public static TMP_InputField Input(Transform parent, string placeholder, float height = 48)
    {
        var root = NewRect("Input", parent);
        root.gameObject.SetActive(false);   // build inactive so the component can be configured before it starts
        root.gameObject.AddComponent<Image>().color = new Color(0.07f, 0.08f, 0.10f);
        root.gameObject.AddComponent<LayoutElement>().preferredHeight = height;

        var viewport = NewRect("Viewport", root);
        Fill(viewport, 10);
        viewport.gameObject.AddComponent<RectMask2D>();

        var text = NewRect("Text", viewport);
        Fill(text);
        var tmp = text.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 22; tmp.color = Color.white; tmp.alignment = TextAlignmentOptions.Left;

        var ph = NewRect("Placeholder", viewport);
        Fill(ph);
        var phTmp = ph.gameObject.AddComponent<TextMeshProUGUI>();
        phTmp.text = placeholder; phTmp.fontSize = 22; phTmp.fontStyle = FontStyles.Italic;
        phTmp.color = new Color(1, 1, 1, 0.35f); phTmp.alignment = TextAlignmentOptions.Left;

        var field = root.gameObject.AddComponent<TMP_InputField>();
        field.textViewport = viewport;
        field.textComponent = tmp;
        field.placeholder = phTmp;
        root.gameObject.SetActive(true);
        return field;
    }

    // A scrollable vertical list. Returns the "content" transform to add rows to.
    public static RectTransform ScrollList(Transform parent, float height)
    {
        var root = NewRect("Scroll", parent);
        root.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.25f);
        root.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
        var scroll = root.gameObject.AddComponent<ScrollRect>();

        var viewport = NewRect("Viewport", root);
        Fill(viewport);
        viewport.gameObject.AddComponent<RectMask2D>();

        var content = NewRect("Content", viewport);
        content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1);
        content.offsetMin = content.offsetMax = Vector2.zero;
        var v = content.gameObject.AddComponent<VerticalLayoutGroup>();
        v.spacing = 6; v.padding = new RectOffset(6, 6, 6, 6);
        v.childControlWidth = true; v.childControlHeight = true;
        v.childForceExpandWidth = true; v.childForceExpandHeight = false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewport; scroll.content = content;
        scroll.horizontal = false; scroll.scrollSensitivity = 30;
        return content;
    }

    public static void Clear(Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
    }
}
