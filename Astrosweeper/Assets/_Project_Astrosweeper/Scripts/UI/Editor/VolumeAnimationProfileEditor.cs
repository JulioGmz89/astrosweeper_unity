using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection; // Requerido para inspeccionar los campos de las clases
using UnityEngine.Rendering;

[CustomEditor(typeof(VolumeAnimationProfile))]
public class VolumeAnimationProfileEditor : Editor
{
    private SerializedProperty triggerStateProp;
    private SerializedProperty transitionDurationProp;
    private SerializedProperty interpolatorsProp;

    private Type[] interpolatorTypes;
    
    // Caché para no tener que buscar los tipos y campos en cada frame
    private static Dictionary<string, List<string>> floatParamsCache = new Dictionary<string, List<string>>();
    private static string[] allOverrideTypeNames;

    private void OnEnable()
    {
        triggerStateProp = serializedObject.FindProperty("triggerState");
        transitionDurationProp = serializedObject.FindProperty("transitionDuration");
        interpolatorsProp = serializedObject.FindProperty("interpolators");

        // Buscamos todos los tipos de interpoladores que hemos creado (FloatInterpolator, etc.)
        interpolatorTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsSubclassOf(typeof(VolumeParameterInterpolator)) && !type.IsAbstract)
            .ToArray();

        // Buscamos todos los tipos de overrides de Volumen disponibles en el proyecto
        if (allOverrideTypeNames == null)
        {
            var allVolumeComponentTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(VolumeComponent).IsAssignableFrom(t) && !t.IsAbstract && t != typeof(VolumeComponent));

            allOverrideTypeNames = allVolumeComponentTypes.Select(t => t.Name).ToArray();
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(triggerStateProp);
        EditorGUILayout.PropertyField(transitionDurationProp);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Interpolators", EditorStyles.boldLabel);

        // Dibujamos cada interpolador de la lista manualmente
        for (int i = 0; i < interpolatorsProp.arraySize; i++)
        {
            SerializedProperty interpolatorProp = interpolatorsProp.GetArrayElementAtIndex(i);
            
            EditorGUILayout.BeginVertical(GUI.skin.box);
            // Mostramos el nombre del tipo de interpolador
            EditorGUILayout.LabelField(interpolatorProp.managedReferenceFullTypename.Split('.').Last(), EditorStyles.boldLabel);

            // Botón para eliminar
            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                interpolatorsProp.DeleteArrayElementAtIndex(i);
                continue; // Saltamos el resto del dibujado para este elemento
            }
            
            DrawInterpolatorFields(interpolatorProp);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        // Nuestro botón para añadir nuevos interpoladores
        if (GUILayout.Button("Add Interpolator..."))
        {
            ShowInterpolatorMenu();
        }

        serializedObject.ApplyModifiedProperties();
    }

    // Dibuja los campos específicos para cada tipo de interpolador
    private void DrawInterpolatorFields(SerializedProperty interpolatorProp)
    {
        // Esto es un ejemplo para FloatInterpolator, se puede expandir para otros tipos
        if (interpolatorProp.managedReferenceValue is FloatInterpolator)
        {
            var overrideTypeNameProp = interpolatorProp.FindPropertyRelative("overrideTypeName");
            var parameterNameProp = interpolatorProp.FindPropertyRelative("parameterName");
            var targetValueProp = interpolatorProp.FindPropertyRelative("targetValue");

            int currentTypeIndex = Array.IndexOf(allOverrideTypeNames, overrideTypeNameProp.stringValue);
            int newTypeIndex = EditorGUILayout.Popup("Override Type", currentTypeIndex, allOverrideTypeNames);

            if (newTypeIndex != currentTypeIndex)
            {
                overrideTypeNameProp.stringValue = allOverrideTypeNames[newTypeIndex];
                parameterNameProp.stringValue = ""; // Reseteamos el parámetro al cambiar de tipo
            }
            
            // Si se ha seleccionado un tipo de override, mostramos sus parámetros
            if (!string.IsNullOrEmpty(overrideTypeNameProp.stringValue))
            {
                if (!floatParamsCache.ContainsKey(overrideTypeNameProp.stringValue))
                {
                    CacheParametersForType(overrideTypeNameProp.stringValue);
                }

                List<string> parameterNames = floatParamsCache[overrideTypeNameProp.stringValue];
                int currentParamIndex = parameterNames.IndexOf(parameterNameProp.stringValue);
                int newParamIndex = EditorGUILayout.Popup("Parameter", currentParamIndex, parameterNames.ToArray());

                if (newParamIndex != currentParamIndex)
                {
                    parameterNameProp.stringValue = parameterNames[newParamIndex];
                }
            }
            
            EditorGUILayout.PropertyField(targetValueProp);
        }
        else
        {
            // Dibujar campos para otros tipos de interpoladores (Color, Vector2, etc.)
            // Por ahora, solo dibujamos los campos por defecto.
            EditorGUILayout.PropertyField(interpolatorProp, true);
        }
    }

    // Método para cachear los parámetros de un tipo para no usar Reflexión constantemente
    private void CacheParametersForType(string typeName)
    {
        var overrideType = Type.GetType($"UnityEngine.Rendering.Universal.{typeName}, Unity.RenderPipelines.Universal.Runtime") ??
                           Type.GetType($"UnityEngine.Rendering.{typeName}, UnityEngine.CoreModule");
        
        if (overrideType != null)
        {
            var fields = overrideType.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => f.FieldType.IsSubclassOf(typeof(VolumeParameter<float>)))
                .Select(f => f.Name)
                .ToList();
            floatParamsCache[typeName] = fields;
        }
        else
        {
            floatParamsCache[typeName] = new List<string>();
        }
    }

    private void ShowInterpolatorMenu()
    {
        GenericMenu menu = new GenericMenu();
        foreach (Type type in interpolatorTypes)
        {
            menu.AddItem(new GUIContent(type.Name), false, () => {
                AddInterpolator(type);
            });
        }
        menu.ShowAsContext();
    }

    private void AddInterpolator(Type interpolatorType)
    {
        var newInterpolator = Activator.CreateInstance(interpolatorType);
        interpolatorsProp.arraySize++;
        interpolatorsProp.GetArrayElementAtIndex(interpolatorsProp.arraySize - 1).managedReferenceValue = newInterpolator;
        serializedObject.ApplyModifiedProperties();
    }
}