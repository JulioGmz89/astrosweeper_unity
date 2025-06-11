using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class HexTile : MonoBehaviour
{
    [Header("Shader Settings")]
    [SerializeField] private string colorPropertyName = "_BaseColor";

    [Header("Visuals")]
    [SerializeField] private Color defaultColor = Color.gray;
    [SerializeField] private Color flaggedColor = Color.cyan;
    [SerializeField] private Color trapColor = Color.red;

    // Propiedades de la tesela
    public Vector2Int axialCoords;
    public bool isTrap = false;
    public bool isRevealed = false;
    public bool isFlagged = false;
    public int dangerValue = 0;

    // Componentes y referencias
    private MeshRenderer meshRenderer;
    private HeatMapController heatMapController;
    private MaterialPropertyBlock propertyBlock;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        propertyBlock = new MaterialPropertyBlock();
    }

    public void Setup(Vector2Int coords, HeatMapController controller)
    {
        this.axialCoords = coords;
        this.heatMapController = controller;
        transform.name = $"Hex_{coords.x}_{coords.y}";
    }

    public void Reveal()
    {
        if (isRevealed || isFlagged) return;
        isRevealed = true;
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        if (!isRevealed)
        {
            SetVisible(true);
            Color initialColor = isFlagged ? flaggedColor : defaultColor;
            ApplyColor(initialColor);
            return;
        }

        if (isTrap)
        {
            SetVisible(true);
            ApplyColor(trapColor);
        }
        else if (dangerValue == 0)
        {
            SetVisible(false);
        }
        else // dangerValue > 0
        {
            SetVisible(true);
            Color dangerColor = heatMapController.GetColorForValue(dangerValue);
            ApplyColor(dangerColor);
        }
    }

    /// <summary>
    /// Aplica un color específico a la tesela usando un MaterialPropertyBlock.
    /// </summary>
    private void ApplyColor(Color color)
    {
        meshRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(colorPropertyName, color);
        meshRenderer.SetPropertyBlock(propertyBlock);
    }

    /// <summary>
    /// Controla la visibilidad de esta tesela específica habilitando/deshabilitando su MeshRenderer.
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (meshRenderer != null)
        {
            meshRenderer.enabled = visible;
        }
    }

    // --- LÓGICA DE INTERACCIÓN CON OBJETOS ---
    private GameObject placedItemInstance = null;
    private GameObject flagInstance = null; // Referencia para la bandera
    public bool IsOccupied => placedItemInstance != null;
    public bool HasFlag => flagInstance != null;

    public void PlaceItem(GameObject itemPrefab)
    {
        if (IsOccupied) 
        {
            Debug.LogWarning($"Tile {name} is already occupied.");
            return;
        }

        if (itemPrefab != null)
        {
            // Instanciamos el objeto en el centro de la casilla, con un pequeño offset vertical.
            float yOffset = 0.5f; // Ajusta este valor según sea necesario.
            Vector3 position = transform.position + new Vector3(0, yOffset, 0);
            
            placedItemInstance = Instantiate(itemPrefab, position, Quaternion.identity, transform);
        }
    }

    public void PlaceFlag(GameObject flagPrefab)
    {
        if (HasFlag)
        {
            Debug.LogWarning($"Tile {name} already has a flag.");
            return;
        }

        if (flagPrefab != null)
        {
            float yOffset = 0.5f; // Ajusta este valor según sea necesario.
            Vector3 position = transform.position + new Vector3(0, yOffset, 0);
            flagInstance = Instantiate(flagPrefab, position, Quaternion.identity, transform);
        }
    }

    public void SetFlagVisible(bool visible)
    {
        if (flagInstance != null)
        {
            flagInstance.SetActive(visible);
        }
    }
}