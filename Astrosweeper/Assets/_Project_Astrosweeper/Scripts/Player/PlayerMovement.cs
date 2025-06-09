// Guardar en: Assets/_Project_StarSweep/Scripts/Player/PlayerMovement.cs

using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 15f;
    [SerializeField] private float gravity = -9.81f;

    private CharacterController controller;
    private Vector2 moveInput;
    private Transform mainCameraTransform;

    private Vector3 playerVelocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        // Guardamos el transform de la cámara para no llamarlo en cada frame.
        mainCameraTransform = Camera.main.transform;
    }

    void Update()
    {
        // --- Manejo de Gravedad ---
        // Si el personaje está en el suelo y no se está moviendo hacia abajo, reseteamos su velocidad vertical.
        if (controller.isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f; // Un pequeño valor para mantenerlo pegado al suelo.
        }
        
        // Aplicamos la gravedad constantemente. Se multiplica por Time.deltaTime dos veces.
        playerVelocity.y += gravity * Time.deltaTime;

        // --- Manejo de Movimiento ---
        Vector3 moveDirection = new Vector3(moveInput.x, 0, moveInput.y);
        
        // Solo procesamos el movimiento si hay input para evitar que el personaje rote al centro.
        if (moveDirection.magnitude >= 0.1f)
        {
            // Obtenemos la dirección hacia adelante de la cámara, ignorando su inclinación vertical.
            Vector3 cameraForward = Vector3.Scale(mainCameraTransform.forward, new Vector3(1, 0, 1)).normalized;
            Vector3 move = moveDirection.z * cameraForward + moveDirection.x * mainCameraTransform.right;

            // Aplicamos el movimiento usando el método .Move() que espera un delta de movimiento.
            controller.Move(move * moveSpeed * Time.deltaTime);

            // Rotamos el personaje para que mire en la dirección del movimiento.
            // Usamos Atan2 para una rotación suave y correcta basada en el input.
            float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.y) * Mathf.Rad2Deg + mainCameraTransform.eulerAngles.y;
            Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }

        // Aplicamos el movimiento final de la gravedad.
        controller.Move(playerVelocity * Time.deltaTime);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
}