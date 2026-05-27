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
    public Image[] recipeBaseUnitBackgroundImages;
    public Image[] recipeBaseUnitCheckMarkImages;
    public GameObject[] recipeBaseUnitCheckMarkObjects;
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
    public Color defaultBackgroundColor = Color.white;
    public Color missingRecipeUnitColor = Color.white;
    public Color ownedRecipeUnitColor = new(0.45f, 0.85f, 0.55f, 1f);

    private readonly Dictionary<Image, TMP_Text> runtimeRecipeUnitCheckMarks = new();

    public void Refresh(EvolutionMaterialDisplayData displayData, int ownedCount, IReadOnlyList<EvolutionRecipe> recipes)
    {
        if (displayData == null)
        {
            SetActiveIfChanged(gameObject, false);
            return;
        }

        SetActiveIfChanged(gameObject, true);
        bool isOwned = ownedCount > 0;

        RefreshImages(displayData);
        RefreshTexts(displayData, ownedCount, recipes);
        RefreshRecipeImages(displayData, recipes);
        RefreshRecipeUnitOwnedStates(recipes);
        RefreshOwnedState(isOwned);
    }

    private void RefreshImages(EvolutionMaterialDisplayData displayData)
    {
        if (backgroundImage != null)
        {
            SetSpriteIfChanged(backgroundImage, displayData.backgroundSprite);
            backgroundImage.color = defaultBackgroundColor;
        }

        if (itemImage != null)
        {
            SetSpriteIfChanged(itemImage, displayData.itemSprite);
            itemImage.enabled = displayData.itemSprite != null;
        }
    }

    private void RefreshTexts(EvolutionMaterialDisplayData displayData, int ownedCount, IReadOnlyList<EvolutionRecipe> recipes)
    {
        SetTextIfChanged(itemNameText, displayData.DisplayName);

        SetTextIfChanged(ownedCountText, $"\uBCF4\uC720:{ownedCount}\uAC1C");

        SetTextIfChanged(
            acquisitionText,
            string.IsNullOrEmpty(displayData.acquisitionText)
                ? DefaultAcquisitionText
                : displayData.acquisitionText);

        SetTextIfChanged(ownedBadgeText, OwnedBadgeLabel);

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
            SetSpriteIfChanged(image, hasRecipe ? displayData.itemSprite : null);
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
            SetActiveIfChanged(text.gameObject, hasRecipe);
            SetTextIfChanged(text, hasRecipe ? textGetter(recipes[i]) : string.Empty);
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

            SetSpriteIfChanged(image, sprite);
            image.enabled = sprite != null;
        }
    }

    private void RefreshOwnedState(bool isOwned)
    {
        if (lockImage != null)
            SetActiveIfChanged(lockImage.gameObject, !isOwned);

        if (ownedBadgeText != null)
            SetActiveIfChanged(ownedBadgeText.gameObject, isOwned);

        if (canvasGroup != null)
        {
            float targetAlpha = isOwned ? 1f : lockedAlpha;
            if (!Mathf.Approximately(canvasGroup.alpha, targetAlpha))
                canvasGroup.alpha = targetAlpha;

            if (canvasGroup.interactable != isOwned)
                canvasGroup.interactable = isOwned;

            if (canvasGroup.blocksRaycasts != isOwned)
                canvasGroup.blocksRaycasts = isOwned;
        }
    }

    private void RefreshRecipeUnitOwnedStates(IReadOnlyList<EvolutionRecipe> recipes)
    {
        int maxCount = recipeBaseUnitImages != null ? recipeBaseUnitImages.Length : 0;
        for (int i = 0; i < maxCount; i++)
        {
            Image unitImage = recipeBaseUnitImages[i];
            if (unitImage == null)
                continue;

            EvolutionRecipe recipe = recipes != null && i < recipes.Count ? recipes[i] : null;
            bool hasRecipe = recipe != null && recipe.requiredBaseUnit != null;
            bool hasRequiredUnit = hasRecipe && HasPlacedUnit(recipe.requiredBaseUnit);

            SetRecipeUnitOwnedState(i, unitImage, hasRecipe, hasRequiredUnit);
        }
    }

    private void SetRecipeUnitOwnedState(int index, Image unitImage, bool hasRecipe, bool hasRequiredUnit)
    {
        Image background = GetArrayElement(recipeBaseUnitBackgroundImages, index);
        Image checkImage = GetArrayElement(recipeBaseUnitCheckMarkImages, index);
        GameObject checkObject = GetArrayElement(recipeBaseUnitCheckMarkObjects, index);

        if (background != null)
            background.color = hasRequiredUnit ? ownedRecipeUnitColor : missingRecipeUnitColor;
        else if (unitImage != null)
            unitImage.color = hasRequiredUnit ? ownedRecipeUnitColor : missingRecipeUnitColor;

        if (checkImage != null)
            SetActiveIfChanged(checkImage.gameObject, hasRequiredUnit);

        if (checkObject != null)
            SetActiveIfChanged(checkObject, hasRequiredUnit);
        else if (checkImage == null)
            SetRuntimeRecipeUnitCheckMarkActive(unitImage, hasRecipe && hasRequiredUnit);
    }

    private bool HasPlacedUnit(UnitData requiredUnit)
    {
        if (requiredUnit == null)
            return false;

        UnitPlacementManager placementManager = UnitPlacementManager.Instance != null
            ? UnitPlacementManager.Instance
            : FindAnyObjectByType<UnitPlacementManager>();

        if (placementManager == null)
            return false;

        IReadOnlyList<UnitController> placedUnits = placementManager.GetPlacedUnits();
        foreach (UnitController placedUnit in placedUnits)
        {
            if (placedUnit != null && IsSameUnit(placedUnit.Data, requiredUnit))
                return true;
        }

        return false;
    }

    private static bool IsSameUnit(UnitData left, UnitData right)
    {
        if (left == right)
            return true;

        return left != null
            && right != null
            && !string.IsNullOrEmpty(left.unitId)
            && left.unitId == right.unitId;
    }

    private void SetRuntimeRecipeUnitCheckMarkActive(Image unitImage, bool active)
    {
        TMP_Text checkMarkText = GetOrCreateRuntimeRecipeUnitCheckMarkText(unitImage);
        if (checkMarkText != null)
            SetActiveIfChanged(checkMarkText.gameObject, active);
    }

    private TMP_Text GetOrCreateRuntimeRecipeUnitCheckMarkText(Image unitImage)
    {
        if (unitImage == null)
            return null;

        if (runtimeRecipeUnitCheckMarks.TryGetValue(unitImage, out TMP_Text existingText) && existingText != null)
            return existingText;

        GameObject checkMarkTextObject = new GameObject("Runtime Unit Check Mark", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rectTransform = checkMarkTextObject.GetComponent<RectTransform>();
        rectTransform.SetParent(unitImage.transform, false);
        rectTransform.anchorMin = new Vector2(1f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(1f, 1f);
        rectTransform.anchoredPosition = new Vector2(-4f, -4f);
        rectTransform.sizeDelta = new Vector2(28f, 28f);

        TMP_Text checkMarkText = checkMarkTextObject.GetComponent<TMP_Text>();
        SetTextIfChanged(checkMarkText, "V");
        checkMarkText.alignment = TextAlignmentOptions.Center;
        checkMarkText.fontSize = 24f;
        checkMarkText.fontStyle = FontStyles.Bold;
        checkMarkText.color = Color.white;
        checkMarkText.raycastTarget = false;
        runtimeRecipeUnitCheckMarks[unitImage] = checkMarkText;
        return checkMarkText;
    }

    private static T GetArrayElement<T>(T[] array, int index)
    {
        if (array == null || index < 0 || index >= array.Length)
            return default(T);

        return array[index];
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

    private static void SetTextIfChanged(TMP_Text target, string value)
    {
        if (target == null || target.text == value)
            return;

        target.text = value;
    }

    private static void SetSpriteIfChanged(Image image, Sprite sprite)
    {
        if (image == null || image.sprite == sprite)
            return;

        image.sprite = sprite;
    }

    private static void SetActiveIfChanged(GameObject target, bool value)
    {
        if (target == null || target.activeSelf == value)
            return;

        target.SetActive(value);
    }
}
