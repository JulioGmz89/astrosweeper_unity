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

    // --- Estado ---
    private HexTile currentTargetTile;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
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
        if (GameManager.Instance.CurrentState == GameState.Prospecting)
        {
            FindClosestTile();
        }
        else
        {
            currentTargetTile = null;
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

    public void OnToggleProspecting(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            GameManager.Instance.ToggleProspectingMode();
        }
    }

    public void OnDisarm(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        Debug.Log("Se presionó la tecla de Desarmar.");
        if (GameManager.Instance.CurrentState != GameState.Prospecting)
        {
            Debug.Log("Intento de desarme fallido: No estamos en Modo Prospección.");
            return;
        }
        if (currentTargetTile == null)
        {
            Debug.Log("Intento de desarme fallido: No hay ninguna tesela en el rango de interacción.");
            return;
        }
        
        Debug.Log($"Éxito. Enviando orden de revelar para: {currentTargetTile.name}");
        ProspectingManager.Instance.RevealTile(currentTargetTile);
    }

    private void HandleGameStateChange(GameState newState)
    {
        playerMovement.enabled = (newState == GameState.Exploration);
        if (newState != GameState.Prospecting)
        {
            currentTargetTile = null;
        }
    }

    /// <summary>
    /// Este método especial de Unity dibuja Gizmos en el Editor de la Escena.
    /// Solo se dibuja cuando el objeto está seleccionado.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}