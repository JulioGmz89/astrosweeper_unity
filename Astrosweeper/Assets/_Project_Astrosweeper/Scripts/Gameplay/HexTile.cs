using UnityEngine;

public class HexTile : MonoBehaviour
{
    [Header("Shader Settings")]
    [SerializeField] private string colorPropertyName = "_BaseColor";

    [Header("Visuals")]
    // --- ¡AQUÍ ESTÁN LOS CAMBIOS! ---
    [ColorUsage(true, true)] [SerializeField] private Color defaultColor = Color.gray;
    [ColorUsage(true, true)] [SerializeField] private Color flaggedColor = Color.cyan;
    [ColorUsage(true, true)] [SerializeField] private Color trapColor = Color.red;


    [Header("Resource/Item Prefabs")]
    [SerializeField] private GameObject mineralPrefab;
    [SerializeField] private GameObject disarmedTrapPrefab;
    [SerializeField] private GameObject mineralCollectiblePrefab;

    [Header("Animation Settings")]
    [SerializeField] private string scaleControllerName = "HexTile_Scale_Controller";
    [SerializeField] private string innerAnimatorName = "Hextile_Inner_01";
    [SerializeField] private string outerAnimatorName = "Hextile_Outer_01";
    [SerializeField] private string flagAnimatorName = "Hextile_Flag_01";

    // Propiedades de la tesela
    public Vector2Int axialCoords;
    public bool isTrap = false;
    public bool isRevealed = false;
    public bool isFlagged { get; private set; } = false;
    public bool hasMineral { get; private set; } = false;
    public bool hasDisarmedTrap { get; private set; } = false;
    public int dangerValue = 0;
    public float selectedIntensity = 1.55f;

    // Componentes y referencias
    private MeshRenderer[] meshRenderers;
    private Animator innerAnimator;
    private Animator outerAnimator;
    private Animator flagAnimator;
    private GameObject mineralInstance;
    private GameObject disarmedTrapInstance;
    private HeatMapController heatMapController;
    private MaterialPropertyBlock propertyBlock;

    void Awake()
    {
        Transform scaleController = transform.Find(scaleControllerName);
        if (scaleController != null)
        {
            meshRenderers = scaleController.GetComponentsInChildren<MeshRenderer>();

            Transform innerChild = scaleController.Find(innerAnimatorName);
            if (innerChild) innerAnimator = innerChild.GetComponent<Animator>();
            else Debug.LogWarning($"Animator child '{innerAnimatorName}' not found on {gameObject.name}", this);

            Transform outerChild = scaleController.Find(outerAnimatorName);
            if (outerChild) outerAnimator = outerChild.GetComponent<Animator>();
            else Debug.LogWarning($"Animator child '{outerAnimatorName}' not found on {gameObject.name}", this);

            Transform flagChild = scaleController.Find(flagAnimatorName);
            if (flagChild) flagAnimator = flagChild.GetComponent<Animator>();
            else Debug.LogWarning($"Animator child '{flagAnimatorName}' not found on {gameObject.name}", this);
        }
        else
        {
            Debug.LogError($"Child object '{scaleControllerName}' not found on {gameObject.name}. Visuals and animations will not work.", this);
            meshRenderers = new MeshRenderer[0];
        }
        
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
        if (isRevealed && dangerValue == 0 && !isTrap)
        {
            SetVisible(false);
        }
        else
        {
            SetVisible(true);
            ApplyColor(GetColorForCurrentState());
        }
    }

    private void ApplyColor(Color color)
    {
        if (meshRenderers == null || meshRenderers.Length == 0) return;
        meshRenderers[0].GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(colorPropertyName, color);
        foreach (var renderer in meshRenderers)
        {
            renderer.SetPropertyBlock(propertyBlock);
        }
    }

    public void SetVisible(bool visible)
    {
        if (meshRenderers == null) return;
        foreach (var renderer in meshRenderers)
        {
            if (renderer != null) renderer.enabled = visible;
        }
    }

    // --- LÓGICA DE INTERACCIÓN CON OBJETOS ---
    private GameObject placedItemInstance = null;
    public bool IsOccupied => placedItemInstance != null;

