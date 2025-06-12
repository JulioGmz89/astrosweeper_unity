using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ProspectingManager : MonoBehaviour
{
    public static ProspectingManager Instance { get; private set; }

    public event Action<HexTile> OnSelectedTileChanged;
    public event Action<int, int> OnMineralQuotaUpdated; // int: current, int: total

    [Header("Grid Settings")]
    [SerializeField] private GameObject hexGridContainer;
    [SerializeField] private HexTile hexTilePrefab;
    [SerializeField] private int gridRadius = 5;
    [SerializeField, Range(0, 1)] private float trapDensity = 0.15f;
    [SerializeField] private int safeZoneRadius = 1;

    [Header("Mineral Settings")]
    [SerializeField] private int baseMineralQuota = 8;
    [SerializeField] private ClusterSizeRatio[] clusterSizeRatios = new ClusterSizeRatio[]
    {
        new ClusterSizeRatio(1, 0.6f),
        new ClusterSizeRatio(2, 0.3f),
        new ClusterSizeRatio(3, 0.1f)
    };

    [Header("System References")]
    [SerializeField] private HeatMapController heatMapController;

    // --- ¡AQUÍ ESTÁ LA PROPIEDAD QUE FALTABA! ---
    // Guardará una referencia a la tesela que el jugador tiene seleccionada en el modo de selección.
    public HexTile CurrentlySelectedTile { get; private set; }

    public int MineralsCollected { get; private set; } = 0;
    public int TotalMineralQuota { get; private set; } = 0;

    // --- Variables Privadas ---
    private Dictionary<Vector2Int, HexTile> hexGrid = new Dictionary<Vector2Int, HexTile>();
    private float hexOuterRadius = 1f;
    public void CollectMineral()
    {
        MineralsCollected++;
        Debug.Log($"Mineral collected! Total: {MineralsCollected}/{TotalMineralQuota}");
        OnMineralQuotaUpdated?.Invoke(MineralsCollected, TotalMineralQuota);

        // Future: Check for win condition here
    }

    private static readonly Vector2Int[] neighborDirections = {
        new Vector2Int(1, 0), new Vector2Int(1, -1), new Vector2Int(0, -1),
        new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1)
    };

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    void Start()
    {
        GameManager.OnGameStateChanged += HandleGameStateChange;
        GenerateAndSetupGrid();
        // El contenedor permanece activo, pero sus renderers se apagan.
        SetGridVisibility(false);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.OnGameStateChanged -= HandleGameStateChange;
        }
    }

    private void HandleGameStateChange(GameState newState)
    {
        // Al cambiar de estado, actualizamos la visibilidad de la cuadrícula.
        SetGridVisibility(newState == GameState.Prospecting || newState == GameState.TileSelection);
    }

    void GenerateAndSetupGrid()
    {
        TotalMineralQuota = baseMineralQuota;
        GenerateGrid();
        List<HexTile> safeZoneTiles = GetSafeZoneTiles();
        List<HexTile> trapTiles = PlaceTraps(safeZoneTiles);
        PlaceMinerals(safeZoneTiles, trapTiles);
        CalculateDangerValues();
        
        foreach (var tile in hexGrid.Values)
        {
            tile.UpdateVisuals();
        }

        RevealStartingArea();
    }
    
    public void RevealTile(HexTile tile)
    {
        if (tile == null || tile.isRevealed || tile.isFlagged) return;
        tile.Reveal();

        if (tile.isTrap)
        {
            Debug.LogError("¡BOOM! Has activado una trampa.");
            return;
        }
        if (tile.dangerValue == 0)
        {
            RevealNeighborsInCascade(tile);
        }
    }

    private void SetGridVisibility(bool isVisible)
    {
        if (isVisible)
        {
            foreach (var tile in hexGrid.Values)
            {
                tile.UpdateVisuals();
            }
        }
        else
        {
            foreach (var tile in hexGrid.Values)
            {
                tile.SetVisible(false);
            }
        }
    }

    /// <summary>
    /// Establece una nueva tesela como la seleccionada actualmente.
    /// </summary>
    public void SelectFirstTile()
    {
        if (hexGrid.Count > 0)
        {
            SetSelectedTile(hexGrid.Values.First());
        }
    }

    public void SetSelectedTile(HexTile newTile)
    {
        if (newTile == CurrentlySelectedTile) return;
        
        // TODO: Lógica para des-resaltar la tesela anterior (CurrentlySelectedTile)
        CurrentlySelectedTile = newTile;
        // TODO: Lógica para aplicar un resaltado a la nueva tesela seleccionada
        Debug.Log($"Nueva tesela seleccionada: {CurrentlySelectedTile.name}");

        // --- LÍNEA 2: HACER SONAR EL TIMBRE (INVOCAR EL EVENTO) ---
        // Aquí avisamos a todos los que estén escuchando (el inventario) que
        // el tile ha cambiado, y les pasamos el nuevo tile.
        OnSelectedTileChanged?.Invoke(newTile);
    }

    /// <summary>
    /// Obtiene el vecino de una tesela en una dirección del input, considerando la rotación de la cámara.
    /// </summary>
    public HexTile GetNeighborInDirection(HexTile originTile, Vector2 direction)
    {
        if (originTile == null || direction.sqrMagnitude < 0.1f) return null;

        Vector3 cameraForward = Camera.main.transform.forward;
        cameraForward.y = 0;
        cameraForward.Normalize();

        Vector3 inputWorldDirection = (cameraForward * direction.y + Camera.main.transform.right * direction.x).normalized;

        float maxDot = -1;
        Vector2Int bestDirection = Vector2Int.zero;

        foreach (Vector2Int hexDir in neighborDirections)
        {
            float x = hexOuterRadius * 1.5f * hexDir.x;
            float z = (hexOuterRadius * Mathf.Sqrt(3) / 2) * 2 * (hexDir.y + hexDir.x / 2f);
            Vector3 hexWorldDir = new Vector3(x, 0, z).normalized;

            float dot = Vector3.Dot(inputWorldDirection, hexWorldDir);
            if (dot > maxDot)
            {
                maxDot = dot;
                bestDirection = hexDir;
            }
        }
        
        Vector2Int neighborCoord = originTile.axialCoords + bestDirection;
        return hexGrid.TryGetValue(neighborCoord, out HexTile neighbor) ? neighbor : null;
    }
    
    // --- El resto de los métodos se queda igual ---
    private List<HexTile> GetSafeZoneTiles()
    {
        List<HexTile> safeTiles = new List<HexTile>();
        Vector3Int centerCube = Vector3Int.zero;
        foreach (HexTile tile in hexGrid.Values)
        {
            int q = tile.axialCoords.x;
            int r = tile.axialCoords.y;
            Vector3Int tileCube = new Vector3Int(q, r, -q - r);
            int distance = (Mathf.Abs(centerCube.x - tileCube.x) + Mathf.Abs(centerCube.y - tileCube.y) + Mathf.Abs(centerCube.z - tileCube.z)) / 2;
            if (distance <= safeZoneRadius)
            {
                safeTiles.Add(tile);
            }
        }
        return safeTiles;
    }
    
    private void RevealStartingArea()
    {
        if (hexGrid.TryGetValue(Vector2Int.zero, out HexTile centerTile))
        {
            RevealTile(centerTile);
        }
    }
    
    private List<HexTile> PlaceTraps(List<HexTile> forbiddenTiles)
    {
        List<HexTile> validTrapTiles = hexGrid.Values.Except(forbiddenTiles).ToList();

        int totalTrapCount = Mathf.FloorToInt(hexGrid.Count * trapDensity);

        int trapsToPlace = Mathf.Min(totalTrapCount, validTrapTiles.Count);

        System.Random rng = new System.Random();
        List<HexTile> trapLocations = validTrapTiles.OrderBy(x => rng.Next()).Take(trapsToPlace).ToList();

        foreach (HexTile tile in trapLocations)
        {
            tile.isTrap = true;
            // Opcional: Podrías añadir aquí la instanciación de un objeto visual para la trampa en el modo exploración.
            // Ejemplo: Instantiate(trapPrefab, tile.transform.position, Quaternion.identity);
        }

        Debug.Log($"Se colocaron {trapsToPlace} trampas en el tablero.");
        return trapLocations;
    }
    
    void GenerateGrid()
    {
        foreach (Transform child in hexGridContainer.transform) { Destroy(child.gameObject); }
        hexGrid.Clear();
        
        float hexInnerRadius = hexOuterRadius * Mathf.Sqrt(3) / 2;
        for (int q = -gridRadius; q <= gridRadius; q++)
        {
            int r1 = Mathf.Max(-gridRadius, -q - gridRadius);
            int r2 = Mathf.Min(gridRadius, -q + gridRadius);
            for (int r = r1; r <= r2; r++)
            {
                Vector2Int axialCoords = new Vector2Int(q, r);
                float x = hexOuterRadius * 1.5f * q;
                float z = hexInnerRadius * 2 * (r + q / 2f);
                Vector3 position = hexGridContainer.transform.position + new Vector3(x, 0, z);
                HexTile newTile = Instantiate(hexTilePrefab, position, Quaternion.identity, hexGridContainer.transform);
                newTile.Setup(axialCoords, heatMapController);
                hexGrid.Add(axialCoords, newTile);
            }
        }
    }

    void CalculateDangerValues()
    {
        foreach (HexTile tile in hexGrid.Values)
        {
            if (tile.isTrap) continue;
            int neighborTrapCount = 0;
            foreach (Vector2Int direction in neighborDirections)
            {
                if (hexGrid.TryGetValue(tile.axialCoords + direction, out HexTile neighbor) && neighbor.isTrap)
                {
                    neighborTrapCount++;
                }
            }
            tile.dangerValue = neighborTrapCount;
        }
    }
    
    public void RevealNeighborsInCascade(HexTile startTile)
    {
        Queue<HexTile> tilesToReveal = new Queue<HexTile>();
        tilesToReveal.Enqueue(startTile);
        HashSet<HexTile> processedTiles = new HashSet<HexTile> { startTile };
        while (tilesToReveal.Count > 0)
        {
            HexTile currentTile = tilesToReveal.Dequeue();
            foreach (Vector2Int direction in neighborDirections)
            {
                Vector2Int neighborCoords = currentTile.axialCoords + direction;
                if (hexGrid.TryGetValue(neighborCoords, out HexTile neighbor) && !processedTiles.Contains(neighbor))
                {
                    processedTiles.Add(neighbor);
                    if (!neighbor.isRevealed && !neighbor.isFlagged)
                    {
                        neighbor.Reveal();
                        if (neighbor.dangerValue == 0)
                        {
                            tilesToReveal.Enqueue(neighbor);
                        }
                    }
                }
            }
        }
    }

    private void PlaceMinerals(List<HexTile> safeZone, List<HexTile> traps)
    {
        List<HexTile> forbiddenTiles = new List<HexTile>(safeZone);
        forbiddenTiles.AddRange(traps);
        List<HexTile> availableTiles = hexGrid.Values.Except(forbiddenTiles).ToList();

        int mineralsPlaced = 0;
        while (mineralsPlaced < baseMineralQuota && availableTiles.Count > 0)
        {
            int clusterSize = GetRandomClusterSize();
            bool success = TryPlaceCluster(clusterSize, availableTiles);
            if (success)
            {
                mineralsPlaced += clusterSize;
            }
            else
            {
                // Could not find space for the cluster, try a smaller one or break.
                if (clusterSize > 1) continue; // Try again with a new random size
                else break; // If we can't even place a single mineral, stop.
            }
        }
        Debug.Log($"Placed {mineralsPlaced} mineral deposits.");
    }

    private bool TryPlaceCluster(int size, List<HexTile> availableTiles)
    {
        System.Random rng = new System.Random();
        List<HexTile> shuffledTiles = availableTiles.OrderBy(t => rng.Next()).ToList();

        foreach (HexTile startTile in shuffledTiles)
        {
            List<HexTile> cluster = new List<HexTile> { startTile };
            if (FindCluster(startTile, size, cluster, availableTiles))
            {
                foreach (HexTile tile in cluster)
                {
                    tile.SetMineralState(true);
                    availableTiles.Remove(tile);
                }
                return true;
            }
        }
        return false;
    }

    private bool FindCluster(HexTile currentTile, int remainingSize, List<HexTile> cluster, List<HexTile> available)
    {
        if (cluster.Count == remainingSize) return true;

        foreach (var dir in neighborDirections.OrderBy(d => Guid.NewGuid()))
        {
            if (hexGrid.TryGetValue(currentTile.axialCoords + dir, out HexTile neighbor) && 
                available.Contains(neighbor) && !cluster.Contains(neighbor))
            {
                cluster.Add(neighbor);
                if (FindCluster(neighbor, remainingSize, cluster, available)) return true;
                cluster.Remove(neighbor); // Backtrack
            }
        }
        return false;
    }

    private int GetRandomClusterSize()
    {
        float totalRatio = clusterSizeRatios.Sum(r => r.ratio);
        float randomValue = UnityEngine.Random.Range(0, totalRatio);
        float cumulativeRatio = 0f;

        foreach (var ratioInfo in clusterSizeRatios)
        {
            cumulativeRatio += ratioInfo.ratio;
            if (randomValue <= cumulativeRatio)
            {
                return ratioInfo.size;
            }
        }
        return clusterSizeRatios.Last().size; // Fallback
    }
}

[System.Serializable]
public struct ClusterSizeRatio
{
    public int size;
    [Range(0,1)]
    public float ratio;

    public ClusterSizeRatio(int size, float ratio)
    {
        this.size = size;
        this.ratio = ratio;
    }
}