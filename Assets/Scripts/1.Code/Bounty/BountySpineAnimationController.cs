using Spine.Unity;
using UnityEngine;

[System.Serializable]
public class BountySpineAppearance
{
    [HideInInspector] public int difficulty;
    public string bountyName;
    public SkeletonDataAsset skeletonData;
    public string loopAnimationName = "walk";
    public string skinName;
    public Vector3 localPosition = Vector3.zero;
    public Vector3 localScale = Vector3.one;
    public float animationTimeScale = 1f;
    public bool spineFacesRightByDefault = true;
    public string sortingLayerName = "Default";
    public int sortingOrder;
}

public class BountySpineAnimationController : MonoBehaviour
{
    // Created at runtime so bounty monsters can share the existing movement and combat prefab.
    private const string SpineVisualObjectName = "BountySpineVisual";

    private MonsterController owner;
    private BountySpineAppearance appearance;
    private SkeletonAnimation skeletonAnimation;
    private SkeletonRenderer skeletonRenderer;
    private MeshRenderer meshRenderer;
    private Vector3 baseLocalScale;
    private Vector3 lastOwnerPosition;
    private bool configured;

    public bool Configure(MonsterController monster, BountySpineAppearance spineAppearance)
    {
        if (monster == null || spineAppearance == null || spineAppearance.skeletonData == null)
            return false;

        owner = monster;
        appearance = spineAppearance;

        EnsureSkeletonAnimation();
        if (skeletonAnimation == null)
            return false;

        transform.localPosition = appearance.localPosition;
        transform.localRotation = Quaternion.identity;
        transform.localScale = appearance.localScale;
        baseLocalScale = transform.localScale;

        skeletonAnimation.loop = true;
        skeletonAnimation.timeScale = Mathf.Max(0f, appearance.animationTimeScale);
        skeletonAnimation.SkeletonDataAsset = appearance.skeletonData;
        skeletonAnimation.Initialize(true, true);

        if (skeletonAnimation.Skeleton == null || skeletonAnimation.AnimationState == null)
            return false;

        ApplySkin();
        ApplySorting();
        PlayLoopAnimation();
        HideOriginalSpriteRenderers();

        lastOwnerPosition = owner.transform.position;
        configured = true;
        return true;
    }

    private void LateUpdate()
    {
        if (!configured || owner == null || !owner.faceMovementDirection)
            return;

        Vector3 currentPosition = owner.transform.position;
        float horizontalMovement = currentPosition.x - lastOwnerPosition.x;
        lastOwnerPosition = currentPosition;

        if (Mathf.Abs(horizontalMovement) < owner.horizontalFacingThreshold)
            return;

        ApplyFacing(horizontalMovement > 0f);
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
                    appearance.skeletonData,
                    true);

            skeletonRenderer = components.skeletonRenderer;
            skeletonAnimation = components.skeletonAnimation;
        }

        meshRenderer = GetComponent<MeshRenderer>();
    }

    private void ApplySkin()
    {
        if (string.IsNullOrWhiteSpace(appearance.skinName))
            return;

        if (skeletonAnimation.Skeleton.Data.FindSkin(appearance.skinName) == null)
        {
            Debug.LogWarning(
                $"현상금 {appearance.difficulty} ({appearance.bountyName}) Spine에 '{appearance.skinName}' 스킨이 없습니다.",
                owner);
            return;
        }

        skeletonAnimation.Skeleton.SetSkin(appearance.skinName);
        skeletonAnimation.Skeleton.SetupPoseSlots();
        skeletonAnimation.AnimationState.Apply(skeletonAnimation.Skeleton);
    }

    private void ApplySorting()
    {
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();

        if (meshRenderer == null)
            return;

        if (!string.IsNullOrWhiteSpace(appearance.sortingLayerName))
            meshRenderer.sortingLayerName = appearance.sortingLayerName;

        meshRenderer.sortingOrder = appearance.sortingOrder;
    }

    private void PlayLoopAnimation()
    {
        if (string.IsNullOrWhiteSpace(appearance.loopAnimationName))
        {
            Debug.LogWarning(
                $"현상금 {appearance.difficulty} ({appearance.bountyName})의 반복 애니메이션 이름이 비어 있습니다.",
                owner);
            return;
        }

        if (skeletonAnimation.Skeleton.Data.FindAnimation(appearance.loopAnimationName) == null)
        {
            Debug.LogWarning(
                $"현상금 {appearance.difficulty} ({appearance.bountyName}) Spine에 '{appearance.loopAnimationName}' 애니메이션이 없습니다.",
                owner);
            return;
        }

        skeletonAnimation.AnimationState.SetAnimation(0, appearance.loopAnimationName, true);
    }

    private void ApplyFacing(bool movingRight)
    {
        bool shouldFlip = appearance.spineFacesRightByDefault ? !movingRight : movingRight;
        Vector3 scale = baseLocalScale;
        scale.x = Mathf.Abs(baseLocalScale.x) * (shouldFlip ? -1f : 1f);
        transform.localScale = scale;
    }

    private void HideOriginalSpriteRenderers()
    {
        SpriteRenderer[] renderers = owner.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer targetRenderer in renderers)
        {
            if (targetRenderer != null)
                targetRenderer.enabled = false;
        }
    }

    public static BountySpineAnimationController GetOrCreate(MonsterController monster)
    {
        if (monster == null)
            return null;

        Transform existing = monster.transform.Find(SpineVisualObjectName);
        if (existing != null)
        {
            BountySpineAnimationController existingController =
                existing.GetComponent<BountySpineAnimationController>();

            if (existingController != null)
                return existingController;
        }

        GameObject visualObject = new GameObject(SpineVisualObjectName);
        visualObject.transform.SetParent(monster.transform, false);
        return visualObject.AddComponent<BountySpineAnimationController>();
    }
}
