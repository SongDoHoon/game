using UnityEngine;

[System.Serializable]
public class PassiveSkillData
{
    public string skillName;
    [TextArea] public string description;

    public BuffType passiveBuffType = BuffType.AttackPowerUp;
    public DebuffType passiveDebuffType = DebuffType.None;

    public bool useStack;
    public int maxStack;

    public float value1;
    public float value2;
    public float value3;

    public bool hasExecute;
    [Range(0f, 1f)] public float executeHpPercent = 0.05f;

    public bool affectsBossDifferently;
    public float bossMultiplier = 1f;
}

[System.Serializable]
public class ActiveSkillData
{
    public string skillName;
    [TextArea] public string description;

    public ActiveSkillCastType castType = ActiveSkillCastType.None;
    public TargetType targetType = TargetType.None;

    public float cooldown = 10f;
    public float duration = 3f;
    public float radius = 2f;

    public int hitCount = 1;
    public float interval = 0.5f;

    public float value1;
    public float value2;
    public float value3;

    public bool canStackDebuff;
    public int maxDebuffStack = 1;
}

[System.Serializable]
public class BuffInstance
{
    public BuffType buffType;
    public float value;
    public float duration;
    public float remainTime;
    public UnitController source;
    public bool isRuntime;
}

[System.Serializable]
public class DebuffInstance
{
    public DebuffType debuffType;
    public float value;
    public float duration;
    public float remainTime;
    public int stack = 1;
    public int maxStack = 1;
    public UnitController source;
}