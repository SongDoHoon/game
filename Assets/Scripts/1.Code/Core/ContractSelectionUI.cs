using System.Collections.Generic;
using UnityEngine;

public class ContractSelectionUI : MonoBehaviour
{
    private static readonly Rect WindowRect = new Rect(60f, 40f, 980f, 620f);

    private ContractManager contractManager;
    private IReadOnlyList<ContractData> options;
    private bool isVisible;
    private Vector2 scrollPosition;

    public static ContractSelectionUI EnsureInstance(ContractManager manager)
    {
        ContractSelectionUI existing = FindAnyObjectByType<ContractSelectionUI>();
        if (existing != null)
        {
            existing.contractManager = manager;
            return existing;
        }

        GameObject root = new GameObject("ContractSelectionUI");
        ContractSelectionUI ui = root.AddComponent<ContractSelectionUI>();
        ui.contractManager = manager;
        return ui;
    }

    public void Show(IReadOnlyList<ContractData> contractOptions)
    {
        options = contractOptions;
        isVisible = true;
        scrollPosition = Vector2.zero;
    }

    public void Hide()
    {
        isVisible = false;
        options = null;
    }

    private void OnGUI()
    {
        if (!isVisible || contractManager == null || options == null)
            return;

        GUILayout.Window(GetInstanceID(), WindowRect, DrawWindow, "운명의 계약");
    }

    private void DrawWindow(int windowId)
    {
        GUILayout.Label($"남은 시간: {Mathf.CeilToInt(contractManager.SelectionRemainingTime)}초");
        GUILayout.Space(8f);

        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        for (int i = 0; i < options.Count; i++)
        {
            ContractData contract = options[i];
            if (contract == null)
                continue;

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label($"[{contract.GetGradeDisplayName()}]");
            GUILayout.Label(contract.contractName);
            GUILayout.TextArea(contract.description, GUILayout.Height(92f));

            if (GUILayout.Button("선택", GUILayout.Height(30f)))
                contractManager.SelectOfferedContract(i);

            GUILayout.EndVertical();
            GUILayout.Space(6f);
        }

        GUILayout.EndScrollView();
    }
}
