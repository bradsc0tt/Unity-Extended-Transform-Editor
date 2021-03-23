using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

public static class uEditorUtils
{

    private static GUISkin guiSkin = null;
    public static GUISkin uEditorSkin
    {
        get
        {
            if (guiSkin == null)
            {
                guiSkin = Resources.Load("uEditorGUI") as GUISkin;
            }
            return guiSkin;
        }
    }
    private static Texture2D _xIcon;
    private static Texture2D xIcon
    {
        get
        {
            if (_xIcon == null) _xIcon = Resources.Load("uEditor_X") as Texture2D;
            return _xIcon;
        }
    }
    private static Texture2D _yIcon;
    private static Texture2D yIcon
    {
        get
        {
            if (_yIcon == null) _yIcon = Resources.Load("uEditor_Y") as Texture2D;
            return _yIcon;
        }
    }
    private static Texture2D _zIcon;
    private static Texture2D zIcon
    {
        get
        {
            if (_zIcon == null) _zIcon = Resources.Load("uEditor_Z") as Texture2D;
            return _zIcon;
        }
    }

    static public bool DrawHeader(string text) { return DrawHeader(text, text); }
    static public bool DrawHeader(string text, string key)
    {
        bool state = EditorPrefs.GetBool(key, true);

        GUIStyle style = uEditorSkin.button;

        GUILayout.Label("", GUILayout.Height(20));
        Rect rect = GUILayoutUtility.GetLastRect();
        rect.x = 20;
        rect.width = rect.width - 20;

        GUI.backgroundColor = state ? new Color(1f, 1f, 1f, 0.4f) : new Color(1f, 1f, 1f, 0.25f);

        if (!GUI.Toggle(rect, true, text, style))
            state = !state;

        if (GUI.changed)
            EditorPrefs.SetBool(key, state);

        GUI.backgroundColor = Color.white;
        return state;
    }

    public static Vector3 Vector3InputField(string Label, Vector3 value, float resetTo) { return Vector3InputField(Label, value, 0, false, false, false); }
    public static Vector3 Vector3InputField(string label, Vector3 value, float resetTo, bool lockX, bool lockY, bool lockZ)
    {

        Vector3 originalValue = value;
        Vector3 newValue = value;

        bool resetOnClickInRect()
        {
            var rect = GUILayoutUtility.GetLastRect();
            rect.width = 16f;
            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 1 && rect.Contains(e.mousePosition))
            {
                GUI.changed = true;
                return true;
            }
            return false;
        }

        GUIContent[] Labels = new GUIContent[3];
        Labels[0] = new GUIContent("", xIcon, "");
        Labels[1] = new GUIContent("", yIcon, "");
        Labels[2] = new GUIContent("", zIcon, "");

        GUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(label);

        EditorGUIUtility.labelWidth = 16f;

        EditorGUI.BeginChangeCheck();
        EditorGUI.BeginDisabledGroup(lockX);
        newValue.x = EditorGUILayout.FloatField(Labels[0], newValue.x);
        if (resetOnClickInRect())
            newValue.x = resetTo;

        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginDisabledGroup(lockY);
        newValue.y = EditorGUILayout.FloatField(Labels[1], newValue.y);
        if (resetOnClickInRect())
            newValue.y = resetTo;

        EditorGUI.EndDisabledGroup();
        EditorGUI.BeginDisabledGroup(lockZ);
        newValue.z = EditorGUILayout.FloatField(Labels[2], newValue.z);
        if (resetOnClickInRect())
            newValue.z = resetTo;

        EditorGUIUtility.labelWidth = 0;
        EditorGUI.EndDisabledGroup();

        if (EditorGUI.EndChangeCheck())
        {
            float difference = newValue.x / originalValue.x;
            //Debug.Log("Difference: " + difference);
            if (lockY)
            {
                newValue.y = originalValue.y * difference;
            }
            if (lockZ)
                newValue.z = originalValue.z * difference;
        }

        EditorGUIUtility.labelWidth = 0f;

        GUILayout.EndHorizontal();
        return newValue;
    }
}