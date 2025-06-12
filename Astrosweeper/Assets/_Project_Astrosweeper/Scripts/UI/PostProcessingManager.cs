using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;
using System.Collections.Generic;

public class PostProcessingManager : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Arrastra aquí el GameObject de tu escena que contiene el Global Volume.")]
    [SerializeField] private Volume globalVolume;

    [Header("Perfiles de Animación")]
    [Tooltip("El perfil que se usará por defecto cuando no haya uno específico para el estado actual.")]
    [SerializeField] private VolumeAnimationProfile defaultProfile;
    [Tooltip("Añade aquí todos los perfiles de animación para estados específicos.")]
    [SerializeField] private List<VolumeAnimationProfile> profiles;

    private void OnEnable()
    {
        // Es más seguro suscribirse en Start para garantizar que el GameManager ya existe.
        // Lo dejamos en OnEnable pero seremos cuidadosos en OnDisable.
        GameManager.OnGameStateChanged += HandleGameStateChange;
    }

    private void OnDisable()
    {
        // Si el GameManager ya fue destruido (al cerrar el juego), no intentes desuscribirte.
        if (GameManager.Instance != null)
        {
            GameManager.OnGameStateChanged -= HandleGameStateChange;
        }
    }

    private void HandleGameStateChange(GameState newState)
    {
        if (globalVolume == null || globalVolume.profile == null) return;

        // Buscamos un perfil que coincida con el nuevo estado del juego.
        VolumeAnimationProfile targetProfile = profiles.Find(p => p.triggerState == newState);

        // Si no se encuentra un perfil específico para este estado, usamos el perfil por defecto.
        if (targetProfile == null)
        {
            targetProfile = defaultProfile;
        }

        // Si tenemos un perfil válido (ya sea específico o el por defecto), iniciamos la animación.
        if (targetProfile != null)
        {
            StopAllCoroutines();
            AnimateToProfile(targetProfile);
        }
    }

    private void AnimateToProfile(VolumeAnimationProfile profile)
    {
        // Este bucle genérico funciona con cualquier perfil, incluido el por defecto,
        // y le pide a cada interpolador que se ejecute.
        foreach (var interpolator in profile.interpolators)
        {
            if (interpolator != null && interpolator.enabled)
            {
                StartCoroutine(interpolator.Animate(globalVolume.profile, profile.transitionDuration));
            }
        }
    }
}