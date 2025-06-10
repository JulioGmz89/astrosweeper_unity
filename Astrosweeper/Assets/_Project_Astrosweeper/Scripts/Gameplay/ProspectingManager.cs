using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ProspectingManager : MonoBehaviour
{
    public static ProspectingManager Instance { get; private set; }

    public event Action<HexTile> OnSelectedTileChanged;

    [Header("Grid Settings")]
    [SerializeField] private GameObject hexGridContainer;
    [SerializeField] private HexTile hexTilePrefab;
    [SerializeField] private int gridRadius = 5;
    [SerializeField, Range(0, 1)] private float trapDensity = 0.15f;
    [SerializeField] private int safeZoneRadius = 1;

    [Header("System References")]
    [SerializeField] private HeatMapController heatMapController;

    // --- ¡AQUÍ ESTÁ LA PROPIEDAD QUE FALTABA! ---
    // Guardará una referencia a la tesela que el jugador tiene seleccionada en el modo de selección.
    public HexTile CurrentlySelectedTile { get; private set; }

    
    // --- Variables Privadas ---
    private Dictionary<Vector2Int, HexTile> hexGrid = new Dictionary<Vector2Int, HexTile>();
    private float hexOuterRadius = 1f;
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
        GenerateGrid();
        List<HexTile> safeZoneTiles = GetSafeZoneTiles();
        PlaceTraps(safeZoneTiles);
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
    public void SetSelectedTile(HexTile newTile)
    {
        if (newTile == null || newTile == CurrentlySelectedTile) return;
        
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
    
    private void PlaceTraps(List<HexTile> forbiddenTiles)
    {
        List<HexTile> validTrapTiles = hexGrid.Values.Except(forbiddenTiles).ToList();
        int trapCount = Mathf.FloorToInt(hexGrid.Count * trapDensity);
        
        List<HexTile> tilesToPlaceTraps = validTrapTiles.OrderBy(x => UnityEngine.Random.value).Take(trapCount).ToList();
        foreach (HexTile tile in tilesToPlaceTraps)
        {
            tile.isTrap = true;
        }
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
}