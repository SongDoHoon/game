using UnityEngine;

public static class UnitTargetFinder
{
    public static MonsterController FindNearestTarget(Vector3 unitPos, float range)
    {
        MonsterController[] monsters = Object.FindObjectsByType<MonsterController>(FindObjectsSortMode.None);

        MonsterController nearest = null;
        float minDist = float.MaxValue;

        foreach (MonsterController monster in monsters)
        {
            if (!monster.IsAlive) continue;

            float dist = Vector3.Distance(unitPos, monster.transform.position);
            if (dist <= range && dist < minDist)
            {
                minDist = dist;
                nearest = monster;
            }
        }

        return nearest;
    }
}