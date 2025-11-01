using UnityEngine;
using System;
using System.IO;               // Para manejar archivos Json
using System.Collections.Generic;
using UnityEngine.UI;          // Para Controlar Botón

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Grid")]
    [SerializeField] public Coordenadas gridSize;
    [field: SerializeField] public int TileSize { get; private set; }
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private Transform cameraTransform;

    [Header("Obstáculos / Edición")]
    [SerializeField] private bool editMode = true;
    [SerializeField] private Button editModeButton;
    [SerializeField] private Color activeColor = Color.red;
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private int obstacleCount = 30;

    // 👉 NUEVO: posiciones que NO se pueden bloquear
    [Header("Celdas protegidas (no poner obstáculos aquí)")]
    [SerializeField] private Coordenadas startCoord = new Coordenadas(0, 0);
    [SerializeField] private Coordenadas keyCoord = new Coordenadas(4, 2);
    [SerializeField] private Coordenadas goalCoord = new Coordenadas(4, 4);

    private List<Coordenadas> obstacles = new List<Coordenadas>();
    public bool EditMode => editMode;

    private string savePath;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        savePath = Path.Combine(Application.persistentDataPath, "obstacles.json");
        Debug.Log($"📂 Ruta del archivo de obstáculos: {savePath}");
    }

    private void Start()
    {
        ClearGrid();
        SpawnGrid();
        UpdateButtonColor();
    }

    [ContextMenu("Spawn Grid")]
    private void SpawnGrid()
    {
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                Vector3 tilePosition = new Vector3(x * TileSize, 0, y * TileSize);
                GameObject tile = Instantiate(tilePrefab, tilePosition, Quaternion.identity);
                tile.transform.parent = transform;
                tile.name = $"Tile {x}, {y}";

                Etiquetas etiquetas = tile.GetComponent<Etiquetas>();
                if (etiquetas != null)
                    etiquetas.SetCoordinates(x, y);

                Tile tileComponent = tile.AddComponent<Tile>();
                tileComponent.SetCoordinates(x, y);
            }
        }

        // Centrar cámara
        if (cameraTransform != null)
        {
            cameraTransform.position = new Vector3(
                (float)gridSize.x / 2 - 0.5f,
                gridSize.y,
                (float)gridSize.y / 2 - 0.5f
            );
        }
    }

    private void ClearGrid()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }

    // 👇 utilidad: saber si una celda está protegida
    private bool IsProtectedCell(Coordenadas coord)
    {
        return CoordinateHelper.AreEqual(coord, startCoord)
            || CoordinateHelper.AreEqual(coord, keyCoord)
            || CoordinateHelper.AreEqual(coord, goalCoord);
    }

    public void ToggleEditMode()
    {
        editMode = !editMode;
        Debug.Log(editMode ? "Modo Obstáculos Activado" : "Modo Obstáculos Desactivado");
        UpdateButtonColor();
    }

    public void ToggleEditModeUI()
    {
        ToggleEditMode();
    }

    // 👇 Agregar obstáculo (desde clic en el tile)
    public void AddObstacle(Coordenadas coord)
    {
        if (!editMode) return;

        // NO bloquear celdas protegidas
        if (IsProtectedCell(coord))
        {
            Debug.Log($"⛔ No se puede poner obstáculo en celda protegida: ({coord.x}, {coord.y})");
            return;
        }

        if (!obstacles.Contains(coord))
        {
            obstacles.Add(coord);
            Debug.Log($"🧱 Obstáculo añadido en: {coord.x}, {coord.y}");
        }
    }

    public List<Coordenadas> GetObstacles()
    {
        return obstacles;
    }

    private void UpdateButtonColor()
    {
        if (editModeButton != null)
        {
            ColorBlock colors = editModeButton.colors;
            colors.normalColor = editMode ? activeColor : defaultColor;
            colors.highlightedColor = colors.normalColor;
            colors.pressedColor = colors.normalColor;
            editModeButton.colors = colors;
        }
    }

    // 🔹 Guardar
    public void SaveObstacles()
    {
        ObstacleData data = new ObstacleData(obstacles);
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);
        Debug.Log($"✅ Obstáculos guardados en: {savePath}");
    }

    // 🔹 Cargar
    public void LoadObstacles()
    {
        if (!File.Exists(savePath))
        {
            Debug.LogWarning("⚠️ No se encontró una configuración previa de obstáculos.");
            return;
        }

        string json = File.ReadAllText(savePath);
        ObstacleData data = JsonUtility.FromJson<ObstacleData>(json);

        // limpiar antes
        ClearObstacles();   // 👈 así no se acumulan

        foreach (Coordenadas coord in data.obstacles)
        {
            // NO restaurar si es una celda protegida
            if (IsProtectedCell(coord))
                continue;

            obstacles.Add(coord);

            Tile tile = GetTileAt(coord);
            if (tile != null)
            {
                tile.ForceObstacle();
            }
            Debug.Log($"📌 Obstáculo cargado en: ({coord.x}, {coord.y})");
        }

        Debug.Log($"✅ Obstáculos cargados desde: {savePath}");
        Debug.Log($"✅ Total de obstáculos cargados: {obstacles.Count}");
    }

    // 🔹 Obtener un tile por coordenadas
    private Tile GetTileAt(Coordenadas coord)
    {
        foreach (Transform child in transform)
        {
            Tile tile = child.GetComponent<Tile>();
            if (tile != null && tile.CoordenadasEquals(coord))
            {
                return tile;
            }
        }
        return null;
    }

    public Vector3 GetWorldPosition(Coordenadas coordenadas)
    {
        return new Vector3(coordenadas.x * TileSize, 0, coordenadas.y * TileSize);
    }

    // 🔹 Generar obstáculos aleatorios (saltando start/key/goal)
    public void GenerateRandomObstacles()
    {
        if (!editMode) return;

        // primero limpiar
        ClearObstacles();

        int tries = 0;
        int placed = 0;

        // para evitar loops infinitos: máximo 5x el número buscado
        while (placed < obstacleCount && tries < obstacleCount * 5)
        {
            tries++;

            int x = UnityEngine.Random.Range(0, gridSize.x);
            int y = UnityEngine.Random.Range(0, gridSize.y);
            Coordenadas coord = new Coordenadas(x, y);

            if (IsProtectedCell(coord))           // 👈 no tocar start/key/goal
                continue;
            if (obstacles.Contains(coord))
                continue;

            obstacles.Add(coord);
            Tile tile = GetTileAt(coord);
            if (tile != null)
                tile.ForceObstacle();

            placed++;
        }

        SaveObstacles();
        Debug.Log($"✅ Obstáculos generados aleatoriamente y guardados. Total: {placed}");
    }

    // 👇 NUEVO: borrar TODOS los obstáculos del grid y de la lista
    public void ClearObstacles()
    {
        obstacles.Clear();

        foreach (Transform child in transform)
        {
            Tile tile = child.GetComponent<Tile>();
            if (tile != null)
            {
                // si el tile estaba como obstáculo, su propio script creó un cubito invisible
                // pero no tenemos referencia desde aquí, así que lo más fácil es:
                // - quitarle el tag
                // - regresarle el color
                // - destruir el hijo-collider si existe
                // Mejor: hacemos que el tile tenga un método que limpie, pero como no lo tienes:
                child.tag = "Untagged";

                Renderer r = child.GetComponent<Renderer>();
                if (r != null)
                    r.material.color = Color.white;

                // si el Tile creó un objeto extra como collider, estaba fuera de la jerarquía
                // así que solo con esto limpiamos la vista
            }
        }

        Debug.Log("🧹 Todos los obstáculos han sido eliminados.");
    }
}

[Serializable]
public class ObstacleData
{
    public List<Coordenadas> obstacles;

    public ObstacleData(List<Coordenadas> obstacles)
    {
        this.obstacles = obstacles;
    }
}
