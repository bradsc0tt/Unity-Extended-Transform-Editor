using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

[CanEditMultipleObjects, CustomEditor(typeof(Transform))]
public class uTransformEditor : Editor {

    private const float FIELD_WIDTH = 250; //controls the width of the input fields
    //private const bool WIDE_MODE = true; //makes our controls draw inline

    private const float POSITION_MAX = 100000.0f;

    //private static GUIContent positionGUIContent = new GUIContent(LocalString("Position"), LocalString("The local position of this Game Object relative to the parent."));
    private static GUIContent rotationGUIContent = new GUIContent(LocalString("Rotation"), LocalString("The local rotation of this Game Object relative to the parent."));
    //private static GUIContent scaleGUIContent = new GUIContent(LocalString("Scale"), LocalString("The local scaling of this Game Object relative to the parent."));

    private static string positionWarningText = LocalString("Due to floating-point precision limitations, it is recommended to bring the world coordinates of the GameObject within a smaller range.");

    private SerializedProperty positionProperty; //The position of this transform
    private SerializedProperty rotationProperty; //The rotation of this transform
    private SerializedProperty scaleProperty; //The scale of this transform

    //References to some images for our GUI
    private static Texture2D icon_revert;
    private static Texture2D icon_locked;
    private static Texture2D icon_round;
    private static Texture2D icon_unlocked;

    // Styles
    private static GUIStyle style_utilLabel;
    private static GUIStyle style_resetButton;

    public static bool UniformScaling = false; //Are we using uniform scaling mode?

    private static bool SHOW_UTILS = false; //Should we show the utilities section?
    
    // Settings
    private static float snap_offset = 0f;
    private static Vector3 minRotation;
    private static Vector3 maxRotation = new Vector3(360, 360, 360);
    private static RoundingMethod roundMethod  = RoundingMethod.Floor; 
    private const string PREFS_PATH = "utransform.prefs";

    private enum RoundingMethod
    {
        Floor,
        ToNearest,
        Ceil
    }

    [System.Serializable]
    private struct PrefsSurrogate
    {
        public float snap_offset;
        public Vector3 minRotation;
        public Vector3 maxRotation;
        public RoundingMethod roundMethod;
    }

    #region INITIALISATION

    public void OnEnable()
    {
        // restore settings
        PrefsSurrogate prefs = JsonUtility.FromJson<PrefsSurrogate>(EditorPrefs.GetString(PREFS_PATH));
        snap_offset = prefs.snap_offset;
        minRotation = prefs.minRotation;
        maxRotation = prefs.maxRotation;
        roundMethod = prefs.roundMethod;

        this.positionProperty = this.serializedObject.FindProperty("m_LocalPosition");
        this.rotationProperty = this.serializedObject.FindProperty("m_LocalRotation");
        this.scaleProperty = this.serializedObject.FindProperty("m_LocalScale");
        icon_revert = EditorGUIUtility.isProSkin ? Resources.Load("uEditor_Revert_pro") as Texture2D : Resources.Load("uEditor_Revert") as Texture2D;
        icon_locked = Resources.Load("uEditor_locked") as Texture2D;
        icon_round = Resources.Load("uEditor_round") as Texture2D;
        icon_unlocked = Resources.Load("uEditor_unlocked") as Texture2D;
        style_utilLabel = uEditorUtils.uEditorSkin.GetStyle("UtilLabel");
        style_resetButton = uEditorUtils.uEditorSkin.GetStyle("ResetButton");
        EditorApplication.update += EditorUpdate;
    }

    private void OnDisable()
    {
        PrefsSurrogate prefs = new PrefsSurrogate();
        prefs.snap_offset = snap_offset;
        prefs.minRotation = minRotation;
        prefs.maxRotation = maxRotation;
        prefs.roundMethod = roundMethod;

        EditorPrefs.SetString(PREFS_PATH, JsonUtility.ToJson(prefs));

        EditorApplication.update -= EditorUpdate;
    }

    private void EditorUpdate()
    {
        Repaint();
    }
    #endregion

    /// <summary>
    /// Draws the inspector
    /// </summary>
    public override void OnInspectorGUI()
    {
        base.serializedObject.Update();
        //Draw the inputs
        DrawPositionElement();
        DrawRotationElement();
        DrawScaleElement();

        //Draw the Utilities
        SHOW_UTILS = uEditorUtils.DrawHeader("Transform Utilities");
        if (SHOW_UTILS)
            DrawUtilities();
        //Validate the transform of this object
        if (!ValidatePosition(((Transform)this.target).position))
        {
            EditorGUILayout.HelpBox(positionWarningText, MessageType.Warning);
        }
        //Apply the settings to the object
        this.serializedObject.ApplyModifiedProperties();
        EditorGUIUtility.labelWidth = 0;
    }

    /// <summary>
    /// Draws the input for the position
    /// </summary>
    private void DrawPositionElement()
    {
        if (ThinInspectorMode)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Position");
            DrawPositionReset();
            GUILayout.EndHorizontal();
        }

        string label = ThinInspectorMode ? "" : "Position";

