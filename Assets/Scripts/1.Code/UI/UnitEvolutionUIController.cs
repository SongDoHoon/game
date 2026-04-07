using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitEvolutionUIController : MonoBehaviour
{
    [Header("References")]
    public UnitEvolutionService evolutionService;
    public RectTransform buttonRoot;
    public Button evolveButtonTemplate;
    public Camera worldCamera;

    [Header("Layout")]
    public Vector3 worldOffset = new Vector3(0f, 0.9f, 0f);

    private readonly Dictionary<UnitController, Button> buttonsByUnit = new Dictionary<UnitController, Button>();
    private Canvas rootCanvas;
    private float scanTimer;

    private void Awake()
    {
        if (evolutionService == null)
            evolutionService = FindFirstObjectByType<UnitEvolutionService>();

        if (worldCamera == null)
            worldCamera = Camera.main;

        if (buttonRoot == null && evolveButtonTemplate != null)
            buttonRoot = evolveButtonTemplate.transform.parent as RectTransform;

        if (buttonRoot != null)
            rootCanvas = buttonRoot.GetComponentInParent<Canvas>();

        if (evolveButtonTemplate != null)
            evolveButtonTemplate.gameObject.SetActive(false);
    }

    private void Update()
    {
        scanTimer -= Time.deltaTime;

        if (scanTimer <= 0f)
        {
            scanTimer = 0.2f;
            RefreshButtons();
        }

        UpdateButtonPositions();
    }

    private void RefreshButtons()
    {
        UnitController[] units = FindObjectsByType<UnitController>();
        HashSet<UnitController> aliveUnits = new HashSet<UnitController>(units);
        List<UnitController> staleUnits = new List<UnitController>();

        foreach (KeyValuePair<UnitController, Button> pair in buttonsByUnit)
        {
            if (pair.Key == null || !aliveUnits.Contains(pair.Key))
                staleUnits.Add(pair.Key);
        }

        foreach (UnitController staleUnit in staleUnits)
            RemoveButton(staleUnit);

        foreach (UnitController unit in units)
        {
            if (unit == null || unit.Data == null)
                continue;

            EvolutionRecipe recipe = null;
            bool canEvolve = evolutionService != null
                && evolutionService.TryGetAvailableRecipe(unit, out recipe);

            if (!canEvolve)
            {
                HideButton(unit);
                continue;
            }

            Button button = GetOrCreateButton(unit);
            if (button == null)
                continue;

            SetButtonLabel(button, recipe.resultUnit != null
                ? $"Evolve: {recipe.resultUnit.unitName}"
                : "Evolve");

            button.gameObject.SetActive(true);
        }
    }

    private void UpdateButtonPositions()
    {
        if (buttonRoot == null)
            return;

        foreach (KeyValuePair<UnitController, Button> pair in buttonsByUnit)
        {
            UnitController unit = pair.Key;
            Button button = pair.Value;

            if (unit == null || button == null || !button.gameObject.activeSelf)
                continue;

            Vector3 screenPoint = RectTransformUtility.WorldToScreenPoint(worldCamera, unit.transform.position + worldOffset);
            RectTransform buttonRect = button.transform as RectTransform;
            Camera uiCamera = rootCanvas != null && rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : worldCamera;

            if (buttonRect == null)
                continue;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(buttonRoot, screenPoint, uiCamera, out Vector2 localPoint))
                buttonRect.anchoredPosition = localPoint;
        }
    }

    private Button GetOrCreateButton(UnitController unit)
    {
        if (unit == null)
            return null;

        if (buttonsByUnit.TryGetValue(unit, out Button existingButton))
            return existingButton;

        if (evolveButtonTemplate == null || buttonRoot == null)
            return null;

        Button newButton = Instantiate(evolveButtonTemplate, buttonRoot);
        newButton.gameObject.SetActive(true);
        newButton.onClick.AddListener(() => OnClickEvolve(unit));
        buttonsByUnit[unit] = newButton;
        return newButton;
    }

    private void HideButton(UnitController unit)
    {
        if (unit == null)
            return;

        if (!buttonsByUnit.TryGetValue(unit, out Button button) || button == null)
            return;

        button.gameObject.SetActive(false);
    }

    private void RemoveButton(UnitController unit)
    {
        if (unit == null)
            return;

        if (!buttonsByUnit.TryGetValue(unit, out Button button))
            return;

        buttonsByUnit.Remove(unit);

        if (button != null)
            Destroy(button.gameObject);
    }

    private void OnClickEvolve(UnitController unit)
    {
        if (unit == null || evolutionService == null)
            return;

        bool success = evolutionService.TryEvolveFirstAvailable(unit);

        if (!success)
            return;

        HideButton(unit);
    }

    private void SetButtonLabel(Button button, string label)
    {
        if (button == null)
            return;

        TMP_Text tmpText = button.GetComponentInChildren<TMP_Text>(true);
        if (tmpText != null)
        {
            tmpText.text = label;
            return;
        }

        Text legacyText = button.GetComponentInChildren<Text>(true);
        if (legacyText != null)
            legacyText.text = label;
    }
}
