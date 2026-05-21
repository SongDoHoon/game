using System;
using UnityEngine;

public static class InGamePanelCoordinator
{
    public static void CloseOtherPanels(GameObject currentPanelRoot)
    {
        ClosePanels(currentPanelRoot);
    }

    public static void CloseAllPanels()
    {
        ClosePanels(null);
    }

    private static void ClosePanels(GameObject currentPanelRoot)
    {
        ClosePanelRoots(UnityEngine.Object.FindObjectsByType<MissionScrollViewUI>(FindObjectsInactive.Include), panel => panel.panelRoot, currentPanelRoot);
        ClosePanelRoots(UnityEngine.Object.FindObjectsByType<EvolutionMaterialPanelManager>(FindObjectsInactive.Include), panel => panel.panelRoot, currentPanelRoot);
        ClosePanelRoots(UnityEngine.Object.FindObjectsByType<BountyUIController>(FindObjectsInactive.Include), panel => panel.panelRoot, currentPanelRoot);
        ClosePanelRoots(UnityEngine.Object.FindObjectsByType<DivineGraveUIController>(FindObjectsInactive.Include), panel => panel.panelRoot, currentPanelRoot);
        ClosePanelRoots(UnityEngine.Object.FindObjectsByType<EnhancementUIController>(FindObjectsInactive.Include), panel => panel.enhancementPanel, currentPanelRoot);
        ClosePanelRoots(UnityEngine.Object.FindObjectsByType<EnhancementPanelManager>(FindObjectsInactive.Include), panel => panel.enhancementPanel, currentPanelRoot);
        ClosePanelRoots(UnityEngine.Object.FindObjectsByType<UnitShardUpgradePanelUI>(FindObjectsInactive.Include), panel => panel.panelRoot, currentPanelRoot);
    }

    private static void ClosePanelRoots<T>(T[] panels, Func<T, GameObject> getPanelRoot, GameObject currentPanelRoot)
    {
        if (panels == null)
            return;

        foreach (T panel in panels)
        {
            GameObject panelRoot = getPanelRoot(panel);
            if (panelRoot == null || panelRoot == currentPanelRoot)
                continue;

            panelRoot.SetActive(false);
        }
    }
}
