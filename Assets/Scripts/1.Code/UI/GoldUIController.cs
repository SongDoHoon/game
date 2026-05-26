using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GoldUIController : MonoBehaviour
{
    [Header("References")]
    public GoldManager goldManager;
    public TMP_Text goldText;
    public Sprite goldSprite;

    [Header("Display")]
    public string prefix = "골드 ";
    public bool hidePrefixWhenIconIsAssigned = true;
    public bool showMainGoldWhenGoldManagerIsMissing = true;
    public bool createIconWhenSpriteIsAssigned = true;
    public Vector2 iconSize = new(32f, 32f);
    public float iconSpacing = 6f;

    private int lastGold = int.MinValue;
    private PlayerProgressManager playerProgressManager;
    private Image goldIcon;

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
        goldIcon = CurrencyDisplayUtility.EnsureIconImage(goldIcon, goldText, "Gold Icon", goldSprite, createIconWhenSpriteIsAssigned, iconSize, iconSpacing);
        CurrencyDisplayUtility.SetIconSprite(goldIcon, goldSprite);
        bool useIcon = CurrencyDisplayUtility.ShouldUseIcon(goldIcon);
        string message = useIcon && hidePrefixWhenIconIsAssigned ? currentGold.ToString() : prefix + currentGold;

        CurrencyDisplayUtility.SetIconVisible(goldIcon, useIcon);

        if (goldText != null)
            goldText.text = message;

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

}