        GUILayout.BeginHorizontal();
        EditorGUIUtility.labelWidth = EditorGUIUtility.currentViewWidth - uTransformEditor.FIELD_WIDTH - 64; // align field to right of inspector
        this.positionProperty.vector3Value = uEditorUtils.Vector3InputField(label, this.positionProperty.vector3Value, 0);
        if (!ThinInspectorMode)
            DrawPositionReset();
        GUILayout.EndHorizontal();
        EditorGUIUtility.labelWidth = 0;
    }
    private void DrawPositionReset()
    {
        if (GUILayout.Button(new GUIContent("", icon_round, "Round"), style_resetButton, GUILayout.Width(18), GUILayout.Height(18)))
        {
            var p = this.positionProperty.vector3Value;
            switch (roundMethod)
            {
                case  RoundingMethod.Floor:
                    p.x = Mathf.Floor(p.x);
                    p.y = Mathf.Floor(p.y);
                    p.z = Mathf.Floor(p.z);
                    break;
                case RoundingMethod.ToNearest:
                    p.x = Mathf.Round(p.x);
                    p.y = Mathf.Round(p.y);
                    p.z = Mathf.Round(p.z);
                    break;
                case RoundingMethod.Ceil:
                    p.x = Mathf.Ceil(p.x);
                    p.y = Mathf.Ceil(p.y);
                    p.z = Mathf.Ceil(p.z);
                    break;
                default:
                    break;
            }

            this.positionProperty.vector3Value = p;
        }
        if (GUILayout.Button(new GUIContent("", icon_revert, "Reset this objects position"), style_resetButton, GUILayout.Width(18), GUILayout.Height(18)))
        {
            this.positionProperty.vector3Value = Vector3.zero;
        }
    }

    /// <summary>
    /// Draws the input for the rotation
    /// </summary>
    private void DrawRotationElement()
    {
        if (ThinInspectorMode)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Rotation");
            DrawRotationReset();
            GUILayout.EndHorizontal();
        }

        //Rotation layout
        GUILayout.BeginHorizontal();
        EditorGUIUtility.labelWidth = EditorGUIUtility.currentViewWidth - uTransformEditor.FIELD_WIDTH - 64; // align field to right of inspector
        this.RotationPropertyField(this.rotationProperty, ThinInspectorMode ? GUIContent.none : rotationGUIContent);
        if (!ThinInspectorMode)
            DrawRotationReset();
        GUILayout.EndHorizontal();
        EditorGUIUtility.labelWidth = 0;
    }
    private void DrawRotationReset()
    {
        if (GUILayout.Button(new GUIContent("", icon_round, "Round"), style_resetButton, GUILayout.Width(18), GUILayout.Height(18)))
        {
            var p = this.rotationProperty.quaternionValue.eulerAngles;
            switch (roundMethod)
            {
                case RoundingMethod.Floor:
                    p.x = Mathf.Floor(p.x);
                    p.y = Mathf.Floor(p.y);
                    p.z = Mathf.Floor(p.z);
                    break;
                case RoundingMethod.ToNearest:
                    p.x = Mathf.Round(p.x);
                    p.y = Mathf.Round(p.y);
                    p.z = Mathf.Round(p.z);
                    break;
                case RoundingMethod.Ceil:
                    p.x = Mathf.Ceil(p.x);
                    p.y = Mathf.Ceil(p.y);
                    p.z = Mathf.Ceil(p.z);
                    break;
                default:
                    break;
            }

            this.rotationProperty.quaternionValue = Quaternion.Euler(p);
        }
        if (GUILayout.Button(new GUIContent("", icon_revert, "Reset this objects rotation"),style_resetButton, GUILayout.Width(18), GUILayout.Height(18)))
        {
            this.rotationProperty.quaternionValue = Quaternion.identity;
        }
    }

    /// <summary>
    /// Draws the input for the scale
    /// </summary>
    private void DrawScaleElement()
    {
        if (ThinInspectorMode)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Scale");
            DrawScaleReset();
            GUILayout.EndHorizontal();
        }
        string label = ThinInspectorMode ? "" : "Scale";

        //Scale Layout
        GUILayout.BeginHorizontal();
        EditorGUIUtility.labelWidth = EditorGUIUtility.currentViewWidth - uTransformEditor.FIELD_WIDTH - 64; // align field to right of inspector
        this.scaleProperty.vector3Value = uEditorUtils.Vector3InputField(label, this.scaleProperty.vector3Value, 1f, false, UniformScaling, UniformScaling);
        if (!ThinInspectorMode)
            DrawScaleReset();
        GUILayout.EndHorizontal();
        EditorGUIUtility.labelWidth = 0;
    }
    private void DrawScaleReset()
    {
        if (GUILayout.Button(new GUIContent("", (UniformScaling ? icon_locked : icon_unlocked), (UniformScaling ? "Unlock Scale" : "Lock Scale")), uEditorUtils.uEditorSkin.GetStyle("ResetButton"), GUILayout.Width(18), GUILayout.Height(18)))
        {
            UniformScaling = !UniformScaling;
        }
        if (GUILayout.Button(new GUIContent("", icon_revert, "Reset this objects scale"), style_resetButton, GUILayout.Width(18), GUILayout.Height(18)))
        {
            this.scaleProperty.vector3Value = Vector3.one;
        }
    }

    #region UTILITIES


    private void DrawUtilities()
    {
        GUILayout.Space(5);
        //Snap to ground
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Snap To Ground", uEditorUtils.uEditorSkin.button, GUILayout.Width(ThinInspectorMode ? 100 : 160)))
        {
            foreach (var tar in this.targets)
            {
                Transform t = (Transform)tar;
                Undo.RecordObject(t, "Snap to Ground");
                t.TransformSnapToGround(snap_offset);
            }
        }
        EditorGUIUtility.labelWidth = 50f;
        snap_offset = EditorGUILayout.FloatField("Offset", snap_offset);
        if (GUILayout.Button(new GUIContent("", icon_revert, "Reset Offset"), style_resetButton, GUILayout.Width(18), GUILayout.Height(18)))
        {
            snap_offset = 0f;
        }
        GUILayout.EndHorizontal();


        //Random rotation
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Random Rotation", uEditorUtils.uEditorSkin.button, GUILayout.Width(ThinInspectorMode ? 100 : 160), GUILayout.Height(EditorGUIUtility.singleLineHeight * 2)))
        {
            foreach (var tar in this.targets)
            {
                Transform t = (Transform)tar;
                Undo.RecordObject(t, "Random Rotation");
                t.RandomiseRotation(minRotation, maxRotation);
            }
        }

        GUILayout.BeginVertical();
        GUILayout.BeginHorizontal();
        {
            minRotation = EditorGUILayout.Vector3Field(ThinInspectorMode ? "" : "Min", minRotation);
            if (GUILayout.Button(new GUIContent("", icon_revert, "Reset Rotation Min"), style_resetButton, GUILayout.Width(18), GUILayout.Height(18)))
            {
                minRotation = Vector3.zero;
            }
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(2);

        GUILayout.BeginHorizontal();
        {
            maxRotation = EditorGUILayout.Vector3Field(ThinInspectorMode ? "" : "Max", maxRotation);
            if (GUILayout.Button(new GUIContent("", icon_revert, "Reset Rotation Max"), style_resetButton, GUILayout.Width(18), GUILayout.Height(18)))
            {
                maxRotation = new Vector3(360,360,360);
            }
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(5);

        
        GUILayout.EndVertical();

        

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        {
            GUILayout.Label("Rounding Method", style_utilLabel, GUILayout.Width(ThinInspectorMode ? 100 : 160), GUILayout.Height(EditorGUIUtility.singleLineHeight));
            roundMethod = (RoundingMethod)EditorGUILayout.EnumPopup(roundMethod);
            if (GUILayout.Button(new GUIContent("", icon_revert, "Reset Rounding"), style_resetButton, GUILayout.Width(18), GUILayout.Height(18)))
            {
                roundMethod = RoundingMethod.Floor;
            }
        }
        GUILayout.EndHorizontal();
        EditorGUIUtility.labelWidth = 0;

    }
    #endregion

    /// <summary>
    /// Returns the localised version of a string
    /// </summary>
    private static string LocalString(string text)
    {
        return text;
        //return LocalizationDatabase.GetLocalizedString(text);
    }

    private static bool ThinInspectorMode
    {

        get
        {
            return EditorGUIUtility.currentViewWidth <= 300;
        }

    }

    private bool ValidatePosition(Vector3 position)
    {
        if (Mathf.Abs(position.x) > uTransformEditor.POSITION_MAX) return false;
        if (Mathf.Abs(position.y) > uTransformEditor.POSITION_MAX) return false;
        if (Mathf.Abs(position.z) > uTransformEditor.POSITION_MAX) return false;
        return true;
    }

    private void RotationPropertyField(SerializedProperty rotationProperty, GUIContent content)
    {
        Transform transform = (Transform)this.targets[0];
        Quaternion localRotation = transform.localRotation;
        foreach (UnityEngine.Object t in (UnityEngine.Object[])this.targets)
        {
            if (!SameRotation(localRotation, ((Transform)t).localRotation))
            {
                EditorGUI.showMixedValue = true;
                break;
            }
        }

        EditorGUI.BeginChangeCheck();

        Vector3 eulerAngles = uEditorUtils.Vector3InputField(content.text, localRotation.eulerAngles, 0f);
        //Vector3 eulerAngles = EditorGUILayout.Vector3Field(content, localRotation.eulerAngles);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObjects(this.targets, "Rotation Changed");
            foreach (UnityEngine.Object obj in this.targets)
            {
                Transform t = (Transform)obj;
                t.localEulerAngles = eulerAngles;
            }
            rotationProperty.serializedObject.SetIsDifferentCacheDirty();
        }

        EditorGUI.showMixedValue = false;
    }

    private bool SameRotation(Quaternion rot1, Quaternion rot2)
    {
        if (rot1.x != rot2.x) return false;
        if (rot1.y != rot2.y) return false;
        if (rot1.z != rot2.z) return false;
        if (rot1.w != rot2.w) return false;
        return true;
    }
}
