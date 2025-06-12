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
    [SerializeField] private float throwRange = 10f; // Rango para lanzar el explosivo
    [SerializeField] private LayerMask tileLayer;

    [Header("Movement Settings")]
    [SerializeField] private float rotationSpeed = 100f; // Velocidad de rotación en modo Prospecting

    [Header("Visuals")]
    [SerializeField] private GameObject holoCone;
    [SerializeField] private GameObject carriedExplosiveVisual; // Visual del explosivo que carga el jugador
    [SerializeField] private GameObject explosiveProjectilePrefab; // Prefab for the thrown explosive
    [SerializeField] private float throwDuration = 1.0f; // Duration of the throw
    [SerializeField] private float throwArcHeight = 2.0f; // Arc height of the throw

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
        if (currentState == GameState.Exploration || currentState == GameState.CarryingExplosive)
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
        // Allow tile navigation in both standard selection and throw-aiming modes.
        if ((GameManager.Instance.CurrentState == GameState.TileSelection || GameManager.Instance.CurrentState == GameState.ThrowObject) 
            && context.performed)
        {
            Vector2 moveInput = context.ReadValue<Vector2>();
            NavigateTiles(moveInput);
        }
    }

    public void OnToggleProspecting(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        if (GameManager.Instance.CurrentState == GameState.CarryingExplosive)
        {
            DetonateCarriedExplosive();
            return;
        }

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
            HexTile selectedTile = ProspectingManager.Instance.CurrentlySelectedTile;
            if (selectedTile != null)
            {
                ProspectingManager.Instance.RevealTile(selectedTile);
            }
        }
    }

    public void OnDisarm(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        GameState currentState = GameManager.Instance.CurrentState;

        if (currentState == GameState.TileSelection)
        {
            HexTile tileToDisarm = ProspectingManager.Instance.CurrentlySelectedTile;
            if (tileToDisarm != null && tileToDisarm.isTrap)
            {
                tileToDisarm.DefuseTrap();
                GameManager.Instance.SwitchState(GameState.Prospecting);
            }
            else if (tileToDisarm != null)
            {
                ProspectingManager.Instance.RevealTile(tileToDisarm);
            }
        }
        else if (currentState == GameState.ThrowObject)
        {
            HexTile targetTile = ProspectingManager.Instance.CurrentlySelectedTile;
            if (targetTile != null)
            {
                ThrowExplosive(targetTile);
            }
        }
    }

    // --- LÓGICA PRIVADA Y DE GESTIÓN DE ESTADO ---

    private void HandleGameStateChange(GameState newState)
    {
        playerMovement.enabled = (newState == GameState.Exploration || newState == GameState.CarryingExplosive);

        if (playerCamera != null)
            playerCamera.enabled = (newState == GameState.Exploration || newState == GameState.Prospecting || newState == GameState.CarryingExplosive);
        
        if (tileSelectionCamera != null)
            tileSelectionCamera.enabled = (newState == GameState.TileSelection || newState == GameState.ThrowObject);

        if (holoCone != null)
            holoCone.SetActive(newState == GameState.Prospecting || newState == GameState.TileSelection || newState == GameState.ThrowObject);

        if (carriedExplosiveVisual != null)
            // Keep the visual active while carrying and aiming the throw
            carriedExplosiveVisual.SetActive(newState == GameState.CarryingExplosive || newState == GameState.ThrowObject);

        if (newState == GameState.Exploration)
        {
            currentTargetTile = null;
        }

        if (newState == GameState.ThrowObject)
        {
            HexTile selectedTile = ProspectingManager.Instance.CurrentlySelectedTile;
            if (tileSelectionCamera != null && selectedTile != null)
            {
                tileSelectionCamera.Follow = selectedTile.transform;
                tileSelectionCamera.LookAt = selectedTile.transform;
            }
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

        // Draw the throw range when carrying an explosive
        if (GameManager.Instance != null && 
           (GameManager.Instance.CurrentState == GameState.CarryingExplosive || GameManager.Instance.CurrentState == GameState.ThrowObject))
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, throwRange);
        }
    }

    // --- NUEVO MÉTODO DE INPUT ---
    public void OnThrowMode(InputAction.CallbackContext context)
    {
        if (!context.performed || GameManager.Instance.CurrentState != GameState.CarryingExplosive) return;

        // --- REORDERED LOGIC ---
        // First, find and select the closest tile to start aiming from.
        FindClosestTile();
        if (currentTargetTile != null)
        {
            ProspectingManager.Instance.SetSelectedTile(currentTargetTile);
        }
        else
        {
            // If no tile is nearby, select the first tile of the grid as a fallback.
            ProspectingManager.Instance.SelectFirstTile();
        }

        // Then, switch the game state. This ensures the camera focuses on the correct tile.
        GameManager.Instance.EnterThrowObjectMode();
    }

    private void ThrowExplosive(HexTile targetTile)
    {
        float distance = Vector3.Distance(transform.position, targetTile.transform.position);
        if (distance > throwRange)
        { 
            Debug.LogWarning($"Target {targetTile.name} is out of range! ({distance:F1}m > {throwRange}m)");
            // Future: Add visual/sound feedback to the player
            return;
        }

        StartCoroutine(ThrowAndExplodeCoroutine(targetTile));
    }

    private System.Collections.IEnumerator ThrowAndExplodeCoroutine(HexTile targetTile)
    {
        Vector3 startPosition = carriedExplosiveVisual.transform.position;

        // Switch to exploration so player can move. This will also hide the carried visual via HandleGameStateChange.
        GameManager.Instance.EnterExplorationMode();

        GameObject projectileGO = Instantiate(explosiveProjectilePrefab, startPosition, Quaternion.identity);
        ExplosiveProjectile projectile = projectileGO.GetComponent<ExplosiveProjectile>();

        if (projectile != null)
        {
            // Wait for the projectile to reach its destination
            yield return projectile.TravelToTarget(targetTile.transform.position, throwDuration, throwArcHeight);

            // Now that it has arrived, trigger the explosion/extraction
            Debug.Log($"Explosive landed on {targetTile.name}.");
            if (targetTile.hasMineral)
            {
                targetTile.ExtractMineral();
            }
            else
            {
                Debug.Log("...but no mineral was on the target tile.");
            }

            // Clean up the projectile
            Destroy(projectileGO);
        }
        else
        {
            Debug.LogError("Explosive projectile prefab is missing the ExplosiveProjectile script!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (GameManager.Instance.CurrentState != GameState.Exploration) return;

        if (other.CompareTag("DisarmedExplosive"))
        {
            Debug.Log("Player picked up a disarmed explosive.");
            Destroy(other.gameObject);
            GameManager.Instance.EnterCarryingExplosiveMode();
        }
        else if (other.CompareTag("MineralCollectible"))
        {
            Debug.Log("Player collected a mineral.");
            ProspectingManager.Instance.CollectMineral();
            Destroy(other.gameObject);
        }
    }

    public void DetonateCarriedExplosive()
    {
        Debug.LogWarning("BOOM! Player detonated the carried explosive!");
        // Here you would add damage logic to the player
        GameManager.Instance.EnterExplorationMode();
    }


}