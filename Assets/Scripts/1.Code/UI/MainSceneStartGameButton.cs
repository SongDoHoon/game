using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainSceneStartGameButton : MonoBehaviour
{
    private const string DefaultGameSceneName = "GameScene";

    public UnitGrowthManager unitGrowthManager;
    public Button startButton;
    public string gameSceneName = DefaultGameSceneName;
    public float gameSceneAutoStartDelay = 5f;

    private void Awake()
    {
        if (unitGrowthManager == null)
            unitGrowthManager = UnitGrowthManager.Instance;

        if (startButton == null)
            startButton = GetComponent<Button>();

        if (startButton != null)
        {
            startButton.onClick.RemoveListener(StartGame);
            startButton.onClick.AddListener(StartGame);
        }
    }

    public void StartGame()
    {
        if (unitGrowthManager == null)
            unitGrowthManager = UnitGrowthManager.Instance;

        if (unitGrowthManager != null)
        {
            unitGrowthManager.SaveGrowthData();
            unitGrowthManager.PrepareGrowthDataForSceneTransfer();
        }

        WaveManager.RequestAutoStartOnNextScene(gameSceneAutoStartDelay);
        SceneManager.LoadScene(GetGameSceneName());
    }

    private string GetGameSceneName()
    {
        if (string.IsNullOrWhiteSpace(gameSceneName) || gameSceneName == "SampleScene")
            return DefaultGameSceneName;

        return gameSceneName;
    }
}
