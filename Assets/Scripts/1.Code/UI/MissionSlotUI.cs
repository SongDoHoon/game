using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class MissionRequiredUnitSlotUI
{
    public GameObject slotRoot;
    public Image unitImage;
    public TMP_Text unitNameText;
    public Image checkMarkImage;
    public GameObject checkMarkObject;
    public Image backgroundImage;
}

public class MissionSlotUI : MonoBehaviour
{
    [Header("Panel")]
    public GameObject missionPanel;
    public TMP_Text missionNameText;

    [Header("Requirement Slots")]
    public MissionRequiredUnitSlotUI[] requiredUnitSlots = new MissionRequiredUnitSlotUI[3];
    public Color defaultBackgroundColor = Color.white;
    public Color satisfiedBackgroundColor = new(0.45f, 0.85f, 0.55f, 1f);

    [Header("Rewards")]
    public Image rewardGoldIcon;
    public TMP_Text rewardGoldText;
    public Image rewardMagicStoneIcon;
    public TMP_Text rewardMagicStoneText;

    [Header("Clear")]
    public TMP_Text clearText;
    public string clearLabel = "클리어";

    private RuntimeMissionState boundState;

    public void Refresh(RuntimeMissionState state)
    {
        boundState = state;

        if (state == null || state.missionData == null)
        {
            SetActiveIfChanged(gameObject, false);
            return;
        }

        SetActiveIfChanged(gameObject, true);
        RefreshMissionText(state);
        RefreshRequirementSlots(state);
        RefreshRewards(state.missionData);
        RefreshClearText(state);
    }

    public RuntimeMissionState GetBoundState()
    {
        return boundState;
    }

    private void RefreshMissionText(RuntimeMissionState state)
    {
        SetTextIfChanged(missionNameText, state.missionData.missionName);
    }

    private void RefreshRequirementSlots(RuntimeMissionState state)
    {
        for (int i = 0; i < requiredUnitSlots.Length; i++)
        {
            MissionRequiredUnitSlotUI slot = requiredUnitSlots[i];
            if (slot == null)
                continue;

            bool hasRequirement = i < state.resolvedRequirements.Count;
            SetSlotActive(slot, hasRequirement);

            if (!hasRequirement)
                continue;

            MissionRequirement requirement = state.resolvedRequirements[i];
            bool satisfied = i < state.slotSatisfiedStates.Count && state.slotSatisfiedStates[i];
            RefreshRequirementSlot(slot, requirement, satisfied);
        }
    }

    private void RefreshRequirementSlot(MissionRequiredUnitSlotUI slot, MissionRequirement requirement, bool satisfied)
    {
        if (slot.unitImage != null)
        {
            Sprite icon = requirement != null ? requirement.DisplayIcon : null;
            SetSpriteIfChanged(slot.unitImage, icon);
            slot.unitImage.enabled = icon != null;
        }

        SetTextIfChanged(slot.unitNameText, requirement != null ? requirement.DisplayName : string.Empty);

        if (slot.checkMarkImage != null)
            SetActiveIfChanged(slot.checkMarkImage.gameObject, satisfied);

        if (slot.checkMarkObject != null)
            SetActiveIfChanged(slot.checkMarkObject, satisfied);

        if (slot.backgroundImage != null)
            slot.backgroundImage.color = satisfied ? satisfiedBackgroundColor : defaultBackgroundColor;
    }

    private void RefreshRewards(MissionData data)
    {
        SetTextIfChanged(rewardGoldText, data.rewardGold.ToString());

        SetTextIfChanged(rewardMagicStoneText, data.rewardBattleMagicStone.ToString());

        if (rewardGoldIcon != null)
            SetActiveIfChanged(rewardGoldIcon.gameObject, data.rewardGold > 0);

        if (rewardMagicStoneIcon != null)
            SetActiveIfChanged(rewardMagicStoneIcon.gameObject, data.rewardBattleMagicStone > 0);
    }

    private void RefreshClearText(RuntimeMissionState state)
    {
        if (clearText == null)
            return;

        SetActiveIfChanged(clearText.gameObject, state.isCleared);
        SetTextIfChanged(clearText, clearLabel);
    }

    private void SetSlotActive(MissionRequiredUnitSlotUI slot, bool active)
    {
        if (slot.slotRoot != null)
        {
            SetActiveIfChanged(slot.slotRoot, active);
            return;
        }

        if (slot.backgroundImage != null)
            SetActiveIfChanged(slot.backgroundImage.gameObject, active);
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
