using UnityEngine;

// Este script se coloca en cada prefab de tesela hexagonal.
public class HeatMapController : MonoBehaviour
{
    // Asigna el renderer de la tesela en el Inspector.
    [SerializeField]
    private Renderer tileRenderer;

    // Guardamos la referencia al material para eficiencia.
    private MaterialPropertyBlock propBlock;
    
    // El nombre de la propiedad en el Shader Graph. Es crucial que coincida.
    private static readonly int ColorPropertyID = Shader.PropertyToID("_BaseColor");

    // Colores para el mapa de calor, puedes definirlos aquí o en un ScriptableObject.
    [Header("Heatmap Colors")]
    [SerializeField] private Color colorSafe = Color.cyan;
    [SerializeField] private Color color1Trap = Color.blue;
    [SerializeField] private Color color2Traps = Color.green;
    [SerializeField] private Color color3Traps = Color.yellow;
    [SerializeField] private Color color4Traps = new Color(1.0f, 0.5f, 0.0f); // Naranja
    [SerializeField] private Color color5Traps = Color.red;
    [SerializeField] private Color color6Traps = Color.magenta;

    void Awake()
    {
        propBlock = new MaterialPropertyBlock();
        // Inicializa la tesela con su color por defecto (seguro o apagado)
        SetState(0); 
    }

    /// <summary>
    /// Actualiza el color de la tesela basado en el número de trampas adyacentes.
    /// Esta función será llamada por el GameManager cuando se revele la tesela.
    /// </summary>
    /// <param name="adjacentTraps">El número de trampas adyacentes, según el GDD.</param>
    public void SetState(int adjacentTraps)
    {
        // Obtiene el renderer actual
        tileRenderer.GetPropertyBlock(propBlock);

        // Selecciona el color según la especificación del GDD 
        Color targetColor;
        switch (adjacentTraps)
        {
            case 0:
                targetColor = colorSafe;
                break;
            case 1:
                targetColor = color1Trap;
                break;
            case 2:
                targetColor = color2Traps;
                break;
            case 3:
                targetColor = color3Traps;
                break;
            case 4:
                targetColor = color4Traps;
                break;
            case 5:
                targetColor = color5Traps;
                break;
            case 6:
                targetColor = color6Traps;
                break;
            default:
                targetColor = colorSafe; // Color por defecto en caso de error
                break;
        }

        // Asigna el color a la propiedad del shader.
        propBlock.SetColor(ColorPropertyID, targetColor);
        tileRenderer.SetPropertyBlock(propBlock);
    }
}