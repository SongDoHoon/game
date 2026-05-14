using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EvolutionMaterialSlotUI : MonoBehaviour
{
    private const string DefaultAcquisitionText = "\uD68D\uB4DD\uCC98 : \uBCF4\uC2A4 \uCC98\uCE58 \uD6C4 \uACBD\uB9E4";
    private const string OwnedBadgeLabel = "\uBCF4\uC720\uC911";
    private const string MissingUnitLabel = "\uBBF8\uC9C0\uC815";

    [Header("Images")]
    public Image backgroundImage;
    public Image itemImage;
    public Image lockImage;
    public Image[] recipeBaseUnitImages;
    public Image[] recipeItemImages;
    public Image[] recipeResultUnitImages;

    [Header("Texts")]
    public TMP_Text itemNameText;
    public TMP_Text ownedCountText;
    public TMP_Text acquisitionText;
    public TMP_Text[] recipeBaseUnitTexts;
    public TMP_Text[] recipeResultUnitTexts;
    public TMP_Text ownedBadgeText;

    [Header("State")]
    public CanvasGroup canvasGroup;
    [Range(0f, 1f)] public float lockedAlpha = 0.45f;

    public void Refresh(EvolutionMaterialDisplayData displayData, int ownedCount, IReadOnlyList<EvolutionRecipe> recipes)
    {
        if (displayData == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        bool isOwned = ownedCount > 0;

        RefreshImages(displayData);
        RefreshTexts(displayData, ownedCount, recipes);
        RefreshRecipeImages(displayData, recipes);
        RefreshOwnedState(isOwned);
    }

    private void RefreshImages(EvolutionMaterialDisplayData displayData)
    {
        if (backgroundImage != null)
            backgroundImage.sprite = displayData.backgroundSprite;

        if (itemImage != null)
        {
            itemImage.sprite = displayData.itemSprite;
            itemImage.enabled = displayData.itemSprite != null;
        }
    }

    private void RefreshTexts(EvolutionMaterialDisplayData displayData, int ownedCount, IReadOnlyList<EvolutionRecipe> recipes)
    {
        if (itemNameText != null)
            itemNameText.text = displayData.DisplayName;

        if (ownedCountText != null)
            ownedCountText.text = $"\uBCF4\uC720:{ownedCount}\uAC1C";

        if (acquisitionText != null)
            acquisitionText.text = string.IsNullOrEmpty(displayData.acquisitionText)
                ? DefaultAcquisitionText
                : displayData.acquisitionText;

        if (ownedBadgeText != null)
            ownedBadgeText.text = OwnedBadgeLabel;

        RefreshRecipeUnitTexts(recipes);
    }

    private void RefreshRecipeImages(EvolutionMaterialDisplayData displayData, IReadOnlyList<EvolutionRecipe> recipes)
    {
        RefreshRecipeImageArray(recipeBaseUnitImages, recipes, GetBaseUnitSprite);
        RefreshRecipeImageArray(recipeResultUnitImages, recipes, GetResultUnitSprite);

        if (recipeItemImages == null)
            return;

        for (int i = 0; i < recipeItemImages.Length; i++)
        {
            Image image = recipeItemImages[i];
            if (image == null)
                continue;

            bool hasRecipe = recipes != null && i < recipes.Count && recipes[i] != null;
            image.sprite = hasRecipe ? displayData.itemSprite : null;
            image.enabled = hasRecipe && displayData.itemSprite != null;
        }
    }

    private void RefreshRecipeUnitTexts(IReadOnlyList<EvolutionRecipe> recipes)
    {
        RefreshRecipeUnitTextArray(recipeBaseUnitTexts, recipes, GetBaseUnitName);
        RefreshRecipeUnitTextArray(recipeResultUnitTexts, recipes, GetResultUnitName);
    }

    private void RefreshRecipeUnitTextArray(TMP_Text[] texts, IReadOnlyList<EvolutionRecipe> recipes, System.Func<EvolutionRecipe, string> textGetter)
    {
        if (texts == null)
            return;

        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text == null)
                continue;

            bool hasRecipe = recipes != null && i < recipes.Count && recipes[i] != null;
            text.gameObject.SetActive(hasRecipe);
            text.text = hasRecipe ? textGetter(recipes[i]) : string.Empty;
        }
    }

    private void RefreshRecipeImageArray(Image[] images, IReadOnlyList<EvolutionRecipe> recipes, System.Func<EvolutionRecipe, Sprite> spriteGetter)
    {
        if (images == null)
            return;

        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image == null)
                continue;

            Sprite sprite = recipes != null && i < recipes.Count && recipes[i] != null
                ? spriteGetter(recipes[i])
                : null;

            image.sprite = sprite;
            image.enabled = sprite != null;
        }
    }

    private void RefreshOwnedState(bool isOwned)
    {
        if (lockImage != null)
            lockImage.gameObject.SetActive(!isOwned);

        if (ownedBadgeText != null)
            ownedBadgeText.gameObject.SetActive(isOwned);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = isOwned ? 1f : lockedAlpha;
            canvasGroup.interactable = isOwned;
            canvasGroup.blocksRaycasts = isOwned;
        }
    }

    private Sprite GetBaseUnitSprite(EvolutionRecipe recipe)
    {
        return GetUnitSprite(recipe != null ? recipe.requiredBaseUnit : null);
    }

    private Sprite GetResultUnitSprite(EvolutionRecipe recipe)
    {
        return GetUnitSprite(recipe != null ? recipe.resultUnit : null);
    }

    private string GetBaseUnitName(EvolutionRecipe recipe)
    {
        return GetUnitName(recipe != null ? recipe.requiredBaseUnit : null);
    }

    private string GetResultUnitName(EvolutionRecipe recipe)
    {
        return GetUnitName(recipe != null ? recipe.resultUnit : null);
    }

    private Sprite GetUnitSprite(UnitData unitData)
    {
        if (unitData == null)
            return null;

        return unitData.portraitSprite != null ? unitData.portraitSprite : unitData.unitSprite;
    }

    private string GetUnitName(UnitData unitData)
    {
        if (unitData == null)
            return MissingUnitLabel;

        return string.IsNullOrEmpty(unitData.unitName) ? unitData.name : unitData.unitName;
    }
}
