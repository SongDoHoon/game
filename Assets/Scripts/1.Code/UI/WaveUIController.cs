using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WaveUIController : MonoBehaviour
{
    [Header("References")]
    public WaveManager waveManager;
    public TMP_Text waveText;
    public Text legacyWaveText;

    [Header("Display")]
    public string prefix = "Wave: ";

    private int lastWave = int.MinValue;

    private void Awake()
    {
        if (waveManager == null)
            waveManager = FindFirstObjectByType<WaveManager>();

        RefreshText(true);
    }

    private void Update()
    {
        RefreshText(false);
    }

    private void RefreshText(bool force)
    {
        if (waveManager == null)
            return;

        int currentWave = waveManager.currentWave;
        if (!force && currentWave == lastWave)
            return;

        lastWave = currentWave;
        string message = prefix + currentWave;

        if (waveText != null)
            waveText.text = message;

        if (legacyWaveText != null)
            legacyWaveText.text = message;
    }
}
