using UnityEngine;

public class DecayingRewardCalculator : IRewardCalculator
{
    private float lambda;
    private float shapingMultiplier;
    
    public DecayingRewardCalculator(float lambda = 500f, float shapingMultiplier = 0.5f)
    {
        this.lambda = lambda;
        this.shapingMultiplier = shapingMultiplier;
    }
    
    public float CalculateReward(Coordenadas current, Coordenadas previous, bool hasKey,
                                 Coordenadas keyPos, Coordenadas goalPos, int currentEpisode)
    {
        float reward = -0.01f;
        
        // Eventos principales (sin decay)
        if (!hasKey && CoordinateHelper.AreEqual(current, keyPos))
            return 10f;
        if (hasKey && CoordinateHelper.AreEqual(current, goalPos))
            return 100f;
        if (!hasKey && CoordinateHelper.AreEqual(current, goalPos))
            return -5f;
        
        // Factor de decay: λ / (λ + n)
        float decayFactor = lambda / (lambda + currentEpisode);
        
        // Shaping reward con decay
        Coordenadas target = hasKey ? goalPos : keyPos;
        
        float currentDist = CoordinateHelper.ManhattanDistance(current, target);
        float previousDist = CoordinateHelper.ManhattanDistance(previous, target);
        
        float shapingReward = (previousDist - currentDist) * shapingMultiplier * decayFactor;
        reward += shapingReward;
        
        return reward;
    }
    
    public string GetModeName()
    {
        return "Decaying";
    }
}