using UnityEngine;

// La clase que define la asociación entre un valor y un color.
// System.Serializable permite que la editemos en el Inspector.
[System.Serializable]
public class HeatColor
{
    public int value;

    // --- ¡AQUÍ ESTÁ EL CAMBIO! ---
    // Este atributo le dice a Unity que use el selector de color HDR para esta variable.
    // El primer 'true' muestra el canal Alfa, el segundo 'true' activa el modo HDR.
    [ColorUsage(true, true)]
    public Color color;
}

public class HeatMapController : MonoBehaviour
{
    // El array público que rellenarás en el Inspector con tus colores HDR.
    public HeatColor[] heatColors;

    /// <summary>
    /// Devuelve el color asociado a un valor de peligro específico.
    /// </summary>
    /// <param name="value">El número de trampas adyacentes.</param>
    /// <returns>El color correspondiente del mapa de calor.</returns>
    public Color GetColorForValue(int value)
    {
        foreach (HeatColor heatColor in heatColors)
        {
            if (heatColor.value == value)
            {
                return heatColor.color;
            }
        }
        
        // Devolvemos un color por defecto si no se encuentra el valor.
        return Color.white;
    }
}