    public void PlaceItem(GameObject itemPrefab)
    {
        if (IsOccupied) 
        {
            Debug.LogWarning($"Tile {name} is already occupied.");
            return;
        }

        if (itemPrefab != null)
        {
            float yOffset = 0.5f; // Ajusta este valor según sea necesario.
            Vector3 position = transform.position + new Vector3(0, yOffset, 0);
            
            placedItemInstance = Instantiate(itemPrefab, position, Quaternion.identity, transform);
        }
    }

    public void SetSelected(bool selected)
    {
        if (innerAnimator != null) innerAnimator.SetBool("IsSelected", selected);
        if (outerAnimator != null) outerAnimator.SetBool("IsSelected", selected);

        if (selected)
        {
            Color baseColor = GetColorForCurrentState();
            
            Color intensifiedColor = new Color(baseColor.r * selectedIntensity, baseColor.g * selectedIntensity, baseColor.b * selectedIntensity, baseColor.a);

            ApplyColor(intensifiedColor);
        }
        else
        {
            UpdateVisuals();
        }
    }

    private Color GetColorForCurrentState()
    {
        if (!isRevealed)
        {
            return isFlagged ? flaggedColor : defaultColor;
        }

        if (isTrap)
        {
            return trapColor;
        }
        
        if (dangerValue > 0)
        {
            return heatMapController.GetColorForValue(dangerValue);
        }

        return defaultColor; // Fallback for revealed, danger 0 tiles
    }

    public void ToggleFlag()
    {
        if (isRevealed) return;
        isFlagged = !isFlagged;
        if (innerAnimator != null) innerAnimator.SetBool("IsFlagged", isFlagged);
        if (outerAnimator != null) outerAnimator.SetBool("IsFlagged", isFlagged);
        if (flagAnimator != null) flagAnimator.SetBool("IsFlagged", isFlagged);
        UpdateVisuals();
    }

    public void DefuseTrap()
    {
        if (isTrap)
        {
            isTrap = false;
            hasDisarmedTrap = true;
            Debug.Log($"Trap on tile {name} has been defused.");
            if (disarmedTrapPrefab != null && disarmedTrapInstance == null)
            {
                Vector3 position = transform.position + new Vector3(0, 0.5f, 0);
                disarmedTrapInstance = Instantiate(disarmedTrapPrefab, position, Quaternion.Euler(0, Random.Range(0f, 360f), 90f), transform);
                disarmedTrapInstance.name = "DisarmedExplosive";
            }
        }
        else
        {
            Debug.Log($"Used Defuse on tile {name}, but it had no trap.");
        }
    }

    public void SetMineralState(bool state)
    {
        hasMineral = state;
        if (hasMineral)
        { 
            if (isTrap) 
            {
                isTrap = false;
                Debug.LogWarning($"Tile {name} was a trap but is now a mineral deposit.");
            }
            if (mineralPrefab != null && mineralInstance == null)
            {
                Vector3 position = transform.position + new Vector3(0, 0.5f, 0);
                mineralInstance = Instantiate(mineralPrefab, position, Quaternion.Euler(0, Random.Range(0f, 360f), 0), transform);
                mineralInstance.name = "MineralDeposit";
            }
        }
        if (mineralInstance != null)
        {
            mineralInstance.SetActive(hasMineral);
        }
    }

    public void ExtractMineral()
    {
        if (hasMineral)
        {
            Debug.Log($"Extracting mineral from {name}.");
            hasMineral = false;
            if (mineralInstance != null)
            {
                Destroy(mineralInstance);
            }
            if (mineralCollectiblePrefab != null)
            {
                Vector3 position = transform.position + new Vector3(0, 0.5f, 0);
                GameObject collectible = Instantiate(mineralCollectiblePrefab, position, Quaternion.Euler(-90, Random.Range(-90f, 360f), 90f));
                collectible.name = "MineralCollectible";
            }
            else
            {
                Debug.LogWarning("MineralCollectiblePrefab is not assigned!");
            }
        }
    }
}