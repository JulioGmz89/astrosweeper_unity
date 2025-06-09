// Guardar en: Assets/_Project_StarSweep/Scripts/Player/PlayerController.cs

using UnityEngine;
using UnityEngine.InputSystem; // ¡Importante añadir esto!

public class PlayerController : MonoBehaviour
{
    // private PlayerMovement playerMovement; 

    private void OnEnable()
    {
        GameManager.OnGameStateChanged += HandleGameStateChange;
    }

    private void OnDisable()
    {
        GameManager.OnGameStateChanged -= HandleGameStateChange;
    }

    // El componente PlayerInput llamará a este método.
    // El nombre del método ("OnToggleProspecting") debe coincidir con el nombre de la Acción en el asset.
    public void OnToggleProspecting(InputAction.CallbackContext context)
    {
        // Nos aseguramos de que la acción se ejecute solo una vez (al presionar la tecla).
        if (context.performed)
        {
            GameManager.Instance.ToggleProspectingMode();
        }
    }

    private void HandleGameStateChange(GameState newState)
    {
        if (newState == GameState.Exploration)
        {
            // playerMovement.enabled = true;
            Debug.Log("Controlador del jugador: MODO EXPLORACIÓN ACTIVADO.");
        }
        else if (newState == GameState.Prospecting)
        {
            // playerMovement.enabled = false;
            Debug.Log("Controlador del jugador: MODO PROSPECCIÓN ACTIVADO.");
        }
    }
}