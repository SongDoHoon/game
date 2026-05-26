using TMPro;
using UnityEngine;

public class BattleTimerUIController : MonoBehaviour
{
    [Header("References")]
    public WaveManager waveManager;
    public TMP_Text timerText;

    private int lastDisplayedSeconds = int.MinValue;

    private void Awake()
    {
        ResolveReferences();
        RefreshText(true);
    }

    private void Update()
    {
        RefreshText(false);
    }

    private void ResolveReferences()
    {
        if (waveManager == null)
            waveManager = FindAnyObjectByType<WaveManager>();
    }

    private void RefreshText(bool force)
    {
        ResolveReferences();

        int currentSeconds = waveManager != null ? waveManager.GetElapsedBattleSeconds() : 0;
        if (!force && currentSeconds == lastDisplayedSeconds)
            return;

        lastDisplayedSeconds = currentSeconds;
        string message = waveManager != null ? waveManager.GetFormattedElapsedBattleTime() : "00:00";

        if (timerText != null)
            timerText.text = message;
    }
}
