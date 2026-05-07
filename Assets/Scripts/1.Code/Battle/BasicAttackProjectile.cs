using UnityEngine;

public class BasicAttackProjectile : MonoBehaviour
{
    private const int CircleTextureSize = 64;
    private const float HitDistance = 0.05f;

    private static Sprite circleSprite;

    private UnitController attacker;
    private MonsterController target;
    private double damage;
    private bool isAreaAttack;
    private float areaRadius;
    private float speed;
    private Color areaIndicatorColor;
    private float areaIndicatorDuration;
    private bool showAreaIndicator;
    private Vector3 lastTargetPosition;

    public static void Spawn(
        UnitController attacker,
        MonsterController target,
        double damage,
        bool isAreaAttack,
        float areaRadius)
    {
        if (attacker == null || target == null || attacker.Data == null)
            return;

        UnitData data = attacker.Data;
        GameObject projectileObject = new GameObject("BasicAttackProjectile");
        projectileObject.transform.position = attacker.transform.position + data.projectileSpawnOffset;
        projectileObject.transform.localScale = Vector3.one * Mathf.Max(0.01f, data.projectileSize);

        SpriteRenderer spriteRenderer = projectileObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetCircleSprite();
        spriteRenderer.color = data.projectileColor;
        spriteRenderer.sortingOrder = 60;

        BasicAttackProjectile projectile = projectileObject.AddComponent<BasicAttackProjectile>();
        projectile.Initialize(attacker, target, damage, isAreaAttack, areaRadius, data);
    }

    private void Initialize(
        UnitController sourceUnit,
        MonsterController targetMonster,
        double attackDamage,
        bool areaAttack,
        float radius,
        UnitData data)
    {
        attacker = sourceUnit;
        target = targetMonster;
        damage = attackDamage;
        isAreaAttack = areaAttack;
        areaRadius = Mathf.Max(0f, radius);
        speed = Mathf.Max(0.01f, data.projectileSpeed);
        showAreaIndicator = data.showAreaAttackIndicator;
        areaIndicatorColor = data.areaAttackIndicatorColor;
        areaIndicatorDuration = Mathf.Max(0.01f, data.areaAttackIndicatorDuration);
        lastTargetPosition = targetMonster != null ? targetMonster.transform.position : transform.position;
    }

    private void Update()
    {
        Vector3 destination = GetDestination();
        transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, destination) > HitDistance)
            return;

        Impact(destination);
    }

    private Vector3 GetDestination()
    {
        if (target != null && target.IsAlive)
            lastTargetPosition = target.transform.position;

        return lastTargetPosition;
    }

    private void Impact(Vector3 impactPosition)
    {
        if (isAreaAttack)
        {
            if (showAreaIndicator && areaRadius > 0f)
                SpawnAreaIndicator(impactPosition, areaRadius, areaIndicatorColor, areaIndicatorDuration);

            DealAreaDamage(impactPosition);
        }
        else if (target != null && target.IsAlive)
        {
            DamageSystem.DealDamage(attacker, target, damage);
        }

        Destroy(gameObject);
    }

    private void DealAreaDamage(Vector3 center)
    {
        MonsterController[] monsters = Object.FindObjectsByType<MonsterController>(FindObjectsSortMode.None);

        foreach (MonsterController monster in monsters)
        {
            if (monster == null || !monster.IsAlive)
                continue;

            if (Vector3.Distance(center, monster.transform.position) <= areaRadius)
                DamageSystem.DealDamage(attacker, monster, damage);
        }
    }

    private static void SpawnAreaIndicator(Vector3 center, float radius, Color color, float duration)
    {
        GameObject indicatorObject = new GameObject("BasicAttackAreaIndicator");
        indicatorObject.transform.position = center;
        indicatorObject.transform.localScale = Vector3.one * Mathf.Max(0.01f, radius * 2f);

        SpriteRenderer spriteRenderer = indicatorObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetCircleSprite();
        spriteRenderer.color = color;
        spriteRenderer.sortingOrder = 20;

        Destroy(indicatorObject, duration);
    }

    private static Sprite GetCircleSprite()
    {
        if (circleSprite != null)
            return circleSprite;

        Texture2D texture = new Texture2D(CircleTextureSize, CircleTextureSize, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        float center = (CircleTextureSize - 1) * 0.5f;
        float radius = center;
        Color clear = new Color(1f, 1f, 1f, 0f);

        for (int y = 0; y < CircleTextureSize; y++)
        {
            for (int x = 0; x < CircleTextureSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                texture.SetPixel(x, y, distance <= radius ? Color.white : clear);
            }
        }

        texture.Apply();
        circleSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, CircleTextureSize, CircleTextureSize),
            new Vector2(0.5f, 0.5f),
            CircleTextureSize);

        return circleSprite;
    }
}
