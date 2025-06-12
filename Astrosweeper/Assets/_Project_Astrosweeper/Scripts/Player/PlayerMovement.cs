using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 15f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Explosive Carrying Settings")]
    [SerializeField] private float carryingSpeedModifier = 0.5f;
    [SerializeField] private float detonationThreshold = 15f; // Velocidad instantánea que causa la detonación

    // Referencias de componentes
    private CharacterController controller;
    private Transform mainCameraTransform;
    private PlayerController playerController; // Referencia para llamar a la detonación

    // Estado del movimiento
    private Vector3 playerVelocity;
    private Vector3 lastPosition;

    private void Awake()
    {
        // Obtenemos las referencias que este script necesita para funcionar
        controller = GetComponent<CharacterController>();
        playerController = GetComponent<PlayerController>();
        mainCameraTransform = Camera.main.transform;
        lastPosition = transform.position;
    }

    // El método Update se mantiene para manejar la física constante como la gravedad.
    // Solo se ejecutará si el componente está habilitado (en Modo Exploración).
    private void Update()
    {
        // --- Lógica de Detonación por Movimiento Brusco ---
        if (GameManager.Instance.CurrentState == GameState.CarryingExplosive)
        {
            Vector3 currentVelocity = (transform.position - lastPosition) / Time.deltaTime;
            if (currentVelocity.magnitude > detonationThreshold)
            {
                playerController.DetonateCarriedExplosive();
                lastPosition = transform.position; // Reset position to prevent multiple detonations
                return; // Salir para evitar más procesamiento este frame
            }
        }
        lastPosition = transform.position;

        // Mantenemos al personaje pegado al suelo
        if (controller.isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
        }

        // Aplicamos la fuerza de la gravedad en cada frame
        playerVelocity.y += gravity * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);
    }

    /// <summary>
    /// Este es el nuevo método público que el PlayerController llamará en cada frame del Modo Exploración.
    /// Recibe el vector de input y ejecuta la lógica de movimiento y rotación.
    /// </summary>
    public void ProcessMove(Vector2 moveInput)
    {
        float currentSpeed = moveSpeed;
        if (GameManager.Instance.CurrentState == GameState.CarryingExplosive)
        {
            currentSpeed *= carryingSpeedModifier;
        }

        // Creamos un vector de movimiento 3D a partir del input 2D
        Vector3 moveDirection = new Vector3(moveInput.x, 0, moveInput.y);

        // Solo procesamos el movimiento si hay un input significativo
        if (moveDirection.magnitude >= 0.1f)
        {
            // --- Lógica de Movimiento Relativo a la Cámara ---
            // Obtenemos la dirección "adelante" de la cámara, aplanada en el suelo
            Vector3 cameraForward = Vector3.Scale(mainCameraTransform.forward, new Vector3(1, 0, 1)).normalized;
            // Calculamos el vector de movimiento final en coordenadas del mundo
            Vector3 move = moveDirection.z * cameraForward + moveDirection.x * mainCameraTransform.right;

            // Movemos el CharacterController
            controller.Move(move * currentSpeed * Time.deltaTime);

            // --- Lógica de Rotación ---
            // Hacemos que el personaje rote suavemente para mirar en la dirección en la que se mueve
            float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.y) * Mathf.Rad2Deg + mainCameraTransform.eulerAngles.y;
            Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }

    // El método OnMove(InputAction.CallbackContext context) ha sido eliminado de este script.
}