using System.Collections.Generic;
using UnityEngine;

public static class DamagePopupPool
{
    private static readonly Dictionary<DamagePopup, Queue<DamagePopup>> PoolsByPrefab = new Dictionary<DamagePopup, Queue<DamagePopup>>();
    private static readonly Dictionary<DamagePopup, DamagePopup> PrefabsByInstance = new Dictionary<DamagePopup, DamagePopup>();
    private static Transform poolRoot;

    public static DamagePopup Get(DamagePopup prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
            return null;

        if (!PoolsByPrefab.TryGetValue(prefab, out Queue<DamagePopup> pool))
        {
            pool = new Queue<DamagePopup>();
            PoolsByPrefab[prefab] = pool;
        }

        DamagePopup popup = null;
        while (pool.Count > 0 && popup == null)
            popup = pool.Dequeue();

        if (popup == null)
        {
            popup = Object.Instantiate(prefab, position, rotation);
            PrefabsByInstance[popup] = prefab;
        }
        else
        {
            popup.transform.SetPositionAndRotation(position, rotation);
        }

        popup.transform.SetParent(null, true);
        popup.MarkPooled();
        popup.gameObject.SetActive(true);
        return popup;
    }

    public static void Release(DamagePopup popup)
    {
        if (popup == null)
            return;

        if (!PrefabsByInstance.TryGetValue(popup, out DamagePopup prefab) || prefab == null)
        {
            Object.Destroy(popup.gameObject);
            return;
        }

        popup.gameObject.SetActive(false);
        popup.transform.SetParent(GetPoolRoot(), false);

        if (!PoolsByPrefab.TryGetValue(prefab, out Queue<DamagePopup> pool))
        {
            pool = new Queue<DamagePopup>();
            PoolsByPrefab[prefab] = pool;
        }

        pool.Enqueue(popup);
    }

    private static Transform GetPoolRoot()
    {
        if (poolRoot != null)
            return poolRoot;

        GameObject rootObject = new GameObject("DamagePopupPool");
        poolRoot = rootObject.transform;
        return poolRoot;
    }
}
