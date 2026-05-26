using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class MonsterHealthBarViewSettings
{
    public Vector3 localPositionOffset = Vector3.zero;
    public Vector3 localScale = Vector3.one;
    public bool overrideSize = false;
    public Vector2 sizeDelta = new Vector2(1f, 0.15f);
    public bool showHpText = true;
    public float hpTextFontSize = 0f;
    public bool overrideFillColor = false;
    public Color fillColor = Color.green;
}

public class MonsterWorldHealthBar : MonoBehaviour
{
    [Header("References")]
    public MonsterController targetMonster;
    public Slider hpSlider;
    public TMP_Text hpText;

    [Header("View")]
    public bool billboardToCamera = true;
    public MonsterHealthBarViewSettings normalView = new MonsterHealthBarViewSettings();
    public MonsterHealthBarViewSettings bossView = new MonsterHealthBarViewSettings
    {
        localScale = new Vector3(1.25f, 1.25f, 1f),
        fillColor = new Color(0.9f, 0.2f, 0.2f, 1f)
    };

    private Camera cachedCamera;
    private bool hasCachedBaseView;
    private Vector3 baseLocalPosition;
    private Vector3 baseLocalScale;
    private Vector2 baseSizeDelta;
    private float baseHpTextFontSize;
    private Color baseFillColor;
    private RectTransform healthBarRect;
    private Image hpFillImage;
    private float lastSliderValue = -1f;
    private string lastHpText;

    private void Awake()
    {
        if (targetMonster == null)
            targetMonster = GetComponentInParent<MonsterController>();

        EnsureViewReferences();

        if (hpSlider != null)
        {
            hpSlider.minValue = 0f;
            hpSlider.maxValue = 1f;
        }

        cachedCamera = Camera.main;
        ApplyViewSettings();
        RefreshImmediately();
    }

    private void OnEnable()
    {
        Subscribe();
        ApplyViewSettings();
        RefreshImmediately();
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
            ApplyViewSettings();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void LateUpdate()
    {
        if (billboardToCamera)
            FaceCamera();
    }

    private void Subscribe()
    {
        if (targetMonster == null)
            return;

        targetMonster.OnHpChanged -= HandleHpChanged;
        targetMonster.OnHpChanged += HandleHpChanged;
    }

    private void Unsubscribe()
    {
        if (targetMonster == null)
            return;

        targetMonster.OnHpChanged -= HandleHpChanged;
    }

    private void HandleHpChanged(MonsterController monster, double currentHp, double maxHp)
    {
        Refresh(currentHp, maxHp);
    }

    private void RefreshImmediately()
    {
        if (targetMonster == null)
            return;

        Refresh(targetMonster.CurrentHp, targetMonster.MaxHp);
    }

    private void Refresh(double currentHp, double maxHp)
    {
        ApplyViewSettings();

        float normalized = maxHp > 0.0 ? Mathf.Clamp01((float)(currentHp / maxHp)) : 0f;

        if (hpSlider != null && !Mathf.Approximately(lastSliderValue, normalized))
        {
            hpSlider.value = normalized;
            lastSliderValue = normalized;
        }

        if (hpText != null)
        {
            string hpMessage = $"{BattleNumberFormatter.Format(currentHp)}/{BattleNumberFormatter.Format(maxHp)}";
            if (lastHpText != hpMessage)
            {
                hpText.text = hpMessage;
                lastHpText = hpMessage;
            }
        }
    }

    private void ApplyViewSettings()
    {
        EnsureViewReferences();
        CacheBaseView();

        MonsterHealthBarViewSettings settings = IsBossTarget() ? bossView : normalView;
        if (settings == null)
            return;

        transform.localPosition = baseLocalPosition + settings.localPositionOffset;
        transform.localScale = Vector3.Scale(baseLocalScale, settings.localScale);

        if (healthBarRect != null)
            healthBarRect.sizeDelta = settings.overrideSize ? settings.sizeDelta : baseSizeDelta;

        if (hpText != null)
        {
            hpText.gameObject.SetActive(settings.showHpText);
            hpText.fontSize = settings.hpTextFontSize > 0f ? settings.hpTextFontSize : baseHpTextFontSize;
        }

        if (hpFillImage != null)
            hpFillImage.color = settings.overrideFillColor ? settings.fillColor : baseFillColor;
    }

    private void EnsureViewReferences()
    {
        if (healthBarRect == null)
            healthBarRect = transform as RectTransform;

        if (hpFillImage == null && hpSlider != null && hpSlider.fillRect != null)
            hpFillImage = hpSlider.fillRect.GetComponent<Image>();
    }

    private void CacheBaseView()
    {
        if (hasCachedBaseView)
            return;

        baseLocalPosition = transform.localPosition;
        baseLocalScale = transform.localScale;
        baseSizeDelta = healthBarRect != null ? healthBarRect.sizeDelta : Vector2.zero;
        baseHpTextFontSize = hpText != null ? hpText.fontSize : 0f;
        baseFillColor = hpFillImage != null ? hpFillImage.color : Color.white;
        hasCachedBaseView = true;
    }

    private bool IsBossTarget()
    {
        return targetMonster != null && (targetMonster.isBoss || targetMonster.monsterType == MonsterType.Boss);
    }

    private void FaceCamera()
    {
        if (cachedCamera == null)
            cachedCamera = Camera.main;

        if (cachedCamera == null)
            return;

        Transform cameraTransform = cachedCamera.transform;
        Vector3 toCamera = transform.position - cameraTransform.position;

        if (toCamera.sqrMagnitude <= 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(toCamera.normalized, cameraTransform.up);
    }
}
