using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ProspectingManager : MonoBehaviour
{
    public static ProspectingManager Instance { get; private set; }

    [Header("Grid Settings")]
    [SerializeField] private GameObject hexGridContainer;
    [SerializeField] private HexTile hexTilePrefab;
    [SerializeField] private int gridRadius = 5;
    [SerializeField, Range(0, 1)] private float trapDensity = 0.15f;
    [Tooltip("El radio alrededor de la tesela (0,0) que estará garantizado sin trampas.")]
    [SerializeField] private int safeZoneRadius = 1;

    [Header("System References")]
    [SerializeField] private HeatMapController heatMapController;

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
        hexGridContainer.SetActive(false);
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
        if (hexGridContainer != null)
        {
            hexGridContainer.SetActive(newState == GameState.Prospecting);
        }
    }

    void GenerateAndSetupGrid()
    {
        GenerateGrid();
        List<HexTile> safeZoneTiles = GetSafeZoneTiles();
        PlaceTraps(safeZoneTiles);
        CalculateDangerValues();
        RevealStartingArea();

        foreach (var tile in hexGrid.Values)
        {
            tile.UpdateVisuals();
        }
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

    private List<HexTile> GetSafeZoneTiles()
    {
        List<HexTile> safeTiles = new List<HexTile>();
        Vector3Int centerCube = Vector3Int.zero;

        foreach (HexTile tile in hexGrid.Values)
        {
            int q = tile.axialCoords.x;
            int r = tile.axialCoords.y;
            Vector3Int tileCube = new Vector3Int(q, r, -q - r);
            
            int distance = (Mathf.Abs(centerCube.x - tileCube.x) 
                          + Mathf.Abs(centerCube.y - tileCube.y) 
                          + Mathf.Abs(centerCube.z - tileCube.z)) / 2;
            
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
        
        List<HexTile> tilesToPlaceTraps = validTrapTiles.OrderBy(x => Random.value).Take(trapCount).ToList();
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