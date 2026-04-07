using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GoldUIController : MonoBehaviour
{
    [Header("References")]
    public GoldManager goldManager;
    public TMP_Text goldText;
    public Text legacyGoldText;

    [Header("Display")]
    public string prefix = "Gold: ";

    private int lastGold = int.MinValue;

    private void Awake()
    {
        if (goldManager == null)
            goldManager = FindFirstObjectByType<GoldManager>();

        RefreshText(force: true);
    }

    private void Update()
    {
        RefreshText();
    }

    private void RefreshText(bool force = false)
    {
        if (goldManager == null)
            return;

        int currentGold = goldManager.currentGold;
        if (!force && currentGold == lastGold)
            return;

        lastGold = currentGold;
        string message = prefix + currentGold;

        if (goldText != null)
            goldText.text = message;

        if (legacyGoldText != null)
            legacyGoldText.text = message;
    }
}
