using UnityEngine;

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
