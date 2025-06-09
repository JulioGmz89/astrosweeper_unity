using UnityEngine;
using System.Collections.Generic;

public class ProspectingManager : MonoBehaviour
{
    [SerializeField] private GameObject hexGridContainer; // Un objeto para mantener los hexágonos ordenados
    [SerializeField] private HexTile hexTilePrefab;
    [SerializeField] private int gridRadius = 5;

    // Diccionario para acceder fácilmente a cualquier tesela por sus coordenadas
    private Dictionary<Vector2Int, HexTile> hexGrid = new Dictionary<Vector2Int, HexTile>();
    
    // Variables para el layout del hexágono
    private float hexOuterRadius = 1f;
    private float hexInnerRadius;
    private Vector3 hexSize;

    void Start()
    {
        // Nos suscribimos al evento del GameManager
        GameManager.OnGameStateChanged += HandleGameStateChange;
        
        // Calculamos las dimensiones del hexágono
        hexInnerRadius = hexOuterRadius * Mathf.Sqrt(3) / 2;
        hexSize = new Vector3(hexOuterRadius, 1, hexInnerRadius * 2);

        // Generamos la cuadrícula al inicio, pero la mantenemos oculta
        GenerateGrid();
        hexGridContainer.SetActive(false);
    }

    private void OnDestroy()
    {
        GameManager.OnGameStateChanged -= HandleGameStateChange;
    }

    private void HandleGameStateChange(GameState newState)
    {
        // Mostramos u ocultamos la cuadrícula según el estado del juego
        hexGridContainer.SetActive(newState == GameState.Prospecting);
        if (newState == GameState.Prospecting)
        {
            // TODO: Centrar la cuadrícula frente al jugador
        }
    }

    void GenerateGrid()
    {
        for (int q = -gridRadius; q <= gridRadius; q++)
        {
            int r1 = Mathf.Max(-gridRadius, -q - gridRadius);
            int r2 = Mathf.Min(gridRadius, -q + gridRadius);
            for (int r = r1; r <= r2; r++)
            {
                Vector2Int axialCoords = new Vector2Int(q, r);
                
                // Convertir coordenadas axiales a posición en el mundo
                float x = hexOuterRadius * 1.5f * q;
                float z = hexInnerRadius * 2 * (r + q / 2f);

                Vector3 position = new Vector3(x, 0, z);

                HexTile newTile = Instantiate(hexTilePrefab, position, Quaternion.identity, hexGridContainer.transform);
                newTile.Setup(axialCoords);
                hexGrid.Add(axialCoords, newTile);
            }
        }
        // TODO: Lógica para colocar trampas y calcular los valores de peligro
    }
}