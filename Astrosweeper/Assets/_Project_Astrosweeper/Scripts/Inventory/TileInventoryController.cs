// TileInventoryController.cs (Versión Final Corregida)
using System.Collections.Generic;
using System.Linq;
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

    private Vector2 VectorLoco;


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

    // void Update()
    // {

    //     Debug.Log(navigateAction);

    //     VectorLoco = navigateAction.ReadValue<Vector2>();

    //     Debug.Log(VectorLoco.x);
    //     Debug.Log(VectorLoco.y);
    // }	

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
        
        List<Vector3> edgeMidpoints = GetHexagonTopEdgeMidpoints(targetTile);
        Vector3 positionOnEdge;

        if (edgeMidpoints != null)
        {
            // Find the midpoint closest to the camera
            Vector3 cameraPosition = Camera.main.transform.position;
            Vector3 closestMidpoint = edgeMidpoints[0];
            float minDistanceSq = (closestMidpoint - cameraPosition).sqrMagnitude;

            for (int i = 1; i < edgeMidpoints.Count; i++)
            {
                float distanceSq = (edgeMidpoints[i] - cameraPosition).sqrMagnitude;
                if (distanceSq < minDistanceSq)
                {
                    minDistanceSq = distanceSq;
                    closestMidpoint = edgeMidpoints[i];
                }
            }
            positionOnEdge = closestMidpoint;
        }
        else
        {
            // Fallback to circular approximation if vertices can't be determined
            var renderer = targetTile.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                Vector3 tilePosition = targetTile.transform.position;
                float hexRadius = renderer.bounds.extents.x;
                Vector3 directionToCamera = Camera.main.transform.position - tilePosition;
                directionToCamera.y = 0;
                directionToCamera.Normalize();
                positionOnEdge = tilePosition + directionToCamera * hexRadius;
            }
            else
            {
                // Absolute fallback to the center
                positionOnEdge = targetTile.transform.position;
            }
        }

        // Posicionamos y mostramos la UI.
        inventoryUIParent.SetActive(true);
        float yOffset = 0.2f;
        inventoryUIParent.transform.position = positionOnEdge + new Vector3(0, yOffset, 0);
        inventoryUIParent.transform.rotation = Quaternion.identity;

        // Actualizamos el modelo del item mostrado.
        UpdateDisplay();
    }

    private List<Vector3> GetHexagonTopEdgeMidpoints(HexTile tile)
    {
        var meshFilter = tile.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null) return null;

        Vector3[] vertices = meshFilter.sharedMesh.vertices;
        if (vertices.Length < 6) return null;

        float topY = vertices.Max(v => v.y);

        List<Vector3> topVertices = new List<Vector3>();
        foreach (Vector3 vertex in vertices)
        {
            if (Mathf.Abs(vertex.y - topY) < 0.001f)
            {
                bool isDuplicate = topVertices.Any(v => Vector3.Distance(v, vertex) < 0.001f);
                if (!isDuplicate)
                {
                    topVertices.Add(vertex);
                }
            }
        }
        
        if (topVertices.Count == 7)
        {
            // A 7-vertex top face likely includes a center point, which we remove.
            topVertices.RemoveAll(v => new Vector2(v.x, v.z).sqrMagnitude < 0.001f);
        }

        if (topVertices.Count != 6)
        {
            Debug.LogWarning($"Could not determine 6 hexagon vertices for tile {tile.name} (found {topVertices.Count}). Falling back to approximation.");
            return null;
        }

        topVertices = topVertices.OrderBy(v => Mathf.Atan2(v.x, v.z)).ToList();

        List<Vector3> edgeMidpoints = new List<Vector3>();
        for (int i = 0; i < topVertices.Count; i++)
        {
            Vector3 p1 = tile.transform.TransformPoint(topVertices[i]);
            Vector3 p2 = tile.transform.TransformPoint(topVertices[(i + 1) % topVertices.Count]);
            edgeMidpoints.Add((p1 + p2) / 2);
        }

        return edgeMidpoints;
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