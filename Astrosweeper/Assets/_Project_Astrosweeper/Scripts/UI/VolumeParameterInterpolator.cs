using UnityEngine.Rendering;
using System.Collections;

// [System.Serializable] es crucial para que Unity pueda guardar esta clase y sus hijas en el Inspector.
[System.Serializable]
public abstract class VolumeParameterInterpolator
{
    public bool enabled = true;

    // Cada interpolador deberá saber cómo animarse a sí mismo.
    public abstract IEnumerator Animate(VolumeProfile profile, float duration);
}