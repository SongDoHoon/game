using UnityEngine;

public static class UnitTargetFinder
{
    public static MonsterController FindNearestTarget(Vector3 unitPos, float range)
    {
        return FindTarget(unitPos, range, UnitTargetPriority.Nearest, UnitTargetPriority.Nearest);
    }

    public static MonsterController FindTarget(
        Vector3 unitPos,
        float range,
        UnitTargetPriority priority,
        UnitTargetPriority bossFallbackPriority)
    {
        MonsterController[] monsters = Object.FindObjectsByType<MonsterController>(FindObjectsSortMode.None);

        return priority switch
        {
            UnitTargetPriority.Farthest => FindFarthestTarget(monsters, unitPos, range),
            UnitTargetPriority.Boss => FindBossTarget(monsters, unitPos, range, bossFallbackPriority),
            UnitTargetPriority.LowestHp => FindLowestHpTarget(monsters, unitPos, range),
            _ => FindNearestTarget(monsters, unitPos, range)
        };
    }

    private static MonsterController FindNearestTarget(MonsterController[] monsters, Vector3 unitPos, float range)
    {
        MonsterController target = null;
        float bestDistance = float.MaxValue;

        foreach (MonsterController monster in monsters)
        {
            if (!IsValidTarget(monster, unitPos, range, out float distance))
                continue;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                target = monster;
            }
        }

        return target;
    }

    private static MonsterController FindFarthestTarget(MonsterController[] monsters, Vector3 unitPos, float range)
    {
        MonsterController target = null;
        float bestDistance = float.MinValue;

        foreach (MonsterController monster in monsters)
        {
            if (!IsValidTarget(monster, unitPos, range, out float distance))
                continue;

            if (distance > bestDistance)
            {
                bestDistance = distance;
                target = monster;
            }
        }

        return target;
    }

    private static MonsterController FindBossTarget(
        MonsterController[] monsters,
        Vector3 unitPos,
        float range,
        UnitTargetPriority fallbackPriority)
    {
        MonsterController bossTarget = null;
        float bestDistance = float.MaxValue;

        foreach (MonsterController monster in monsters)
        {
            if (monster == null || monster.monsterType != MonsterType.Boss)
                continue;

            if (!IsValidTarget(monster, unitPos, range, out float distance))
                continue;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bossTarget = monster;
            }
        }

        if (bossTarget != null)
            return bossTarget;

        UnitTargetPriority safeFallback = fallbackPriority == UnitTargetPriority.Boss
            ? UnitTargetPriority.Nearest
            : fallbackPriority;

        return FindTarget(unitPos, range, safeFallback, UnitTargetPriority.Nearest);
    }

    private static MonsterController FindLowestHpTarget(MonsterController[] monsters, Vector3 unitPos, float range)
    {
        MonsterController target = null;
        double bestHp = double.MaxValue;
        float bestDistance = float.MaxValue;

        foreach (MonsterController monster in monsters)
        {
            if (!IsValidTarget(monster, unitPos, range, out float distance))
                continue;

            if (monster.CurrentHp < bestHp || (monster.CurrentHp == bestHp && distance < bestDistance))
            {
                bestHp = monster.CurrentHp;
                bestDistance = distance;
                target = monster;
            }
        }

        return target;
    }

    private static bool IsValidTarget(MonsterController monster, Vector3 unitPos, float range, out float distance)
    {
        distance = float.MaxValue;

        if (monster == null || !monster.IsAlive)
            return false;

        distance = Vector3.Distance(unitPos, monster.transform.position);
        return distance <= range;
    }
}
