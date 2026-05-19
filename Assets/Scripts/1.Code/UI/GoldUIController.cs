using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GoldUIController : MonoBehaviour
{
    [Header("References")]
    public GoldManager goldManager;
    public PlayerProgressManager playerProgressManager;
    public TMP_Text goldText;
    public Text legacyGoldText;
    public Image goldIcon;
    public Sprite goldSprite;

    [Header("Display")]
    public string prefix = "골드 ";
    public bool hidePrefixWhenIconIsAssigned = true;
    public bool showMainGoldWhenGoldManagerIsMissing = true;
    public bool createIconWhenSpriteIsAssigned = true;
    public Vector2 iconSize = new(32f, 32f);
    public float iconSpacing = 6f;

    private int lastGold = int.MinValue;

    private void Awake()
    {
        if (goldManager == null)
            goldManager = FindAnyObjectByType<GoldManager>();

        if (playerProgressManager == null)
            playerProgressManager = PlayerProgressManager.Instance;

        RefreshText(force: true);
    }

    private void Update()
    {
        RefreshText();
    }

    private void RefreshText(bool force = false)
    {
        if (goldManager == null && playerProgressManager == null)
            playerProgressManager = PlayerProgressManager.Instance;

        if (!TryGetDisplayGold(out int currentGold))
            return;

        if (!force && currentGold == lastGold)
            return;

        lastGold = currentGold;
        goldIcon = EnsureIconImage(goldIcon, goldText, legacyGoldText, "Gold Icon");
        SetIconSprite(goldIcon, goldSprite);
        bool useIcon = ShouldUseIcon(goldIcon);
        string message = useIcon && hidePrefixWhenIconIsAssigned ? currentGold.ToString() : prefix + currentGold;

        SetIconVisible(goldIcon, useIcon);

        if (goldText != null)
            goldText.text = message;

        if (legacyGoldText != null)
            legacyGoldText.text = message;
    }

    private bool TryGetDisplayGold(out int currentGold)
    {
        if (goldManager != null)
        {
            currentGold = goldManager.currentGold;
            return true;
        }

        if (showMainGoldWhenGoldManagerIsMissing)
        {
            if (playerProgressManager != null)
            {
                currentGold = playerProgressManager.playerProgressData.mainGold;
                return true;
            }

            currentGold = PlayerProgressSaveSystem.Data.mainGold;
            return true;
        }

        currentGold = 0;
        return false;
    }

    private static bool ShouldUseIcon(Image icon)
    {
        return icon != null && icon.sprite != null;
    }

    private static void SetIconSprite(Image icon, Sprite sprite)
    {
        if (icon != null && sprite != null)
            icon.sprite = sprite;
    }

    private static void SetIconVisible(Image icon, bool visible)
    {
        if (icon != null)
            icon.gameObject.SetActive(visible);
    }

    private Image EnsureIconImage(Image icon, TMP_Text tmpText, Text legacyText, string iconObjectName)
    {
        if (icon != null || goldSprite == null || !createIconWhenSpriteIsAssigned)
            return icon;

        Transform textTransform = tmpText != null ? tmpText.transform : legacyText != null ? legacyText.transform : null;
        if (textTransform == null)
            return null;

        GameObject iconObject = new GameObject(iconObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.SetParent(textTransform, false);
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(1f, 0.5f);
        iconRect.sizeDelta = iconSize;
        iconRect.anchoredPosition = new Vector2(-iconSpacing, 0f);

        Image createdIcon = iconObject.GetComponent<Image>();
        createdIcon.raycastTarget = false;
        return createdIcon;
    }
}
