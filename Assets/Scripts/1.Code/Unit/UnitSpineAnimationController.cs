using Spine;
using Spine.Unity;
using UnityEngine;

public class UnitSpineAnimationController : MonoBehaviour
{
    private const string SpineVisualObjectName = "UnitSpineVisual";
    private const float DefaultAttackAnimationDuration = 0.5f;

    private UnitController owner;
    private UnitData data;
    private SkeletonAnimation skeletonAnimation;
    private SkeletonRenderer skeletonRenderer;
    private MeshRenderer meshRenderer;

    public bool HasSpineVisual => skeletonAnimation != null && data != null && data.spineSkeletonData != null;

    public void Configure(UnitController unit, UnitData unitData)
    {
        owner = unit;
        data = unitData;

        if (data == null || data.spineSkeletonData == null)
        {
            SetVisible(false);
            return;
        }

        EnsureSkeletonAnimation();
        transform.localPosition = data.spineLocalPosition;
        transform.localRotation = Quaternion.identity;
        transform.localScale = data.spineLocalScale;

        skeletonAnimation.loop = true;
        skeletonAnimation.timeScale = 1f;
        skeletonAnimation.SkeletonDataAsset = data.spineSkeletonData;
        skeletonAnimation.Initialize(true, true);

        ApplySorting();
        SetVisible(true);
        PlayIdle(UnitSpineFacingDirection.Left);
    }

    public void PlayIdle(UnitSpineFacingDirection direction)
    {
        if (!CanPlay())
            return;

        ApplySkin(direction);
        PlayAnimation(data.spineIdleAnimationName, true, false);
    }

    public void PlayBasicAttack(MonsterController target)
    {
        if (!CanPlay())
            return;

        UnitSpineFacingDirection direction = ResolveDirection(target);
        ApplySkin(direction);
        PlayAnimation(data.spineBasicAttackAnimationName, false, true);
    }

    public float GetBasicAttackAnimationDuration()
    {
        if (!CanPlay() || string.IsNullOrWhiteSpace(data.spineBasicAttackAnimationName))
            return DefaultAttackAnimationDuration;

        Spine.Animation animation = skeletonAnimation.Skeleton.Data.FindAnimation(data.spineBasicAttackAnimationName);
        if (animation == null)
            return DefaultAttackAnimationDuration;

        float animationTimeScale = Mathf.Max(0.0001f, skeletonAnimation.timeScale);
        return Mathf.Max(0f, animation.Duration / animationTimeScale);
    }

    public void PlaySkill(MonsterController target)
    {
        if (!CanPlay())
            return;

        UnitSpineFacingDirection direction = ResolveDirection(target);
        ApplySkin(direction);

        string animationName = string.IsNullOrWhiteSpace(data.spineSkillAnimationName)
            ? data.spineBasicAttackAnimationName
            : data.spineSkillAnimationName;
        if (!HasAnimation(animationName))
            animationName = data.spineBasicAttackAnimationName;

        PlayAnimation(animationName, false, true);
    }

    private void EnsureSkeletonAnimation()
    {
        if (skeletonAnimation != null && skeletonRenderer != null)
            return;

        skeletonAnimation = GetComponent<SkeletonAnimation>();
        skeletonRenderer = GetComponent<SkeletonRenderer>();

        if (skeletonAnimation == null || skeletonRenderer == null)
        {
            SkeletonComponents<SkeletonRenderer, SkeletonAnimation> components =
                SkeletonRenderer.AddSpineComponents<SkeletonRenderer, SkeletonAnimation>(
                    gameObject,
                    data.spineSkeletonData,
                    true);

            skeletonRenderer = components.skeletonRenderer;
            skeletonAnimation = components.skeletonAnimation;
        }

        meshRenderer = GetComponent<MeshRenderer>();
    }

    private void ApplySorting()
    {
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();

        if (meshRenderer == null)
            return;

        if (!string.IsNullOrWhiteSpace(data.spineSortingLayerName))
            meshRenderer.sortingLayerName = data.spineSortingLayerName;

        meshRenderer.sortingOrder = data.spineSortingOrder;
    }

