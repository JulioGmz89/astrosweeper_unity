using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

[RequireComponent(typeof(PlayerMovement), typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private CinemachineCamera playerCamera;
    [SerializeField] private CinemachineCamera tileSelectionCamera;

    [Header("Interaction Settings")]
    [SerializeField] private float interactionRadius = 2f;
    [SerializeField] private LayerMask tileLayer;

    [Header("Movement Settings")]
    [SerializeField] private float rotationSpeed = 100f; // Velocidad de rotación en modo Prospecting

    [Header("Visuals")]
    [SerializeField] private GameObject holoCone;

    // --- Referencias de Componentes ---
    private PlayerMovement playerMovement;
    private PlayerInput playerInput; // Referencia al componente PlayerInput

    // --- Estado ---
    private HexTile currentTargetTile;
    private Vector2 moveInputVector; // NUEVA VARIABLE para guardar el input de movimiento

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerInput = GetComponent<PlayerInput>(); // Obtenemos la referencia
    }

    private void OnEnable()
    {
        GameManager.OnGameStateChanged += HandleGameStateChange;
    }

    private void OnDisable()
    {
        GameManager.OnGameStateChanged -= HandleGameStateChange;
    }

    private void Update()
    {
        GameState currentState = GameManager.Instance.CurrentState;

        // Leemos el input de movimiento una vez por frame para usarlo en diferentes estados.
        moveInputVector = playerInput.actions["Move"].ReadValue<Vector2>();

        // --- Lógica de Movimiento Continuo ---
        if (currentState == GameState.Exploration)
        {
            playerMovement.ProcessMove(moveInputVector);
        }

        // --- Lógica de Detección de Teselas y Rotación en modo Prospecting ---
        if (currentState == GameState.Prospecting)
        {
            FindClosestTile();

            // Lógica de rotación con A y D (eje horizontal del input 'Move')
            float rotationInput = moveInputVector.x;
            transform.Rotate(Vector3.up, rotationInput * rotationSpeed * Time.deltaTime);
        }
    }

    private void FindClosestTile()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, interactionRadius, tileLayer);
        float closestDistanceSqr = float.MaxValue;
        HexTile closestTile = null;

        foreach (var hitCollider in hitColliders)
        {
            float distanceSqr = (transform.position - hitCollider.transform.position).sqrMagnitude;
            if (distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closestTile = hitCollider.GetComponent<HexTile>();
            }
        }
        currentTargetTile = closestTile;
    }

    // --- GESTIÓN DE INPUTS (Llamados por el componente PlayerInput) ---

    /// <summary>
    /// Este método ahora SOLO gestiona la navegación por teselas (una vez por presión).
    /// </summary>
    public void OnMove(InputAction.CallbackContext context)
    {
        if (GameManager.Instance.CurrentState == GameState.TileSelection && context.performed)
        {
            Vector2 moveInput = context.ReadValue<Vector2>();
            NavigateTiles(moveInput);
        }
    }

    public void OnToggleProspecting(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        // Obtenemos la instancia del GameManager una sola vez.
        var gameManager = GameManager.Instance;

        // Comprobamos el estado actual para decidir qué hacer.
        if (gameManager.CurrentState == GameState.Exploration)
        {
            // Si estamos explorando, entramos en modo prospección.
            gameManager.EnterProspectingMode();
        }
        else if (gameManager.CurrentState == GameState.Prospecting)
        {
            // Si estamos en prospección, volvemos a exploración.
            gameManager.EnterExplorationMode();
        }
        // Nota: Si estamos en TileSelection, este input no hará nada,
        // lo cual es correcto. Para salir de TileSelection se usa OnConfirmSelection.
    }

    public void OnConfirmSelection(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        GameState currentState = GameManager.Instance.CurrentState;

        if (currentState == GameState.Prospecting && currentTargetTile != null)
        {
            ProspectingManager.Instance.SetSelectedTile(currentTargetTile);
            GameManager.Instance.SwitchState(GameState.TileSelection);

            if (tileSelectionCamera != null)
            {
                tileSelectionCamera.Follow = currentTargetTile.transform;
                tileSelectionCamera.LookAt = currentTargetTile.transform;
            }
        }
        else if (currentState == GameState.TileSelection)
        {
            GameManager.Instance.SwitchState(GameState.Prospecting);
        }
    }

    public void OnDisarm(InputAction.CallbackContext context)
    {
        if (context.performed && GameManager.Instance.CurrentState == GameState.TileSelection)
        {
            HexTile tileToDisarm = ProspectingManager.Instance.CurrentlySelectedTile;
            if (tileToDisarm != null)
            {
                ProspectingManager.Instance.RevealTile(tileToDisarm);
            }
        }
    }
    
    // --- LÓGICA PRIVADA Y DE GESTIÓN DE ESTADO ---

    private void HandleGameStateChange(GameState newState)
    {
        playerMovement.enabled = (newState == GameState.Exploration);
        if (playerCamera != null)
            playerCamera.enabled = (newState == GameState.Exploration || newState == GameState.Prospecting);
        if (tileSelectionCamera != null)
            tileSelectionCamera.enabled = (newState == GameState.TileSelection);

        // --- Lógica del HoloCone ---
        if (holoCone != null)
        {
            holoCone.SetActive(newState == GameState.Prospecting || newState == GameState.TileSelection);
        }

        if (newState == GameState.Exploration)
        {
            currentTargetTile = null;
        }
    }
    
    private void NavigateTiles(Vector2 moveInput)
    {
        HexTile currentSelection = ProspectingManager.Instance.CurrentlySelectedTile;
        HexTile nextTile = ProspectingManager.Instance.GetNeighborInDirection(currentSelection, moveInput);
        if (nextTile != null)
        {
            ProspectingManager.Instance.SetSelectedTile(nextTile);
            if (tileSelectionCamera != null)
            {
                tileSelectionCamera.Follow = nextTile.transform;
                tileSelectionCamera.LookAt = nextTile.transform;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}