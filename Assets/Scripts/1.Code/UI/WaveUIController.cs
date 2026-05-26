using TMPro;
using UnityEngine;

public class WaveUIController : MonoBehaviour
{
    [Header("References")]
    public WaveManager waveManager;
    public TMP_Text waveText;

    [Header("Display")]
    public string prefix = "Wave: ";

    private int lastWave = int.MinValue;

    private void Awake()
    {
        if (waveManager == null)
            waveManager = FindAnyObjectByType<WaveManager>();

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
    }
}
