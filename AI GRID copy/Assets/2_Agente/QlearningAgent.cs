using UnityEngine;
using UnityEngine.UI;  // para poder referenciar el botón
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;
using System.Text;

public class QLearningAgent : MonoBehaviour
{
    [Header("Referencias")]
    public GridManager gridManager;
    public AgentVisualizer visualizer;
    [Tooltip("Opcional: botón de la UI que dispara la simulación")]
    public Button runButton;

    [Header("Parámetros Q-Learning")]
    [Range(0f, 1f)] public float learningRate = 0.1f;
    [Range(0f, 1f)] public float discountFactor = 0.99f;
    [Range(0f, 1f)] public float epsilon = 0.1f;
    [Range(0.9f, 0.999f)] public float epsilonDecay = 0.995f;
    [Range(0.01f, 0.1f)] public float minEpsilon = 0.01f;

    [Header("Configuración")]
    public RewardMode rewardMode = RewardMode.Sparse;
    [Range(100f, 1000f)] public float lambda = 500f;
    [Range(0.1f, 2f)] public float shapingMultiplier = 0.5f;

    [Header("Entrenamiento")]
    public int maxEpisodes = 1000;
    public int maxStepsPerEpisode = 100;
    [Range(0f, 1f)] public float visualizationSpeed = 0.1f;
    public bool showTraining = true;

    [Header("Posiciones")]
    public Coordenadas startCoord = new Coordenadas(0, 0);
    public Coordenadas keyCoord = new Coordenadas(4, 2);
    public Coordenadas goalCoord = new Coordenadas(4, 4);

    // 👇 NUEVO: opciones anti-bucles / anti-atascos
    [Header("Anti-Stuck / Anti-Loop")]
    [Tooltip("Penaliza quedarse en el mismo lugar o hacer back-and-forth")]
    public bool enableBacktrackPenalty = true;
    public float backtrackPenalty = 0.05f;

    [Tooltip("Penaliza visitar de nuevo una casilla muy reciente")]
    public bool enableLoopPenalty = true;
    [Range(2, 8)] public int loopWindow = 4;
    public float loopPenalty = 0.03f;

    [Tooltip("Si true, castiga cuando el 'acercarse' no tiene camino real (BFS)")]
    public bool enablePathAwareShaping = true;
    public float pathBlockedPenalty = 0.08f;

    // Componentes internos
    private QTable qTable;
    private IRewardCalculator rewardCalculator;

    // Estado
    private Coordenadas currentPosition;
    private Coordenadas previousPosition;
    private bool hasKey;
    private int currentEpisode;
    private float currentEpisodeReward;
    private int currentSteps;

    // 👇 para detectar loops
    private Queue<Coordenadas> recentPositions = new Queue<Coordenadas>();

    // Métricas
    private List<EpisodeData> episodeHistory = new List<EpisodeData>();
    private int episodesUntilFirstSuccess = -1;

    // Control
    private bool isTraining = false;  // para no duplicar entrenamientos

    public enum RewardMode { Sparse, DistanceBased, Decaying }

    [Serializable]
    public class EpisodeData
    {
        public int episode;
        public float reward;
        public int steps;
        public float epsilon;
        public bool success;
    }

    void Start()
    {
        if (!ValidateSetup()) return;
        InitializeComponents();
    }

    // ESTE es el método que vas a llamar desde el botón
    public void RunSimulation()
    {
        if (isTraining) return;

        // limpiar visualización anterior 
        if (visualizer != null)
            visualizer.Cleanup();

        // reset métricas
        currentEpisode = 0;
        episodeHistory.Clear();
        episodesUntilFirstSuccess = -1;

        // re-inicializar todo (esto vuelve a crear el agent visual)
        InitializeComponents();

        isTraining = true;
        if (runButton != null)
            runButton.interactable = false;

        if (showTraining)
            StartCoroutine(TrainingWithVisualization());
        else
            StartCoroutine(TrainingFast());
    }

    bool ValidateSetup()
    {
        if (gridManager == null)
        {
            Debug.LogError("❌ GridManager no asignado!");
            return false;
        }

        if (!CoordinateHelper.IsValid(startCoord, gridManager.gridSize) ||
            !CoordinateHelper.IsValid(keyCoord, gridManager.gridSize) ||
            !CoordinateHelper.IsValid(goalCoord, gridManager.gridSize))
        {
            Debug.LogError("❌ Coordenadas fuera del grid!");
            return false;
        }

        return true;
    }

