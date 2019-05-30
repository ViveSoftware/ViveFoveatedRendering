//========= Copyright 2019, HTC Corporation. All rights reserved. ===========

using System;
using UnityEditor;
using UnityEngine;

namespace HTC.UnityPlugin.FoveatedRendering
{
    [CustomEditor(typeof(ViveFoveatedRendering))]
    public class ViveFoveatedRenderingInspector : Editor
    {
        SerializedProperty scriptProp;
        SerializedProperty manualAdjustmentProp;
        SerializedProperty shadingPatternPresetProp;
        SerializedProperty shadingRatePresetProp;
        SerializedProperty innerRadiiProp;
        SerializedProperty middleRadiiProp;
        SerializedProperty peripheralRadiiProp;
        SerializedProperty innerShadingRateProp;
        SerializedProperty middleShadingRateProp;
        SerializedProperty peripheralShadingRateProp;

        GUIContent renderModeLabel = new GUIContent();
        void OnEnable()
        {
            if (target == null || serializedObject == null) { return; }

            scriptProp = serializedObject.FindProperty("m_Script");
            manualAdjustmentProp = serializedObject.FindProperty("manualAdjustment");
            shadingPatternPresetProp = serializedObject.FindProperty("patternPreset");
            shadingRatePresetProp = serializedObject.FindProperty("shadingRatePreset");
            innerRadiiProp = serializedObject.FindProperty("innerRegionRadii");
            middleRadiiProp = serializedObject.FindProperty("middleRegionRadii");
            peripheralRadiiProp = serializedObject.FindProperty("peripheralRegionRadii");
            innerShadingRateProp = serializedObject.FindProperty("innerShadingRate");
            middleShadingRateProp = serializedObject.FindProperty("middleShadingRate");
            peripheralShadingRateProp = serializedObject.FindProperty("peripheralShadingRate");

            renderModeLabel.text = "Render Mode";
        }

        public override void OnInspectorGUI()
        {
            if (target == null || serializedObject == null) { return; }
            
            serializedObject.Update();
            
            GUI.enabled = false;

            EditorGUILayout.PropertyField(scriptProp);
            
            GUI.enabled = true;

            EditorGUILayout.PropertyField(manualAdjustmentProp);

            if (manualAdjustmentProp.boolValue) {
                var targetObject = (ViveFoveatedRendering)target;
                PropertyEnumChange<ShadingPatternPreset>(shadingPatternPresetProp, propVal => targetObject.SetPatternPreset(propVal));
                if (targetObject.GetPatternPreset() == ShadingPatternPreset.SHADING_PATTERN_CUSTOM)
                {
                    EditorGUI.indentLevel = 1;

                    PropertyChange(innerRadiiProp, propVal => targetObject.SetRegionRadii(TargetArea.INNER, propVal.vector2Value));
                    PropertyChange(middleRadiiProp, propVal => targetObject.SetRegionRadii(TargetArea.MIDDLE, propVal.vector2Value));
                    PropertyChange(peripheralRadiiProp, propVal => targetObject.SetRegionRadii(TargetArea.PERIPHERAL, propVal.vector2Value));

                    EditorGUI.indentLevel = 0;
                }
                
                PropertyEnumChange<ShadingRatePreset>(shadingRatePresetProp, propVal => targetObject.SetShadingRatePreset(propVal));
                if (targetObject.GetShadingRatePreset() == ShadingRatePreset.SHADING_RATE_CUSTOM)
                {
                    EditorGUI.indentLevel = 1;

                    PropertyEnumChange<ShadingRate>(innerShadingRateProp, propVal => targetObject.SetShadingRate(TargetArea.INNER, propVal));
                    PropertyEnumChange<ShadingRate>(middleShadingRateProp, propVal => targetObject.SetShadingRate(TargetArea.MIDDLE, propVal));
                    PropertyEnumChange<ShadingRate>(peripheralShadingRateProp, propVal => targetObject.SetShadingRate(TargetArea.PERIPHERAL, propVal));

                    EditorGUI.indentLevel = 0;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        delegate void OnPropertyChange<T>(T propVal);
        void PropertyEnumChange<T>(SerializedProperty prop, OnPropertyChange<T> onEnumChange) {
            PropertyChange(prop, p => 
            {
                var enumStr = p.enumNames[p.enumValueIndex];
                var enumVal = (T)Enum.Parse(typeof(T), enumStr);
                onEnumChange(enumVal);
            });
        }
        void PropertyChange(SerializedProperty prop, OnPropertyChange<SerializedProperty> onPropChange)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(prop);
            if (EditorGUI.EndChangeCheck())
            {
                onPropChange(prop);
            }
        }
    }
}