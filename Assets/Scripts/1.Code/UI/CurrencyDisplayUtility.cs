using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class CurrencyDisplayUtility
{
    public static string FormatAmount(string label, double amount, Image icon, bool hideLabelWhenIconIsAssigned, string numberFormat = "0")
    {
        string amountText = amount.ToString(numberFormat);
        return ShouldUseIcon(icon) && hideLabelWhenIconIsAssigned ? amountText : $"{label} {amountText}";
    }

    public static string FormatAmount(string label, int amount, Image icon, bool hideLabelWhenIconIsAssigned)
    {
        return ShouldUseIcon(icon) && hideLabelWhenIconIsAssigned ? amount.ToString() : $"{label} {amount}";
    }

    public static bool ShouldUseIcon(Image icon)
    {
        return icon != null && icon.sprite != null;
    }

    public static void SetIconSprite(Image icon, Sprite sprite)
    {
        if (icon != null && sprite != null)
            icon.sprite = sprite;
    }

    public static void SetIconVisible(Image icon, bool visible)
    {
        if (icon != null)
            icon.gameObject.SetActive(visible);
    }

    public static Image EnsureIconImage(
        Image icon,
        TMP_Text targetText,
        string iconObjectName,
        Sprite sprite,
        bool createWhenSpriteIsAssigned,
        Vector2 iconSize,
        float iconSpacing)
    {
        return EnsureIconImage(
            icon,
            targetText,
            iconObjectName,
            sprite,
            createWhenSpriteIsAssigned,
            iconSize,
            new Vector2(1f, 0.5f),
            new Vector2(-iconSpacing, 0f));
    }

    public static Image EnsureIconImage(
        Image icon,
        TMP_Text targetText,
        string iconObjectName,
        Sprite sprite,
        bool createWhenSpriteIsAssigned,
        Vector2 iconSize,
        Vector2 pivot,
        Vector2 anchoredPosition)
    {
        if (icon != null || sprite == null || !createWhenSpriteIsAssigned || targetText == null)
            return icon;

        GameObject iconObject = new GameObject(iconObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.SetParent(targetText.transform, false);
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = pivot;
        iconRect.sizeDelta = iconSize;
        iconRect.anchoredPosition = anchoredPosition;

        Image createdIcon = iconObject.GetComponent<Image>();
        createdIcon.raycastTarget = false;
        return createdIcon;
    }
}
