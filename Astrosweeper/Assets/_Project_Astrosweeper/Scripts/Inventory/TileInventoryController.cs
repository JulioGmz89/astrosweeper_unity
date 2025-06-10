// TileInventoryController.cs (Versión Final Corregida)
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TileInventoryController : MonoBehaviour
{
    [Header("Referencias a la UI 3D")]
    [SerializeField] private GameObject inventoryUIParent;
    [SerializeField] private Transform itemDisplaySlot;
    [SerializeField] private GameObject leftArrowObject;
    [SerializeField] private GameObject rightArrowObject;

    [Header("Datos del Inventario")]
    [SerializeField] private List<InventoryItem> playerInventory;

    [Header("Dependencias")]
    [SerializeField] private ProspectingManager prospectingManager;
    [SerializeField] private PlayerInput playerInput;

    private int currentItemIndex = 0;
    private GameObject currentItemInstance;
    private InputAction navigateAction;

    private void Awake()
    {
        if (playerInput != null)
        {
            navigateAction = playerInput.actions["MapsInventory"];
        }
    }

    private void OnEnable()
    {
        GameManager.OnGameStateChanged += HandleGameStateChanged;
        if (prospectingManager != null)
        {
            prospectingManager.OnSelectedTileChanged += UpdatePosition;
        }
    }

    private void OnDisable()
    {
        GameManager.OnGameStateChanged -= HandleGameStateChanged;
        if (prospectingManager != null)
        {
            prospectingManager.OnSelectedTileChanged -= UpdatePosition;
        }
    }

    private void Start()
    {
        inventoryUIParent.SetActive(false);
    }
    
    // --- LÓGICA DE ESTADO MEJORADA ---
    private void HandleGameStateChanged(GameState newState)
    {
        if (newState == GameState.TileSelection)
        {
            // Al entrar en el estado, mostramos el inventario inmediatamente
            // en la posición del tile que ya está seleccionado.
            UpdatePosition(prospectingManager.CurrentlySelectedTile);
            
            // Y nos suscribimos al input de navegación del inventario.
            if (navigateAction != null) navigateAction.performed += OnNavigate;
        }
        else
        {
            // Al salir de este estado, nos aseguramos de ocultar la UI
            // y de dejar de escuchar el input.
            inventoryUIParent.SetActive(false);
            if (navigateAction != null) navigateAction.performed -= OnNavigate;
        }
    }
    
    // --- LÓGICA DE POSICIÓN REFINADA ---
    private void UpdatePosition(HexTile targetTile)
    {
        // Si no hay un tile objetivo, no hay nada que hacer.
        if (targetTile == null)
        {
            inventoryUIParent.SetActive(false);
            return;
        }
        
        // Posicionamos y mostramos la UI.
        inventoryUIParent.SetActive(true);
        float yOffset = 1.5f; 
        inventoryUIParent.transform.position = targetTile.transform.position + new Vector3(0, yOffset, 0);
        inventoryUIParent.transform.rotation = Quaternion.identity;

        // Actualizamos el modelo del item mostrado.
        UpdateDisplay();
    }
    
    public void OnNavigate(InputAction.CallbackContext context)
    {
        if (playerInventory.Count <= 1) return;
        Vector2 input = context.ReadValue<Vector2>();

        if (input.x < 0) { currentItemIndex--; }
        else if (input.x > 0) { currentItemIndex++; }

        if (currentItemIndex < 0) { currentItemIndex = playerInventory.Count - 1; }
        if (currentItemIndex >= playerInventory.Count) { currentItemIndex = 0; }
        
        UpdateDisplay();
    }
    
    private void UpdateDisplay()
    {
        if (itemDisplaySlot == null) return;
        if (currentItemInstance != null) { Destroy(currentItemInstance); }
        if (playerInventory.Count == 0) return;

        InventoryItem currentItem = playerInventory[currentItemIndex];
        if (currentItem.displayModel != null)
        {
            currentItemInstance = Instantiate(currentItem.displayModel, itemDisplaySlot.position, itemDisplaySlot.rotation, itemDisplaySlot);
        }

        bool showArrows = playerInventory.Count > 1;
        leftArrowObject.SetActive(showArrows);
        rightArrowObject.SetActive(showArrows);
    }
}