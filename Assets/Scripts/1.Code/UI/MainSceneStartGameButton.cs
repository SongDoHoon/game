using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainSceneStartGameButton : MonoBehaviour
{
    public UnitGrowthManager unitGrowthManager;
    public Button startButton;
    public string gameSceneName = "SampleScene";

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

        SceneManager.LoadScene(gameSceneName);
    }
}
