using UnityEngine;

public class GoldManager : MonoBehaviour
{
    public int currentGold = 300;

    public void AddGold(int amount)
    {
        currentGold += amount;
    }

    public bool UseGold(int amount)
    {
        if (currentGold < amount)
            return false;

        currentGold -= amount;
        return true;
    }
}
