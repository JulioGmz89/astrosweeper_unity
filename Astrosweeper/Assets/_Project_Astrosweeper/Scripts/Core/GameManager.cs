using UnityEngine;

public enum GameState 
{
    Exploration, // Movimiento libre en 3D
    Prospecting  // Interfaz de Buscaminas holográfica
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; }

    // Eventos para que otros sistemas reaccionen a los cambios de estado
    public static event System.Action<GameState> OnGameStateChanged;

    private void Awake()
    {
        // Patrón Singleton para asegurar una única instancia
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Opcional, si queremos que persista entre escenas
        }
    }

    private void Start()
    {
        // El juego siempre comienza en modo exploración
        SwitchState(GameState.Exploration);
    }

    public void SwitchState(GameState newState)
    {
        if (CurrentState == newState) return;

        CurrentState = newState;
        Debug.Log($"Cambiando a estado: {newState}");

        // Lanzamos el evento para notificar a otros sistemas (UI, PlayerController, Cámara, etc.)
        OnGameStateChanged?.Invoke(newState);
    }

    // Ejemplo de cómo podríamos cambiar de estado (esto lo llamaría el PlayerController)
    public void ToggleProspectingMode()
    {
        if (CurrentState == GameState.Exploration)
        {
            SwitchState(GameState.Prospecting);
        }
        else
        {
            SwitchState(GameState.Exploration);
        }
    }
}