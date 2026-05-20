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
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
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
        if (missionNameText != null)
            missionNameText.text = state.missionData.missionName;
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
            slot.unitImage.sprite = icon;
            slot.unitImage.enabled = icon != null;
        }

        if (slot.unitNameText != null)
            slot.unitNameText.text = requirement != null ? requirement.DisplayName : string.Empty;

        if (slot.checkMarkImage != null)
            slot.checkMarkImage.gameObject.SetActive(satisfied);

        if (slot.checkMarkObject != null)
            slot.checkMarkObject.SetActive(satisfied);

        if (slot.backgroundImage != null)
            slot.backgroundImage.color = satisfied ? satisfiedBackgroundColor : defaultBackgroundColor;
    }

    private void RefreshRewards(MissionData data)
    {
        if (rewardGoldText != null)
            rewardGoldText.text = data.rewardGold.ToString();

        if (rewardMagicStoneText != null)
            rewardMagicStoneText.text = data.rewardBattleMagicStone.ToString();

        if (rewardGoldIcon != null)
            rewardGoldIcon.gameObject.SetActive(data.rewardGold > 0);

        if (rewardMagicStoneIcon != null)
            rewardMagicStoneIcon.gameObject.SetActive(data.rewardBattleMagicStone > 0);
    }

    private void RefreshClearText(RuntimeMissionState state)
    {
        if (clearText == null)
            return;

        clearText.gameObject.SetActive(state.isCleared);
        clearText.text = clearLabel;
    }

    private void SetSlotActive(MissionRequiredUnitSlotUI slot, bool active)
    {
        if (slot.slotRoot != null)
        {
            slot.slotRoot.SetActive(active);
            return;
        }

        if (slot.backgroundImage != null)
            slot.backgroundImage.gameObject.SetActive(active);
    }
}
