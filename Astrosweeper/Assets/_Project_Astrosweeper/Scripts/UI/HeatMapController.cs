using UnityEngine;

// Definimos la estructura de datos para asociar un valor a un color.
// [System.Serializable] permite que esta estructura aparezca y sea editable en el Inspector de Unity.
[System.Serializable]
public class HeatColor
{
    public int value;
    public Color color;
}

public class HeatMapController : MonoBehaviour
{
    // Declaramos el array público. Esta es la variable que no encontraba el error CS0103.
    // Al ser pública y usar la clase [System.Serializable], podrás rellenar este array desde el Inspector de Unity.
    public HeatColor[] heatColors;

    /// <summary>
    /// Devuelve el color asociado a un valor de peligro específico.
    /// </summary>
    /// <param name="value">El número de trampas adyacentes (dangerValue).</param>
    /// <returns>El color correspondiente del mapa de calor.</returns>
    public Color GetColorForValue(int value)
    {
        // Recorremos la lista de colores que definiste en el Inspector.
        // Ahora 'heatColors' y 'HeatColor' existen en este contexto.
        foreach (HeatColor heatColor in heatColors)
        {
            if (heatColor.value == value)
            {
                return heatColor.color;
            }
        }
        
        // Si no se encuentra un color específico para ese valor,
        // devolvemos un color por defecto (blanco) para indicar un estado no definido.
        return Color.white;
    }
}