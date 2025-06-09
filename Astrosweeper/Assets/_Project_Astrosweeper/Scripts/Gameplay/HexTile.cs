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
}