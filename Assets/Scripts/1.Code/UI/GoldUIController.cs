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

    [Header("Display")]
    public string prefix = "Gold: ";
    public bool showMainGoldWhenGoldManagerIsMissing = true;

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
        string message = prefix + currentGold;

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
}
