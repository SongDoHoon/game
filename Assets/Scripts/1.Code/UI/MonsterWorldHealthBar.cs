using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MonsterWorldHealthBar : MonoBehaviour
{
    [Header("References")]
    public MonsterController targetMonster;
    public Slider hpSlider;
    public TMP_Text hpText;

    [Header("View")]
    public bool billboardToCamera = true;

    private Camera cachedCamera;

    private void Awake()
    {
        if (targetMonster == null)
            targetMonster = GetComponentInParent<MonsterController>();

        if (hpSlider != null)
        {
            hpSlider.minValue = 0f;
            hpSlider.maxValue = 1f;
        }

        cachedCamera = Camera.main;
        RefreshImmediately();
    }

    private void OnEnable()
    {
        Subscribe();
        RefreshImmediately();
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

    private void HandleHpChanged(MonsterController monster, float currentHp, float maxHp)
    {
        Refresh(currentHp, maxHp);
    }

    private void RefreshImmediately()
    {
        if (targetMonster == null)
            return;

        Refresh(targetMonster.CurrentHp, targetMonster.MaxHp);
    }

    private void Refresh(float currentHp, float maxHp)
    {
        float normalized = maxHp > 0f ? currentHp / maxHp : 0f;

        if (hpSlider != null)
            hpSlider.value = normalized;

        if (hpText != null)
            hpText.text = $"{Mathf.CeilToInt(currentHp)}/{Mathf.CeilToInt(maxHp)}";
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
