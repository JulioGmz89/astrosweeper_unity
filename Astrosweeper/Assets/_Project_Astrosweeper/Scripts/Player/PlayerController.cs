using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerController : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("Radio para detectar teselas cercanas al jugador.")]
    [SerializeField] private float interactionRadius = 2f;
    [Tooltip("Asigna aquí la Layer que creaste para las teselas (ej. 'HexTile').")]
    [SerializeField] private LayerMask tileLayer;

    // --- Referencias de Componentes ---
    private PlayerMovement playerMovement;
    private PlayerInput playerInput; // Para acceder a las acciones de input

    // --- Estado ---
    private HexTile currentTargetTile; // La tesela que tenemos en rango para interactuar

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerInput = GetComponent<PlayerInput>(); // Obtenemos la referencia al componente PlayerInput
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
        // Solo buscamos teselas si estamos en modo prospección
        if (GameManager.Instance.CurrentState == GameState.Prospecting)
        {
            FindClosestTile();
        }
        else
        {
            // Si no estamos en modo prospección, nos aseguramos de no tener ninguna tesela seleccionada
            if (currentTargetTile != null)
            {
                // TODO: Lógica para 'des-resaltar' la tesela visualmente
                currentTargetTile = null;
            }
        }
    }

    /// <summary>
    /// Detecta la tesela más cercana al jugador dentro de un radio.
    /// </summary>
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

        if (currentTargetTile != closestTile)
        {
            // TODO: Lógica para resaltar el nuevo 'closestTile' y des-resaltar el anterior 'currentTargetTile'
            // Debug.Log($"Nuevo objetivo: {closestTile?.name ?? "Ninguno"}");
        }
        
        currentTargetTile = closestTile;
    }

    /// <summary>
    /// Método llamado por el PlayerInput cuando se presiona la tecla de cambiar modo.
    /// </summary>
    public void OnToggleProspecting(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            GameManager.Instance.ToggleProspectingMode();
        }
    }

    /// <summary>
    /// Método llamado por el PlayerInput cuando se presiona la tecla de Desarmar/Revelar.
    /// </summary>
    public void OnDisarm(InputAction.CallbackContext context)
    {
        // Solo actúa si se presionó la tecla, estamos en modo prospección y tenemos una tesela en rango
        if (context.performed && currentTargetTile != null && GameManager.Instance.CurrentState == GameState.Prospecting)
        {
            Debug.Log($"Acción 'Desarmar' sobre la tesela: {currentTargetTile.name}");
            
            // Le pedimos al manager que se encargue de la lógica de revelar la tesela
            ProspectingManager.Instance.RevealTile(currentTargetTile);
        }
    }

    /// <summary>
    /// Escucha los cambios de estado del juego para activar/desactivar la lógica correspondiente.
    /// </summary>
    private void HandleGameStateChange(GameState newState)
    {
        // El movimiento del jugador solo está activo en modo exploración
        playerMovement.enabled = (newState == GameState.Exploration);

        // Si salimos del modo prospección, limpiamos la tesela objetivo
        if (newState != GameState.Prospecting)
        {
            currentTargetTile = null;
        }
    }
}