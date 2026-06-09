using UnityEngine;
using Spine.Unity;

[CreateAssetMenu(menuName = "TD/Unit Data")]
public class UnitData : ScriptableObject
{
    [Header("Basic Info")]
    public string unitId;
    public string unitName;
    public UnitGrade grade;

    [Header("Visual")]
    public Sprite unitSprite;
    public Sprite portraitSprite;
    public bool useGradeTintOnSprite;
    public RuntimeAnimatorController animatorController;

    [Header("Spine Visual")]
    public SkeletonDataAsset spineSkeletonData;
    public bool hideSpriteWhenSpineVisual = true;
    public Vector3 spineLocalPosition = Vector3.zero;
    public Vector3 spineLocalScale = Vector3.one;
    public string spineSortingLayerName = "unit";
    public int spineSortingOrder = 100;
    public string spineLeftSkinName = "left";
    public string spineFrontSkinName = "front";
    public string spineRightSkinName = "right";
    public string spineIdleAnimationName = "idle";
    public string spineBasicAttackAnimationName = "attack";
    public string spineSkillAnimationName = "skill";

    [Header("Combat")]
    public float attackPower = 10f;
    public float attackSpeed = 1f;
    public float attackRange = 3f;
    public float attackRadius = 1.5f;
    public int maxAreaAttackTargets;

    public BasicAttackType basicAttackType;
    public UnitTargetPriority targetPriority = UnitTargetPriority.Nearest;

    [Header("Boss Targeting")]
    public UnitTargetPriority bossFallbackTargetPriority = UnitTargetPriority.Nearest;

    [Header("Basic Attack Projectile")]
    public float projectileSpeed = 8f;
    public float projectileSize = 0.25f;
    public Color projectileColor = Color.white;
    public Vector3 projectileSpawnOffset = new Vector3(0f, 0.35f, 0f);

    [Header("Area Attack Indicator")]
    public bool showAreaAttackIndicator = true;
    public Color areaAttackIndicatorColor = new Color(1f, 0.45f, 0.1f, 0.25f);
    public float areaAttackIndicatorDuration = 0.25f;

    [Header("Skill")]
    public SkillData skillData;
    public float skillAttackLockDurationOverride = -1f;

}