    private bool CanPlay()
    {
        return skeletonAnimation != null
            && skeletonAnimation.Skeleton != null
            && skeletonAnimation.AnimationState != null
            && data != null
            && data.spineSkeletonData != null;
    }

    private void PlayAnimation(string animationName, bool loop, bool returnToIdle)
    {
        if (string.IsNullOrWhiteSpace(animationName) || !HasAnimation(animationName))
        {
            if (!loop)
                PlayIdle(GetCurrentDirection());

            return;
        }

        skeletonAnimation.AnimationState.SetAnimation(0, animationName, loop);

        if (returnToIdle && HasAnimation(data.spineIdleAnimationName))
            skeletonAnimation.AnimationState.AddAnimation(0, data.spineIdleAnimationName, true, 0f);
    }

    private void ApplySkin(UnitSpineFacingDirection direction)
    {
        string skinName = GetAvailableSkinName(direction);
        if (string.IsNullOrWhiteSpace(skinName))
            return;

        skeletonAnimation.Skeleton.SetSkin(skinName);
        skeletonAnimation.Skeleton.SetupPoseSlots();
        skeletonAnimation.AnimationState.Apply(skeletonAnimation.Skeleton);
    }

    private string GetAvailableSkinName(UnitSpineFacingDirection direction)
    {
        string requestedSkinName = GetSkinName(direction);
        if (HasSkin(requestedSkinName))
            return requestedSkinName;

        string leftSkinName = GetSkinName(UnitSpineFacingDirection.Left);
        if (HasSkin(leftSkinName))
            return leftSkinName;

        return string.Empty;
    }

    private string GetSkinName(UnitSpineFacingDirection direction)
    {
        return direction switch
        {
            UnitSpineFacingDirection.Front => data.spineFrontSkinName,
            UnitSpineFacingDirection.Right => data.spineRightSkinName,
            _ => data.spineLeftSkinName
        };
    }

    private bool HasSkin(string skinName)
    {
        if (string.IsNullOrWhiteSpace(skinName))
            return false;

        SkeletonData skeletonData = skeletonAnimation.Skeleton.Data;
        return skeletonData != null && skeletonData.FindSkin(skinName) != null;
    }

    private bool HasAnimation(string animationName)
    {
        if (string.IsNullOrWhiteSpace(animationName))
            return false;

        SkeletonData skeletonData = skeletonAnimation.Skeleton.Data;
        return skeletonData != null && skeletonData.FindAnimation(animationName) != null;
    }

    private UnitSpineFacingDirection ResolveDirection(MonsterController target)
    {
        Transform targetTransform = target != null ? target.transform : null;
        if (targetTransform == null && owner != null && owner.GetCurrentTarget() != null)
            targetTransform = owner.GetCurrentTarget().transform;

        if (targetTransform == null || owner == null)
            return UnitSpineFacingDirection.Left;

        Vector3 offset = targetTransform.position - owner.transform.position;
        if (offset.y < 0f && Mathf.Abs(offset.y) >= Mathf.Abs(offset.x))
            return UnitSpineFacingDirection.Front;

        if (offset.x > 0f)
            return UnitSpineFacingDirection.Right;

        return UnitSpineFacingDirection.Left;
    }

    private UnitSpineFacingDirection GetCurrentDirection()
    {
        return ResolveDirection(null);
    }

    private void SetVisible(bool visible)
    {
        if (skeletonAnimation != null)
            skeletonAnimation.enabled = visible;

        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();

        if (meshRenderer != null)
            meshRenderer.enabled = visible;
    }

    public static UnitSpineAnimationController GetOrCreate(UnitController owner)
    {
        if (owner == null)
            return null;

        Transform existing = owner.transform.Find(SpineVisualObjectName);
        if (existing != null)
        {
            UnitSpineAnimationController existingController = existing.GetComponent<UnitSpineAnimationController>();
            if (existingController != null)
                return existingController;
        }

        GameObject visualObject = new GameObject(SpineVisualObjectName);
        visualObject.transform.SetParent(owner.transform, false);
        return visualObject.AddComponent<UnitSpineAnimationController>();
    }
}

public enum UnitSpineFacingDirection
{
    Left,
    Front,
    Right
}
