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

    public Vector2Int axialCoords;
    public bool isTrap = false;
    public bool isRevealed = false;
    public bool isFlagged = false;
    public int dangerValue = 0;

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
        // Si no estamos revelados, nos aseguramos de ser visibles con el color por defecto/marcado.
        if (!isRevealed)
        {
            meshRenderer.enabled = true;
            Color initialColor = isFlagged ? flaggedColor : defaultColor;
            propertyBlock.SetColor(colorPropertyName, initialColor);
            meshRenderer.SetPropertyBlock(propertyBlock);
            return;
        }

        // Si estamos revelados...
        if (isTrap)
        {
            meshRenderer.enabled = true;
            propertyBlock.SetColor(colorPropertyName, trapColor);
        }
        else if (dangerValue == 0)
        {
            // ¡La lógica clave! Si el valor es 0, el renderer se desactiva.
            meshRenderer.enabled = false;
            return; // Salimos para no aplicar ningún color.
        }
        else // dangerValue > 0
        {
            meshRenderer.enabled = true;
            Color dangerColor = heatMapController.GetColorForValue(dangerValue);
            propertyBlock.SetColor(colorPropertyName, dangerColor);
        }
        
        meshRenderer.SetPropertyBlock(propertyBlock);
    }
}