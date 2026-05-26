using UnityEngine;
using UnityEngine.UI;

public class BattleLifeManager : MonoBehaviour
{
    [Header("Life")]
    public int maxLife = 10;
    public int currentLife = 10;

    [Header("Sprites")]
    public Sprite fullHeartSprite;
    public Sprite emptyHeartSprite;

    [Header("UI")]
    public Image[] heartImages;

    private void Awake()
    {
        maxLife = Mathf.Max(1, maxLife);
        currentLife = Mathf.Clamp(currentLife, 0, maxLife);

        RefreshUI();
    }

    public void ResetLife()
    {
        maxLife = Mathf.Max(1, maxLife);
        currentLife = maxLife;
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

    private Sprite GetBrokenHeartSprite()
    {
        return emptyHeartSprite;
    }
}
