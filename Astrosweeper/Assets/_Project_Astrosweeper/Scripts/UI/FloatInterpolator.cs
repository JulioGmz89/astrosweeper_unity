using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Linq;
using System.Reflection; // Asegúrate de que este using esté presente
using System;
[System.Serializable]
public class FloatInterpolator : VolumeParameterInterpolator
{
    [Tooltip("El nombre exacto de la clase del Override, ej: 'Bloom', 'DepthOfField'.")]
    public string overrideTypeName;
    [Tooltip("El nombre exacto de la propiedad a animar, ej: 'intensity', 'aperture'.")]
    public string parameterName;

    public float targetValue;

    public override IEnumerator Animate(VolumeProfile profile, float duration)
    {
        // Usamos Reflexión para encontrar el tipo del override por su nombre.
        var overrideType = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => typeof(VolumeComponent).IsAssignableFrom(t) && t.Name == overrideTypeName);

        if (overrideType == null)
        {
            Debug.LogError($"No se pudo encontrar el tipo de VolumeComponent: {overrideTypeName}");
            yield break;
        }

        if (profile.TryGet(overrideType, out VolumeComponent component))
        {
            var parameterInfo = component.GetType().GetField(parameterName);
            if (parameterInfo != null)
            {
                // --- LÓGICA CORREGIDA ---
                // Verificamos si la propiedad es cualquier tipo de VolumeParameter<float>
                if (parameterInfo.GetValue(component) is VolumeParameter<float> floatParam)
                {
                    float startValue = floatParam.value;
                    float time = 0;

                    while (time < duration)
                    {
                        float t = time / duration;
                        floatParam.value = Mathf.Lerp(startValue, targetValue, t);
                        time += Time.deltaTime;
                        yield return null;
                    }
                    floatParam.value = targetValue;
                }
                else
                {
                    Debug.LogError($"El parámetro '{parameterName}' en '{overrideTypeName}' no es de un tipo válido (VolumeParameter<float>).");
                }
            }
            else
            {
                Debug.LogError($"No se pudo encontrar el parámetro llamado '{parameterName}' en '{overrideTypeName}'.");
            }
        }
    }
}