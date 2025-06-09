using UnityEngine;

public class HexTile : MonoBehaviour
{
    // Coordenadas del hexágono en la cuadrícula (usaremos Axial para la lógica)
    public Vector2Int axialCoords;

    // Propiedades según el GDD
    public bool isTrap = false;
    public bool isRevealed = false;
    public bool isFlagged = false; // "Bandera" de Buscaminas
    public int dangerValue = 0; // 0: seguro, 1-6: número de trampas adyacentes

    // TODO: Añadir referencias a materiales o colores para visualización
    // public MeshRenderer meshRenderer;

    public void Setup(Vector2Int coords)
    {
        this.axialCoords = coords;
        transform.name = $"Hex_{coords.x}_{coords.y}";
    }

    // TODO: Lógica para revelar la tesela
    public void Reveal()
    {
        if (isRevealed || isFlagged) return;
        isRevealed = true;
        // Aquí iría la lógica para mostrar el dangerValue o activar la trampa
    }
}