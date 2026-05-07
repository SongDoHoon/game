using UnityEngine;

public static class MonsterEffectHandler
{
    public static void ApplyDebuff(UnitController attacker, MonsterController target, DebuffType debuffType, float value, float duration, int maxStack = 1)
    {
        if (attacker == null || target == null) return;

        target.AddDebuff(new DebuffInstance
        {
            debuffType = debuffType,
            value = value,
            duration = duration,
            remainTime = duration,
            stack = 1,
            maxStack = Mathf.Max(1, maxStack),
            source = attacker
        });
    }
}
