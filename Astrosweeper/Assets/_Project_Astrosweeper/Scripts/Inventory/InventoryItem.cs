using UnityEngine;

/// <summary>
/// Define un objeto que puede ser almacenado en el inventario.
/// Usamos un ScriptableObject para poder crear y configurar items desde el Editor de Unity.
/// </summary>
[CreateAssetMenu(fileName = "NewInventoryItem", menuName = "Astrosweeper/Inventory Item")]
public class InventoryItem : ScriptableObject
{
    [Tooltip("El nombre del objeto que podría mostrarse en un futuro")]
    public string itemName;

    [Tooltip("Prefab del modelo 3D que se mostrará en la UI del inventario en TileSelection mode")]
    public GameObject displayModel;

    // Futura expansión: Aquí podrías añadir una referencia al efecto que causa el item al usarse sobre un tile.
    // public TileEffect itemEffect;
}