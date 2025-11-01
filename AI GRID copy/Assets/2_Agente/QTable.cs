using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class QTable
{
    private Dictionary<string, float[]> table;
    private const int NUM_ACTIONS = 4;
    
    public QTable()
    {
        table = new Dictionary<string, float[]>();
    }
    
    public void Initialize(Coordenadas gridSize, List<Coordenadas> obstacles)
    {
        table.Clear();
        
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                Coordenadas pos = new Coordenadas(x, y);
                
                // Saltar obstáculos
                if (IsObstacle(pos, obstacles)) continue;
                
                // Estado sin llave
                string state0 = GetStateKey(pos, false);
                table[state0] = new float[NUM_ACTIONS];
                
                // Estado con llave
                string state1 = GetStateKey(pos, true);
                table[state1] = new float[NUM_ACTIONS];
            }
        }
        
        Debug.Log($"✅ Q-Table inicializada: {table.Count} estados");
    }
    
    public void UpdateQ(string state, int action, float reward, string nextState, float alpha, float gamma)
    {
        if (!table.ContainsKey(state) || !table.ContainsKey(nextState))
        {
            Debug.LogWarning($"⚠️ Estado no encontrado en Q-Table");
            return;
        }
        
        // Q(s,a) ← Q(s,a) + α[r + γ max Q(s',a') - Q(s,a)]
        float currentQ = table[state][action];
        float maxNextQ = table[nextState].Max();
        
        float newQ = currentQ + alpha * (reward + gamma * maxNextQ - currentQ);
        table[state][action] = newQ;
    }
    
    public float GetQValue(string state, int action)
    {
        if (!table.ContainsKey(state))
            return 0f;
        
        return table[state][action];
    }
    
    public float[] GetQValues(string state)
    {
        if (!table.ContainsKey(state))
            return new float[NUM_ACTIONS];
        
        return table[state];
    }
    
    public int GetBestAction(string state)
    {
        if (!table.ContainsKey(state))
            return Random.Range(0, NUM_ACTIONS);
        
        float[] qValues = table[state];
        float maxQ = qValues.Max();
        
        // En caso de empate, elegir aleatoriamente
        List<int> bestActions = new List<int>();
        for (int i = 0; i < NUM_ACTIONS; i++)
        {
            if (Mathf.Approximately(qValues[i], maxQ))
                bestActions.Add(i);
        }
        
        return bestActions[Random.Range(0, bestActions.Count)];
    }
    
    public bool ContainsState(string state)
    {
        return table.ContainsKey(state);
    }
    
    public static string GetStateKey(Coordenadas pos, bool hasKey)
    {
        return $"{pos.x},{pos.y},{(hasKey ? 1 : 0)}";
    }
    
    private bool IsObstacle(Coordenadas pos, List<Coordenadas> obstacles)
    {
        foreach (Coordenadas obs in obstacles)
        {
            if (CoordinateHelper.AreEqual(pos, obs))
                return true;
        }
        return false;
    }
}