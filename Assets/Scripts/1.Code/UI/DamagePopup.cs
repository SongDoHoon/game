using TMPro;
using System;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    [Header("References")]
    public TMP_Text damageText;

    [Header("Motion")]
    public Vector3 moveVelocity = new Vector3(0f, 1.2f, 0f);
    public float lifetime = 0.8f;
    public bool billboardToCamera = true;

    [Header("Fade")]
    public AnimationCurve alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    private float elapsedTime;
    private Color baseColor = Color.white;
    private Camera cachedCamera;

    private void Awake()
    {
        if (damageText == null)
            damageText = GetComponentInChildren<TMP_Text>();

        cachedCamera = Camera.main;
    }

    private void OnEnable()
    {
        elapsedTime = 0f;
    }

    private void Update()
    {
        if (billboardToCamera)
            FaceCamera();

        transform.position += moveVelocity * Time.deltaTime;

        elapsedTime += Time.deltaTime;
        float normalizedTime = lifetime > 0f ? Mathf.Clamp01(elapsedTime / lifetime) : 1f;
        float alpha = alphaCurve.Evaluate(normalizedTime);

        if (damageText != null)
        {
            Color color = baseColor;
            color.a = alpha;
            damageText.color = color;
        }

        if (elapsedTime >= lifetime)
            Destroy(gameObject);
    }

    public void Setup(double damageAmount, Color textColor)
    {
        if (damageText == null)
            damageText = GetComponentInChildren<TMP_Text>();

        baseColor = textColor;

        if (damageText != null)
        {
            damageText.text = Math.Ceiling(damageAmount).ToString("N0");
            damageText.color = baseColor;
        }
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
