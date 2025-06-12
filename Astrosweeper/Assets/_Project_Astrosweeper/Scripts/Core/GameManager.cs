using UnityEngine;
using System; // Requerido para usar la clase Action

/// <summary>
/// Define los estados globales del juego. Al estar fuera de la clase,
/// es fácilmente accesible desde otros scripts sin necesidad de una referencia al GameManager.
/// </summary>
public enum GameState
{
    Exploration,    // El jugador se mueve libremente por el mundo en 3D.
    Prospecting,    // El jugador observa la cuadrícula holográfica desde una vista orbital.
    TileSelection,  // El jugador navega y selecciona tiles individuales en la cuadrícula.
    CarryingExplosive, // El jugador transporta un explosivo con movimiento limitado.
    ThrowObject     // El jugador está apuntando para lanzar un explosivo.
}

/// <summary>
/// Gestiona el estado global del juego (GameState), actuando como un director central.
/// Utiliza un evento estático para notificar a otros sistemas de los cambios de estado.
/// </summary>
public class GameManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    // Proporciona una única instancia global accesible desde cualquier script.
    public static GameManager Instance { get; private set; }

    // --- State Properties ---
    // Propiedad pública para que cualquier script pueda consultar el estado actual.
    public GameState CurrentState { get; private set; }

    /// <summary>
    /// Evento estático que se dispara cuando el estado del juego cambia.
    /// Otros sistemas pueden suscribirse directamente (ej: GameManager.OnGameStateChanged += ...).
    /// </summary>
    public static event Action<GameState> OnGameStateChanged;

    private void Awake()
    {
        // Lógica del Singleton para asegurar que solo exista una instancia del GameManager.
        if (Instance != null && Instance != this)
        {
            // Si ya hay una instancia, destruir este objeto para evitar duplicados.
            Destroy(gameObject);
        }
        else
        {
            // Si no hay ninguna, esta se convierte en la instancia única.
            Instance = this;
            // Opcional: Hace que el GameManager no se destruya al cargar nuevas escenas.
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Start()
    {
        // Según el GDD y la lógica de juego, la partida siempre comienza en modo Exploración.
        // Usamos una llamada a SwitchState en lugar de asignar la variable directamente
        // para asegurarnos de que el evento OnGameStateChanged se dispare al inicio.
        SwitchState(GameState.Exploration);
    }

    /// <summary>
    /// El método principal para cambiar de un estado de juego a otro.
    /// </summary>
    /// <param name="newState">El nuevo estado al que se va a transicionar.</param>
    public void SwitchState(GameState newState)
    {
        // Evitar cambiar al mismo estado para no ejecutar la lógica innecesariamente.
        if (CurrentState == newState) return;

        CurrentState = newState;
        Debug.Log($"[GameManager] Cambiando a estado: {newState}");

        // Disparar el evento estático para notificar a todos los suscriptores.
        // El operador '?' (null-conditional) previene un error si no hay suscriptores.
        OnGameStateChanged?.Invoke(newState);
    }

    // --- Métodos Públicos de Transición ---
    // Estos métodos pueden ser llamados desde otros scripts (como PlayerController o UI)
    // para solicitar un cambio de estado de manera clara y legible.

    public void EnterProspectingMode()
    {
        // Solo se puede entrar a Prospecting desde Exploration.
        if (CurrentState == GameState.Exploration)
        {
            SwitchState(GameState.Prospecting);
        }
    }

    public void EnterExplorationMode()
    {
        // Se puede volver a Exploration desde varios estados.
        if (CurrentState == GameState.Prospecting || CurrentState == GameState.TileSelection || CurrentState == GameState.CarryingExplosive || CurrentState == GameState.ThrowObject)
        {
            SwitchState(GameState.Exploration);
        }
    }

    public void EnterThrowObjectMode()
    {
        if (CurrentState == GameState.CarryingExplosive)
        {
            SwitchState(GameState.ThrowObject);
        }
    }

    public void EnterCarryingExplosiveMode()
    {
        if (CurrentState == GameState.Exploration)
        {
            SwitchState(GameState.CarryingExplosive);
        }
    }
    
    // NOTA: No hay un método público aquí para entrar en TileSelection.
    // Como hemos discutido, la responsabilidad de entrar en ese estado recae en el
    // ProspectingManager cuando se selecciona un tile específico. Ese manager
    // llamará directamente a: GameManager.Instance.SwitchState(GameState.TileSelection);
}