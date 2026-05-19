using System;

[Serializable]
public class BountyEliteData
{
    public int difficulty;
    public double hp;
    public int rewardGold;
    public int rewardBattleMagicStone;
    public string displayName;

    public BountyEliteData(int difficulty, double hp, int rewardGold, int rewardBattleMagicStone, string displayName)
    {
        this.difficulty = difficulty;
        this.hp = hp;
        this.rewardGold = rewardGold;
        this.rewardBattleMagicStone = rewardBattleMagicStone;
        this.displayName = displayName;
    }
}
