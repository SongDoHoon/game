using UnityEngine;
using UnityEngine.UI;

public class BattleLifeManager : MonoBehaviour
{
    [Header("Life")]
    public int maxLife = 10;
    public int currentLife = 10;

    [Header("Sprites")]
    public Sprite fullHeartSprite;
    public Sprite brokenHeartSprite;
    public Sprite emptyHeartSprite;

    [Header("UI")]
    public Transform heartPanel;
    public Image heartImagePrefab;
    public Image[] heartImages;
    public bool buildHeartsOnAwake = false;
    public bool rebuildIfCountDoesNotMatch = true;

    private void Awake()
    {
        maxLife = Mathf.Max(1, maxLife);
        currentLife = Mathf.Clamp(currentLife, 0, maxLife);

        if (buildHeartsOnAwake)
            BuildHeartImagesIfNeeded();

        RefreshUI();
    }

    public void ResetLife()
    {
        maxLife = Mathf.Max(1, maxLife);
        currentLife = maxLife;
        BuildHeartImagesIfNeeded();
        RefreshUI();
    }

    public bool LoseLife(int amount = 1)
    {
        int safeAmount = Mathf.Max(0, amount);
        if (safeAmount <= 0)
            return currentLife > 0;

        currentLife = Mathf.Max(0, currentLife - safeAmount);
        RefreshUI();
        return currentLife > 0;
    }

    public void RefreshUI()
    {
        if (heartImages == null)
            return;

        for (int i = 0; i < heartImages.Length; i++)
        {
            Image heartImage = heartImages[i];
            if (heartImage == null)
                continue;

            bool isFull = i < currentLife;
            Sprite targetSprite = isFull ? fullHeartSprite : GetBrokenHeartSprite();
            if (targetSprite != null)
                heartImage.sprite = targetSprite;

            heartImage.gameObject.SetActive(i < maxLife);
        }
    }

    private void BuildHeartImagesIfNeeded()
    {
        if (heartImages != null && heartImages.Length == maxLife && !HasMissingHeartImage())
            return;

        if (!rebuildIfCountDoesNotMatch && heartImages != null && heartImages.Length > 0)
            return;

        if (heartPanel == null || heartImagePrefab == null)
            return;

        heartImages = new Image[maxLife];

        for (int i = 0; i < maxLife; i++)
        {
            Image heartImage = Instantiate(heartImagePrefab, heartPanel);
            heartImage.gameObject.name = $"Life Heart {i + 1}";
            heartImages[i] = heartImage;
        }

        heartImagePrefab.gameObject.SetActive(false);
    }

    private bool HasMissingHeartImage()
    {
        if (heartImages == null)
            return true;

        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] == null)
                return true;
        }

        return false;
    }

    private Sprite GetBrokenHeartSprite()
    {
        return brokenHeartSprite != null ? brokenHeartSprite : emptyHeartSprite;
    }
}
