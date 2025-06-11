// Assets/_Project_Astrosweeper/Scripts/Inventory/PlayerInventory.cs
using UnityEngine;
using System.Collections.Generic;
using System;

public class PlayerInventory : MonoBehaviour
{
    // Usamos un System.Action para notificar a otros scripts (como el Display) cuando el item cambia.
    public event Action<InventoryItem> OnSelectedItemChanged;

    [SerializeField]
    private List<InventoryItem> items = new List<InventoryItem>();
    [SerializeField]
    private int maxCapacity = 10;

    private int _currentlySelectedIndex = -1;

    public InventoryItem SelectedItem { get; private set; }
    public bool HasItems => items.Count > 0;

    private void Start()
    {
        // Asegurarnos de que el inventario tenga un item seleccionado al inicio si no está vacío.
        if (items.Count > 0)
        {
            Select(0);
        }
    }

    /// <summary>
    /// Navega por el inventario.
    /// </summary>
    /// <param name="direction">1 para la derecha, -1 para la izquierda.</param>
    public void Navigate(int direction)
    {
        if (!HasItems) return;

        int newIndex = _currentlySelectedIndex + direction;

        // Lógica para dar la vuelta al llegar al final o al principio (wrap-around)
        if (newIndex < 0)
        {
            newIndex = items.Count - 1;
        }
        else if (newIndex >= items.Count)
        {
            newIndex = 0;
        }

        Select(newIndex);
    }

    /// <summary>
    /// Selecciona un ítem por su índice y notifica a los suscriptores.
    /// </summary>
    private void Select(int index)
    {
        if (index < 0 || index >= items.Count)
        {
            SelectedItem = null;
            _currentlySelectedIndex = -1;
        }
        else
        {
            _currentlySelectedIndex = index;
            SelectedItem = items[_currentlySelectedIndex];
        }
        
        // Disparamos el evento para que la UI se actualice.
        OnSelectedItemChanged?.Invoke(SelectedItem);
    }

    public bool AddItem(InventoryItem item)
    {
        if (items.Count >= maxCapacity)
        {
            Debug.LogWarning("Inventory is full.");
            return false;
        }
        items.Add(item);
        // Si es el primer item, lo seleccionamos.
        if (_currentlySelectedIndex == -1)
        {
            Select(0);
        }
        return true;
    }
}