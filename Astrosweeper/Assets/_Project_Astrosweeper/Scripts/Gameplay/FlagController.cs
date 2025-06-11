using UnityEngine;

public class FlagController : MonoBehaviour
{
    private Renderer[] renderers;

    private void Awake()
    {
        // Obtenemos todos los renderers en el prefab y sus hijos para ser más robustos.
        renderers = GetComponentsInChildren<Renderer>();
        Debug.Log($"[FlagController] Awake: Encontrados {renderers.Length} renderers en {gameObject.name}.", this.gameObject);
    }

    private void OnEnable()
    {
        Debug.Log("[FlagController] OnEnable: Suscribiendo a OnGameStateChanged.", this.gameObject);
        GameManager.OnGameStateChanged += HandleGameStateChanged;

        // Forzar una comprobación inicial del estado por si el objeto se instancia en medio de un estado válido
        if (GameManager.Instance != null)
        {
            Debug.Log($"[FlagController] OnEnable: Comprobando estado inicial: {GameManager.Instance.CurrentState}", this.gameObject);
            HandleGameStateChanged(GameManager.Instance.CurrentState);
        }
        else
        {
            Debug.LogWarning("[FlagController] OnEnable: GameManager.Instance es null. No se puede establecer el estado inicial.", this.gameObject);
        }
    }

    private void OnDisable()
    {
        GameManager.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void HandleGameStateChanged(GameState newState)
    {
        Debug.Log($"[FlagController] HandleGameStateChanged: Recibido nuevo estado {newState}.", this.gameObject);
        bool isVisible = newState == GameState.Prospecting || newState == GameState.TileSelection;
        Debug.Log($"[FlagController] Visibilidad de la bandera establecida en: {isVisible}.", this.gameObject);

        foreach (var renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.enabled = isVisible;
            }
        }
    }
}