    void InitializeComponents()
    {
        // Inicializar Q-Table
        qTable = new QTable();
        qTable.Initialize(gridManager.gridSize, gridManager.GetObstacles());

        // Inicializar calculador de rewards
        switch (rewardMode)
        {
            case RewardMode.Sparse:
                rewardCalculator = new SparseRewardCalculator();
                break;
            case RewardMode.DistanceBased:
                rewardCalculator = new DistanceRewardCalculator(shapingMultiplier);
                break;
            case RewardMode.Decaying:
                rewardCalculator = new DecayingRewardCalculator(lambda, shapingMultiplier);
                break;
        }

        // Inicializar visualización
        if (showTraining)
        {
            if (visualizer == null)
                visualizer = gameObject.AddComponent<AgentVisualizer>();

            visualizer.Initialize(gridManager, keyCoord, goalCoord);
        }

        // limpiar buffer de posiciones
        recentPositions.Clear();

        Debug.Log($"🎓 Agente listo - Modo: {rewardCalculator.GetModeName()}");
        Debug.Log($"📁 Resultados se guardarán en: {Application.persistentDataPath}");
    }

    System.Collections.IEnumerator TrainingWithVisualization()
    {
        for (currentEpisode = 0; currentEpisode < maxEpisodes; currentEpisode++)
        {
            ResetEpisode();

            while (!IsEpisodeFinished())
            {
                RunStep();

                if (showTraining && visualizer != null)
                    visualizer.UpdateAgentPosition(currentPosition, hasKey);

                if (visualizationSpeed > 0)
                    yield return new WaitForSeconds(visualizationSpeed);
                else
                    yield return null;
            }

            CompleteEpisode();

            if (currentEpisode % 50 == 0)
                LogProgress();
        }

        FinishTraining();
    }

    System.Collections.IEnumerator TrainingFast()
    {
        for (currentEpisode = 0; currentEpisode < maxEpisodes; currentEpisode++)
        {
            ResetEpisode();

            while (!IsEpisodeFinished())
            {
                RunStep();
            }

            CompleteEpisode();

            if (currentEpisode % 50 == 0)
            {
                LogProgress();
                yield return null;
            }
        }

        FinishTraining();
    }

    void RunStep()
    {
        // 1. Observar estado
        string currentState = QTable.GetStateKey(currentPosition, hasKey);

        // 2. Seleccionar acción
        int action = SelectAction(currentState);

        // 3. Ejecutar acción
        previousPosition = currentPosition;
        ExecuteAction(action);

        // 4. Calcular reward base (la del modo seleccionado)
        float reward = rewardCalculator.CalculateReward(
            currentPosition, previousPosition, hasKey,
            keyCoord, goalCoord, currentEpisode
        );

        // 👇 ANTI-STUCK 1: penalizar quedarse donde mismo o back-and-forth
        if (enableBacktrackPenalty)
        {
            // si después de ejecutar la acción sigo donde mismo → penaliza
            if (CoordinateHelper.AreEqual(currentPosition, previousPosition))
            {
                reward -= backtrackPenalty;
            }
        }

        // 👇 ANTI-STUCK 2: penalizar repetir casillas recientes
        if (enableLoopPenalty)
        {
            if (IsInRecentPositions(currentPosition))
            {
                reward -= loopPenalty;
            }
            RegisterPosition(currentPosition);
        }

        // 👇 ANTI-STUCK 3: path-aware (solo tiene sentido en DistanceBased / Decaying)
        if (enablePathAwareShaping && (rewardMode == RewardMode.DistanceBased || rewardMode == RewardMode.Decaying))
        {
            Coordenadas target = hasKey ? goalCoord : keyCoord;

            int prevDistBfs = GetShortestPathSteps(previousPosition, target);
            int currDistBfs = GetShortestPathSteps(currentPosition, target);

            // caso 1: antes había camino y ahora NO → te estás metiendo en un hoyo
            if (prevDistBfs != -1 && currDistBfs == -1)
            {
                reward -= pathBlockedPenalty;
            }
            // caso 2: sí hay camino pero te alejaste en términos de camino real
            else if (prevDistBfs != -1 && currDistBfs != -1 && currDistBfs > prevDistBfs)
            {
                reward -= pathBlockedPenalty * 0.5f;
            }
        }

        // 5. Acumular
        currentEpisodeReward += reward;

        // 6. Actualizar Q-Table
        string nextState = QTable.GetStateKey(currentPosition, hasKey);
        qTable.UpdateQ(currentState, action, reward, nextState, learningRate, discountFactor);

        currentSteps++;
    }

    int SelectAction(string state)
    {
        // Epsilon-greedy
        if (UnityEngine.Random.value < epsilon)
        {
            // Exploración: acción aleatoria válida
            List<int> validActions = GetValidActions();
            return validActions[UnityEngine.Random.Range(0, validActions.Count)];
        }
        else
        {
            // Explotación: mejor acción
            return qTable.GetBestAction(state);
        }
    }

