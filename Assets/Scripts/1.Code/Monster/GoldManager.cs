using UnityEngine;

public class GoldManager : MonoBehaviour
{
    public int currentGold = 300;

    public void AddGold(int amount)
    {
        currentGold += amount;
        Debug.Log($"골드 획득: +{amount} / 현재 골드: {currentGold}");
    }

    public bool UseGold(int amount)
    {
        if (currentGold < amount)
        {
            Debug.Log($"골드 부족! 필요: {amount}, 보유: {currentGold}");
            return false;
        }

        currentGold -= amount;
        Debug.Log($"골드 사용: -{amount} / 현재 골드: {currentGold}");
        return true;
    }
}
