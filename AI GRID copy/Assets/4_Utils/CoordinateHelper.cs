using UnityEngine;

public static class CoordinateHelper
{
    public static bool AreEqual(Coordenadas a, Coordenadas b)
    {
        return a.x == b.x && a.y == b.y;
    }
    
    public static float ManhattanDistance(Coordenadas a, Coordenadas b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
    
    public static bool IsValid(Coordenadas coord, Coordenadas gridSize)
    {
        return coord.x >= 0 && coord.x < gridSize.x &&
               coord.y >= 0 && coord.y < gridSize.y;
    }
    
    public static Coordenadas GetNewPosition(Coordenadas current, AgentAction action)
    {
        switch (action)
        {
            case AgentAction.Up:
                return new Coordenadas(current.x, current.y + 1);
            case AgentAction.Down:
                return new Coordenadas(current.x, current.y - 1);
            case AgentAction.Left:
                return new Coordenadas(current.x - 1, current.y);
            case AgentAction.Right:
                return new Coordenadas(current.x + 1, current.y);
            default:
                return current;
        }
    }
}

public enum AgentAction { Up = 0, Down = 1, Left = 2, Right = 3 }