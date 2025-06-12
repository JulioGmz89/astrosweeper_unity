using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewVolumeAnimationProfile", menuName = "Astrosweeper/Volume Animation Profile")]
public class VolumeAnimationProfile : ScriptableObject
{
    [Header("Configuración del Disparador")]
    public GameState triggerState;
    public float transitionDuration = 1.0f;

    // Gracias a [SerializeReference], esta lista puede contener diferentes tipos de
    // interpoladores (Float, Color, etc.) y se mostrará con un botón "Add" en el Inspector.
    [SerializeReference]
    public List<VolumeParameterInterpolator> interpolators = new List<VolumeParameterInterpolator>();
}