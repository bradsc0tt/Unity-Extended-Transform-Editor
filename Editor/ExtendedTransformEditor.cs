using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace BS3D.ExtendedTransform
{
    [CustomEditor(typeof(Transform)), CanEditMultipleObjects]
    public class ExtendedTransformEditor : Editor
    {
        const float k_ThinModeThreshold = 300f;
        const float k_PositionMax = 100000f;
        const string k_PackagePath = "Packages/com.bs3d.extended-transform/Editor/";
        const string k_IconPath = k_PackagePath + "Icons/";
        const string k_StylePath = k_PackagePath + "UI/";

        SerializedProperty m_PositionProp;
        SerializedProperty m_RotationProp;
        SerializedProperty m_ScaleProp;

        static bool s_UniformScale;
        static bool s_WorldSpace;

        // Cached icons
        Texture2D m_IconRevert;
        Texture2D m_IconLocked;
        Texture2D m_IconUnlocked;

        // UI element references
        Vector3Row m_PositionRow;
        Vector3Row m_RotationRow;
        Vector3Row m_ScaleRow;
        Button m_PosResetBtn;
        Button m_RotResetBtn;
        Button m_ScaleResetBtn;
        HelpBox m_PositionWarning;
        Button m_LockButton;
        VisualElement m_SpaceToggleContainer;
        Button m_LocalBtn;
        Button m_WorldBtn;
        Label m_SpaceLabel;

        // Rotation euler caching to prevent drift
        Quaternion m_CachedRotation;
        Vector3 m_CachedEuler;
        Quaternion m_CachedWorldRotation;
        Vector3 m_CachedWorldEuler;

        bool HasParent => ((Transform)target).parent != null;
        bool IsPrefabInstance => PrefabUtility.IsPartOfPrefabInstance(target);

        void OnEnable()
        {
            m_PositionProp = serializedObject.FindProperty("m_LocalPosition");
            m_RotationProp = serializedObject.FindProperty("m_LocalRotation");
            m_ScaleProp = serializedObject.FindProperty("m_LocalScale");

            m_IconLocked = LoadIcon("icon_locked.png");
            m_IconUnlocked = LoadIcon("icon_unlocked.png");

            bool isProSkin = EditorGUIUtility.isProSkin;
            m_IconRevert = LoadIcon(isProSkin ? "icon_revert_pro.png" : "icon_revert.png");

            var t = (Transform)target;
            m_CachedRotation = t.localRotation;
            m_CachedEuler = t.localEulerAngles;
        }

        static Texture2D LoadIcon(string filename)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(k_IconPath + filename);
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            // Load stylesheet
            var stylesheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(k_StylePath + "TransformEditorStyles.uss");
            if (stylesheet != null)
                root.styleSheets.Add(stylesheet);

            // World/local space toggle buttons — only visible when target has a parent
            m_SpaceToggleContainer = new VisualElement();
            m_SpaceToggleContainer.AddToClassList("space-toggle");
            m_SpaceToggleContainer.style.display = HasParent ? DisplayStyle.Flex : DisplayStyle.None;

            var localIcon = EditorGUIUtility.IconContent("ToolHandleLocal").image;
            var worldIcon = EditorGUIUtility.IconContent("ToolHandleGlobal").image;

            m_LocalBtn = new Button(() => SetWorldSpace(false)) { tooltip = "Local space" };
            m_LocalBtn.AddToClassList("space-toggle__btn");
            m_LocalBtn.AddToClassList("space-toggle__btn--left");
            m_LocalBtn.Add(new Image { image = localIcon });

            m_WorldBtn = new Button(() => SetWorldSpace(true)) { tooltip = "World space" };
            m_WorldBtn.AddToClassList("space-toggle__btn");
            m_WorldBtn.AddToClassList("space-toggle__btn--right");
            m_WorldBtn.Add(new Image { image = worldIcon });

            m_SpaceLabel = new Label();
            m_SpaceLabel.AddToClassList("space-toggle__label");

            m_SpaceToggleContainer.Add(m_LocalBtn);
            m_SpaceToggleContainer.Add(m_WorldBtn);
            m_SpaceToggleContainer.Add(m_SpaceLabel);
            UpdateSpaceToggleState();
            root.Add(m_SpaceToggleContainer);

            // Position row
            m_PositionRow = new Vector3Row("Position");
            m_PositionRow.OnValueChanged += OnPositionChanged;
            m_PosResetBtn = CreateIconButton(m_IconRevert, "Reset position", () =>
            {
                if (s_WorldSpace && HasParent)
                {
                    Undo.RecordObjects(targets, "Reset Position");
                    foreach (var obj in targets)
                        ((Transform)obj).position = Vector3.zero;
                    serializedObject.Update();
                }
                else if (IsPrefabInstance)
                {
                    PrefabUtility.RevertPropertyOverride(m_PositionProp, InteractionMode.UserAction);
                    serializedObject.Update();
                }
                else
                {
                    m_PositionProp.vector3Value = Vector3.zero;
                    serializedObject.ApplyModifiedProperties();
                }
                UpdateFields();
            });
            m_PositionRow.AddPrefix(CreateSpacer());
            m_PositionRow.AddSuffix(m_PosResetBtn);
            root.Add(m_PositionRow);

            // Rotation row
            m_RotationRow = new Vector3Row("Rotation");
            m_RotationRow.OnValueChanged += OnRotationChanged;
            m_RotResetBtn = CreateIconButton(m_IconRevert, "Reset rotation", () =>
            {
                bool world = s_WorldSpace && HasParent;
                if (!world && IsPrefabInstance)
                {
                    PrefabUtility.RevertPropertyOverride(m_RotationProp, InteractionMode.UserAction);
                    serializedObject.Update();
                }
                else
                {
                    Undo.RecordObjects(targets, "Reset Rotation");
                    foreach (var obj in targets)
                    {
                        var tr = (Transform)obj;
                        if (world)
                            tr.rotation = Quaternion.identity;
                        else
                            tr.localRotation = Quaternion.identity;
                    }
                }
                var t = (Transform)target;
                if (world)
                {
                    m_CachedWorldRotation = t.rotation;
                    m_CachedWorldEuler = t.eulerAngles;
                }
                else
                {
                    m_CachedRotation = t.localRotation;
                    m_CachedEuler = t.localEulerAngles;
                }
                m_RotationProp.serializedObject.SetIsDifferentCacheDirty();
                serializedObject.Update();
                UpdateFields();
            });
            m_RotationRow.AddPrefix(CreateSpacer());
            m_RotationRow.AddSuffix(m_RotResetBtn);
            root.Add(m_RotationRow);

            // Scale row
            m_ScaleRow = new Vector3Row("Scale");
            m_ScaleRow.OnValueChanged += OnScaleChanged;
            m_LockButton = CreateIconButton(
                s_UniformScale ? m_IconLocked : m_IconUnlocked,
                s_UniformScale ? "Unlock scale" : "Lock scale",
                ToggleUniformScale);
            m_ScaleRow.AddPrefix(m_LockButton);
            m_ScaleResetBtn = CreateIconButton(m_IconRevert, "Reset scale", () =>
            {
                if (s_WorldSpace && HasParent)
                {
                    Undo.RecordObjects(targets, "Reset Scale");
                    foreach (var obj in targets)
                    {
                        var tr = (Transform)obj;
                        var parent = tr.parent;
                        if (parent != null)
                        {
                            var ps = parent.lossyScale;
                            tr.localScale = new Vector3(
                                Mathf.Approximately(ps.x, 0f) ? 0f : 1f / ps.x,
                                Mathf.Approximately(ps.y, 0f) ? 0f : 1f / ps.y,
                                Mathf.Approximately(ps.z, 0f) ? 0f : 1f / ps.z);
                        }
                        else
                        {
                            tr.localScale = Vector3.one;
                        }
                    }
                    serializedObject.Update();
                }
                else if (IsPrefabInstance)
                {
                    PrefabUtility.RevertPropertyOverride(m_ScaleProp, InteractionMode.UserAction);
                    serializedObject.Update();
                }
                else
                {
                    m_ScaleProp.vector3Value = Vector3.one;
                    serializedObject.ApplyModifiedProperties();
                }
                UpdateFields();
            });
            m_ScaleRow.AddSuffix(m_ScaleResetBtn);
            UpdateScaleLockState();
            root.Add(m_ScaleRow);

            // Position warning
            m_PositionWarning = new HelpBox(
                "Due to floating-point precision limitations, it is recommended to bring the world coordinates of the GameObject within a smaller range.",
                HelpBoxMessageType.Warning);
            m_PositionWarning.AddToClassList("position-warning");
            m_PositionWarning.style.display = DisplayStyle.None;
            root.Add(m_PositionWarning);

            // Utility toolbar (plugin system)
            var utilityToolbar = new TransformUtilityToolbar(
                () => System.Array.ConvertAll(targets, o => (Transform)o));
            root.Add(utilityToolbar);

            // Thin mode responsive layout
            root.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                bool thin = evt.newRect.width <= k_ThinModeThreshold;
                root.EnableInClassList("thin-mode", thin);
            });

            // Periodic update to sync UI from serialized data
            root.schedule.Execute(UpdateFields).Every(100);

            return root;
        }

        void UpdateFields()
        {
            serializedObject.Update();
            var t = (Transform)target;
            bool world = s_WorldSpace && HasParent;

            // Toggle visibility
            m_SpaceToggleContainer.style.display = HasParent ? DisplayStyle.Flex : DisplayStyle.None;

            // Position
            m_PositionRow.SetValueWithoutNotify(world ? t.position : m_PositionProp.vector3Value);

            // Rotation — cache euler to prevent quaternion->euler drift
            if (world)
            {
                if (t.rotation != m_CachedWorldRotation)
                {
                    m_CachedWorldRotation = t.rotation;
                    m_CachedWorldEuler = t.eulerAngles;
                }
            }
            else
            {
                if (t.localRotation != m_CachedRotation)
                {
                    m_CachedRotation = t.localRotation;
                    m_CachedEuler = t.localEulerAngles;
                }
            }

            bool mixedRotation = false;
            if (targets.Length > 1)
            {
                var refRot = world
                    ? ((Transform)targets[0]).rotation
                    : ((Transform)targets[0]).localRotation;
                for (int i = 1; i < targets.Length; i++)
                {
                    var rot = world
                        ? ((Transform)targets[i]).rotation
                        : ((Transform)targets[i]).localRotation;
                    if (rot != refRot)
                    {
                        mixedRotation = true;
                        break;
                    }
                }
            }
            m_RotationRow.SetValueWithoutNotify(world ? m_CachedWorldEuler : m_CachedEuler);
            m_RotationRow.ShowMixedValue(mixedRotation);

            // Scale
            m_ScaleRow.SetValueWithoutNotify(world ? t.lossyScale : m_ScaleProp.vector3Value);

            // Position warning
            var worldPos = t.position;
            bool outOfRange = Mathf.Abs(worldPos.x) > k_PositionMax
                           || Mathf.Abs(worldPos.y) > k_PositionMax
                           || Mathf.Abs(worldPos.z) > k_PositionMax;
            m_PositionWarning.style.display = outOfRange ? DisplayStyle.Flex : DisplayStyle.None;

            // Auto-hide reset buttons when already at default
            UpdateResetButtonVisibility(world, t);
        }

        void OnPositionChanged(Vector3 newValue)
        {
            if (s_WorldSpace && HasParent)
            {
                Undo.RecordObjects(targets, "Position Changed");
                foreach (var obj in targets)
                    ((Transform)obj).position = newValue;
                serializedObject.Update();
            }
            else
            {
                m_PositionProp.vector3Value = newValue;
                serializedObject.ApplyModifiedProperties();
            }
        }

        void OnRotationChanged(Vector3 newEuler)
        {
            Undo.RecordObjects(targets, "Rotation Changed");
            bool world = s_WorldSpace && HasParent;
            foreach (var obj in targets)
            {
                if (world)
                    ((Transform)obj).eulerAngles = newEuler;
                else
                    ((Transform)obj).localEulerAngles = newEuler;
            }

            var t = (Transform)target;
            if (world)
            {
                m_CachedWorldRotation = t.rotation;
                m_CachedWorldEuler = newEuler;
            }
            else
            {
                m_CachedRotation = t.localRotation;
                m_CachedEuler = newEuler;
            }
            m_RotationProp.serializedObject.SetIsDifferentCacheDirty();
        }

        void OnScaleChanged(Vector3 newValue)
        {
            if (s_WorldSpace && HasParent)
            {
                Undo.RecordObjects(targets, "Scale Changed");
                foreach (var obj in targets)
                {
                    var t = (Transform)obj;
                    var parent = t.parent;
                    if (parent != null)
                    {
                        var parentScale = parent.lossyScale;
                        t.localScale = new Vector3(
                            Mathf.Approximately(parentScale.x, 0f) ? 0f : newValue.x / parentScale.x,
                            Mathf.Approximately(parentScale.y, 0f) ? 0f : newValue.y / parentScale.y,
                            Mathf.Approximately(parentScale.z, 0f) ? 0f : newValue.z / parentScale.z);
                    }
                    else
                    {
                        t.localScale = newValue;
                    }
                }
                serializedObject.Update();
                return;
            }

            if (s_UniformScale)
            {
                var oldValue = m_ScaleProp.vector3Value;
                // Determine which axis changed by comparing to old value
                float ratio;
                if (!Mathf.Approximately(newValue.x, oldValue.x) && !Mathf.Approximately(oldValue.x, 0f))
                {
                    ratio = newValue.x / oldValue.x;
                    newValue.y = oldValue.y * ratio;
                    newValue.z = oldValue.z * ratio;
                }
                else if (!Mathf.Approximately(newValue.x, oldValue.x))
                {
                    // oldValue.x was ~0, just set all to new X
                    newValue.y = newValue.x;
                    newValue.z = newValue.x;
                }
            }
            m_ScaleProp.vector3Value = newValue;
            serializedObject.ApplyModifiedProperties();
        }

        void SetWorldSpace(bool world)
        {
            s_WorldSpace = world;
            UpdateSpaceToggleState();
            UpdateFields();
        }

        void UpdateSpaceToggleState()
        {
            m_LocalBtn.EnableInClassList("space-toggle__btn--active", !s_WorldSpace);
            m_WorldBtn.EnableInClassList("space-toggle__btn--active", s_WorldSpace);
            m_SpaceLabel.text = s_WorldSpace ? "World" : "Local";
        }

        void ToggleUniformScale()
        {
            s_UniformScale = !s_UniformScale;
            UpdateScaleLockState();
        }

        void UpdateScaleLockState()
        {
            m_ScaleRow.SetAxisEnabled(1, !s_UniformScale);
            m_ScaleRow.SetAxisEnabled(2, !s_UniformScale);

            var lockImg = m_LockButton.Q<Image>();
            if (lockImg != null)
                lockImg.image = s_UniformScale ? m_IconLocked : m_IconUnlocked;
            m_LockButton.tooltip = s_UniformScale ? "Unlock scale" : "Lock scale";
        }

        /// <summary>
        /// Creates an invisible spacer the same size as the lock button to keep rows aligned.
        /// </summary>
        static VisualElement CreateSpacer()
        {
            var spacer = new VisualElement();
            spacer.style.width = 20;
            spacer.style.height = 20;
            spacer.style.marginLeft = 1;
            spacer.style.marginRight = 1;
            return spacer;
        }

        Button CreateIconButton(Texture2D icon, string tooltip, System.Action onClick)
        {
            var btn = new Button(onClick);
            btn.AddToClassList("icon-button");
            btn.tooltip = tooltip;
            var img = new Image { image = icon };
            btn.Add(img);
            return btn;
        }

        void UpdateResetButtonVisibility(bool world, Transform t)
        {
            bool posAtDefault, rotAtDefault, scaleAtDefault;

            if (world)
            {
                posAtDefault = IsApproxEqual(t.position, Vector3.zero);
                rotAtDefault = Quaternion.Angle(t.rotation, Quaternion.identity) <= 0.01f;
                scaleAtDefault = IsApproxEqual(t.lossyScale, Vector3.one);
            }
            else
            {
                var source = PrefabUtility.GetCorrespondingObjectFromSource(t);
                if (source != null)
                {
                    posAtDefault = IsApproxEqual(t.localPosition, source.localPosition);
                    rotAtDefault = Quaternion.Angle(t.localRotation, source.localRotation) <= 0.01f;
                    scaleAtDefault = IsApproxEqual(t.localScale, source.localScale);
                }
                else
                {
                    posAtDefault = IsApproxEqual(m_PositionProp.vector3Value, Vector3.zero);
                    rotAtDefault = Quaternion.Angle(t.localRotation, Quaternion.identity) <= 0.01f;
                    scaleAtDefault = IsApproxEqual(m_ScaleProp.vector3Value, Vector3.one);
                }
            }

            SetButtonVisible(m_PosResetBtn, !posAtDefault);
            SetButtonVisible(m_RotResetBtn, !rotAtDefault);
            SetButtonVisible(m_ScaleResetBtn, !scaleAtDefault);
        }

        static void SetButtonVisible(VisualElement element, bool visible)
        {
            element.style.visibility = visible ? Visibility.Visible : Visibility.Hidden;
        }

        static bool IsApproxEqual(Vector3 a, Vector3 b, float epsilon = 0.0001f)
        {
            return Mathf.Abs(a.x - b.x) < epsilon
                && Mathf.Abs(a.y - b.y) < epsilon
                && Mathf.Abs(a.z - b.z) < epsilon;
        }

    }
}