    void ExecuteAction(int action)
    {
        Coordenadas newPos = CoordinateHelper.GetNewPosition(currentPosition, (AgentAction)action);

        if (IsValidMove(newPos))
        {
            currentPosition = newPos;

            // Verificar si recogió la llave
            if (!hasKey && CoordinateHelper.AreEqual(currentPosition, keyCoord))
            {
                hasKey = true;
                if (showTraining && visualizer != null)
                    visualizer.ShowKey(false);
            }
        }
    }

    List<int> GetValidActions()
    {
        List<int> validActions = new List<int>();

        for (int i = 0; i < 4; i++)
        {
            Coordenadas newPos = CoordinateHelper.GetNewPosition(currentPosition, (AgentAction)i);
            if (IsValidMove(newPos))
                validActions.Add(i);
        }

        if (validActions.Count == 0)
        {
            // Caso extremo: devolver todas
            for (int i = 0; i < 4; i++)
                validActions.Add(i);
        }

        return validActions;
    }

    bool IsValidMove(Coordenadas pos)
    {
        if (!CoordinateHelper.IsValid(pos, gridManager.gridSize))
            return false;

        List<Coordenadas> obstacles = gridManager.GetObstacles();
        foreach (Coordenadas obs in obstacles)
        {
            if (CoordinateHelper.AreEqual(pos, obs))
                return false;
        }

        return true;
    }

    void ResetEpisode()
    {
        currentPosition = startCoord;
        previousPosition = startCoord;
        hasKey = false;
        currentSteps = 0;
        currentEpisodeReward = 0f;

        // limpiar historial de posiciones
        recentPositions.Clear();
        RegisterPosition(currentPosition);

        if (showTraining && visualizer != null)
        {
            visualizer.ShowKey(true);
            visualizer.UpdateAgentPosition(currentPosition, hasKey);
        }
    }

    bool IsEpisodeFinished()
    {
        // Terminó si llegó a la meta o excedió pasos máximos
        return CoordinateHelper.AreEqual(currentPosition, goalCoord) ||
               currentSteps >= maxStepsPerEpisode;
    }

    void CompleteEpisode()
    {
        bool success = hasKey && CoordinateHelper.AreEqual(currentPosition, goalCoord);

        // Guardar métricas
        episodeHistory.Add(new EpisodeData
        {
            episode = currentEpisode,
            reward = currentEpisodeReward,
            steps = currentSteps,
            epsilon = epsilon,
            success = success
        });

        // Primera victoria
        if (success && episodesUntilFirstSuccess == -1)
        {
            episodesUntilFirstSuccess = currentEpisode;
            Debug.Log($"🎉 ¡Primera victoria en episodio {currentEpisode}!");
        }

        // Decay epsilon
        epsilon = Mathf.Max(minEpsilon, epsilon * epsilonDecay);
    }

    void LogProgress()
    {
        int startIdx = Mathf.Max(0, currentEpisode - 49);
        var recent = episodeHistory.Skip(startIdx).Take(50).ToList();

        if (recent.Count == 0) return;

        float avgReward = (float)recent.Average(e => e.reward);
        float avgSteps = (float)recent.Average(e => e.steps);
        int successCount = recent.Count(e => e.success);
        float successRate = (successCount / (float)recent.Count) * 100f;

        Debug.Log($"📊 Ep {currentEpisode}/{maxEpisodes} | " +
                  $"Reward: {avgReward:F2} | " +
                  $"Steps: {avgSteps:F1} | " +
                  $"Success: {successRate:F1}% | " +
                  $"ε: {epsilon:F3}");
    }

    void FinishTraining()
    {
        Debug.Log("✅ ¡Entrenamiento completado!");
        ExportResults();
        PrintSummary();

        isTraining = false; // ya no está corriendo

        // reactivar botón
        if (runButton != null)
            runButton.interactable = true;
    }

    void ExportResults()
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string modeName = rewardCalculator.GetModeName();
        string filename = $"Training_{modeName}_{timestamp}.csv";
        string path = Path.Combine(Application.persistentDataPath, filename);

        StringBuilder csv = new StringBuilder();
        csv.AppendLine("Episode,Reward,Steps,Epsilon,Success");

        foreach (var data in episodeHistory)
        {
            csv.AppendLine($"{data.episode},{data.reward},{data.steps},{data.epsilon},{(data.success ? 1 : 0)}");
        }

        File.WriteAllText(path, csv.ToString());

        Debug.Log($"📁 CSV exportado: {path}");

