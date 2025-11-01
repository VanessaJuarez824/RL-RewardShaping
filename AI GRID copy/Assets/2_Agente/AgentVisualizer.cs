using UnityEngine;

public class AgentVisualizer : MonoBehaviour
{
    [Header("Visuales")]
    public GameObject agentPrefab;
    public GameObject keyPrefab;
    public GameObject goalPrefab;
    
    private GameObject agentVisual;
    private GameObject keyVisual;
    private GameObject goalVisual;
    
    private GridManager gridManager;
    
    public void Initialize(GridManager grid, Coordenadas keyPos, Coordenadas goalPos)
    {
        gridManager = grid;
        
        // Crear agente
        if (agentPrefab != null)
            agentVisual = Instantiate(agentPrefab);
        else
            agentVisual = CreateDefaultAgent();
        
        // Crear llave
        if (keyPrefab != null)
            keyVisual = Instantiate(keyPrefab);
        else
            keyVisual = CreateDefaultKey();
        
        keyVisual.transform.position = grid.GetWorldPosition(keyPos) + Vector3.up * 0.5f;
        
        // Crear meta
        if (goalPrefab != null)
            goalVisual = Instantiate(goalPrefab);
        else
            goalVisual = CreateDefaultGoal();
        
        goalVisual.transform.position = grid.GetWorldPosition(goalPos) + Vector3.up * 0.4f;
    }
    
    public void UpdateAgentPosition(Coordenadas pos, bool hasKey)
    {
        if (agentVisual != null)
        {
            Vector3 worldPos = gridManager.GetWorldPosition(pos);
            agentVisual.transform.position = worldPos + Vector3.up * 0.5f;
            
            // Cambiar color si tiene la llave
            Renderer renderer = agentVisual.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = hasKey ? Color.cyan : Color.blue;
            }
        }
    }
    
    public void ShowKey(bool show)
    {
        if (keyVisual != null)
            keyVisual.SetActive(show);
    }
    
    public void Cleanup()
    {
        if (agentVisual != null) Destroy(agentVisual);
        if (keyVisual != null) Destroy(keyVisual);
        if (goalVisual != null) Destroy(goalVisual);
    }
    
    private GameObject CreateDefaultAgent()
    {
        GameObject agent = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        agent.name = "Agent";
        agent.transform.localScale = Vector3.one * 0.5f;
        agent.GetComponent<Renderer>().material.color = Color.blue;
        return agent;
    }
    
    private GameObject CreateDefaultKey()
    {
        GameObject key = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        key.name = "Key";
        key.transform.localScale = new Vector3(0.3f, 0.5f, 0.3f);
        key.GetComponent<Renderer>().material.color = Color.yellow;
        return key;
    }
    
    private GameObject CreateDefaultGoal()
    {
        GameObject goal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        goal.name = "Goal";
        goal.transform.localScale = Vector3.one * 0.8f;
        goal.GetComponent<Renderer>().material.color = Color.green;
        return goal;
    }
}