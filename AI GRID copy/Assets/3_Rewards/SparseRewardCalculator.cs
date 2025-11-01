using UnityEngine;

public class SparseRewardCalculator : IRewardCalculator
{
    public float CalculateReward(Coordenadas current, Coordenadas previous, bool hasKey,
                                 Coordenadas keyPos, Coordenadas goalPos, int currentEpisode)
    {
        float reward = -0.01f;  // Living penalty
        
        // Recogió la llave
        if (!hasKey && CoordinateHelper.AreEqual(current, keyPos))
        {
            return 10f;
        }
        
        // Llegó a la meta CON llave
        if (hasKey && CoordinateHelper.AreEqual(current, goalPos))
        {
            return 100f;
        }
        
        // Llegó a la meta SIN llave
        if (!hasKey && CoordinateHelper.AreEqual(current, goalPos))
        {
            return -5f;
        }
        
        return reward;
    }
    
    public string GetModeName()
    {
        return "Sparse";
    }
}