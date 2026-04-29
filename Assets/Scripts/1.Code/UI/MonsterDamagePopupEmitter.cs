using UnityEngine;

public class MonsterDamagePopupEmitter : MonoBehaviour
{
    [Header("References")]
    public MonsterController targetMonster;
    public DamagePopup damagePopupPrefab;
    public Transform popupSpawnPoint;

    [Header("Spawn Offset")]
    public Vector3 worldOffset = new Vector3(0f, 1.6f, 0f);
    public Vector2 randomOffsetRange = new Vector2(0.25f, 0.15f);

    [Header("Text Color")]
    public Color textColor = Color.white;

    private void Awake()
    {
        if (targetMonster == null)
            targetMonster = GetComponent<MonsterController>();
    }

    private void OnEnable()
    {
        if (targetMonster == null)
            return;

        targetMonster.OnDamageTaken -= HandleDamageTaken;
        targetMonster.OnDamageTaken += HandleDamageTaken;
    }

    private void OnDisable()
    {
        if (targetMonster == null)
            return;

        targetMonster.OnDamageTaken -= HandleDamageTaken;
    }

    private void HandleDamageTaken(MonsterController monster, double damageAmount)
    {
        if (damagePopupPrefab == null)
            return;

        Vector3 basePosition = popupSpawnPoint != null
            ? popupSpawnPoint.position
            : transform.position;

        Vector3 randomOffset = new Vector3(
            Random.Range(-randomOffsetRange.x, randomOffsetRange.x),
            Random.Range(-randomOffsetRange.y, randomOffsetRange.y),
            0f);

        DamagePopup popupInstance = Instantiate(
            damagePopupPrefab,
            basePosition + worldOffset + randomOffset,
            Quaternion.identity);

        popupInstance.Setup(damageAmount, textColor);
    }
}