        // Exportar resumen también
        ExportSummary(path.Replace(".csv", "_summary.txt"), modeName);
    }

    void ExportSummary(string path, string modeName)
    {
        var last100 = episodeHistory.Skip(Mathf.Max(0, episodeHistory.Count - 100)).ToList();

        if (last100.Count == 0) return;

        float avgReward = (float)last100.Average(e => e.reward);
        float avgSteps = (float)last100.Average(e => e.steps);
        int successCount = last100.Count(e => e.success);
        float successRate = (successCount / (float)last100.Count) * 100f;

        // Calcular desviación estándar en float
        float sumSq = 0f;
        foreach (var e in last100)
        {
            float diff = e.reward - avgReward;
            sumSq += diff * diff;
        }
        float variance = sumSq / last100.Count;
        float stdDev = Mathf.Sqrt(variance);

        string summary = "=== RESUMEN DEL ENTRENAMIENTO ===\n\n";
        summary += $"Modo: {modeName}\n\n";
        summary += "Parámetros:\n";
        summary += $"  - Learning Rate (α): {learningRate}\n";
        summary += $"  - Discount Factor (γ): {discountFactor}\n";
        summary += $"  - Lambda (λ): {lambda}\n";
        summary += $"  - Shaping Multiplier: {shapingMultiplier}\n\n";
        summary += "Resultados:\n";
        summary += $"  - Episodios totales: {episodeHistory.Count}\n";
        summary += $"  - Primera victoria: Episodio {episodesUntilFirstSuccess}\n";
        summary += $"  - Recompensa promedio (últimos 100): {avgReward:F2} ± {stdDev:F2}\n";
        summary += $"  - Pasos promedio (últimos 100): {avgSteps:F2}\n";
        summary += $"  - Tasa de éxito (últimos 100): {successRate:F1}%\n";

        File.WriteAllText(path, summary);
    }

    void PrintSummary()
    {
        var last100 = episodeHistory.Skip(Mathf.Max(0, episodeHistory.Count - 100)).ToList();

        if (last100.Count == 0) return;

        float avgReward = (float)last100.Average(e => e.reward);
        float avgSteps = (float)last100.Average(e => e.steps);
        int successCount = last100.Count(e => e.success);
        float successRate = (successCount / (float)last100.Count) * 100f;

        string separator = new string('=', 50);

        Debug.Log("\n" + separator);
        Debug.Log($"🎯 RESUMEN FINAL - {rewardCalculator.GetModeName()}");
        Debug.Log(separator);
        Debug.Log($"📊 Primera victoria: Episodio {episodesUntilFirstSuccess}");
        Debug.Log($"📊 Recompensa promedio (últimos 100): {avgReward:F2}");
        Debug.Log($"📊 Pasos promedio (últimos 100): {avgSteps:F2}");
        Debug.Log($"📊 Tasa de éxito (últimos 100): {successRate:F1}%");
        Debug.Log(separator + "\n");
    }

    // =========================
    //    HELPERS ANTI-LOOP
    // =========================

    private void RegisterPosition(Coordenadas pos)
    {
        recentPositions.Enqueue(pos);
        while (recentPositions.Count > loopWindow)
            recentPositions.Dequeue();
    }

    private bool IsInRecentPositions(Coordenadas pos)
    {
        foreach (var p in recentPositions)
        {
            if (CoordinateHelper.AreEqual(p, pos))
                return true;
        }
        return false;
    }

    // =========================
    //   BFS PARA PATH-AWARE
    // =========================
    // devuelve número de pasos del camino más corto o -1 si no hay
    private int GetShortestPathSteps(Coordenadas start, Coordenadas target)
    {
        if (CoordinateHelper.AreEqual(start, target))
            return 0;

        int width = gridManager.gridSize.x;
        int height = gridManager.gridSize.y;

        bool[,] visited = new bool[width, height];
        Queue<(Coordenadas c, int d)> q = new Queue<(Coordenadas c, int d)>();

        visited[start.x, start.y] = true;
        q.Enqueue((start, 0));

        List<Coordenadas> obstacles = gridManager.GetObstacles();

        while (q.Count > 0)
        {
            var (cur, dist) = q.Dequeue();

            // 4 direcciones
            for (int i = 0; i < 4; i++)
            {
                Coordenadas next = CoordinateHelper.GetNewPosition(cur, (AgentAction)i);

                if (!CoordinateHelper.IsValid(next, gridManager.gridSize))
                    continue;

                // si es obstáculo, no pasa
                bool isObs = false;
                foreach (var o in obstacles)
                {
                    if (CoordinateHelper.AreEqual(o, next))
                    {
                        isObs = true;
                        break;
                    }
                }
                if (isObs) continue;

                if (!visited[next.x, next.y])
                {
                    if (CoordinateHelper.AreEqual(next, target))
                        return dist + 1;

                    visited[next.x, next.y] = true;
                    q.Enqueue((next, dist + 1));
                }
            }
        }

        // no found
        return -1;
    }
}
