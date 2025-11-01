using UnityEngine;

public class DistanceRewardCalculator : IRewardCalculator
{
    private float shapingMultiplier;
    
    public DistanceRewardCalculator(float shapingMultiplier = 0.5f)
    {
        this.shapingMultiplier = shapingMultiplier;
    }
    
    public float CalculateReward(Coordenadas current, Coordenadas previous, bool hasKey,
                                 Coordenadas keyPos, Coordenadas goalPos, int currentEpisode)
    {
        float reward = -0.01f;
        
        // Eventos principales
        if (!hasKey && CoordinateHelper.AreEqual(current, keyPos))
            return 10f;
        if (hasKey && CoordinateHelper.AreEqual(current, goalPos))
            return 100f;
        if (!hasKey && CoordinateHelper.AreEqual(current, goalPos))
            return -5f;
        
        // Shaping reward
        Coordenadas target = hasKey ? goalPos : keyPos;
        
        float currentDist = CoordinateHelper.ManhattanDistance(current, target);
        float previousDist = CoordinateHelper.ManhattanDistance(previous, target);
        
        float shapingReward = (previousDist - currentDist) * shapingMultiplier;
        reward += shapingReward;
        
        return reward;
    }
    
    public string GetModeName()
    {
        return "DistanceBased";
    }
}