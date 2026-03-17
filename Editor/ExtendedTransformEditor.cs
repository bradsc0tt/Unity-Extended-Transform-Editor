using UnityEditor;
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

        private SerializedProperty m_PositionProp;
        private SerializedProperty m_RotationProp;
        private SerializedProperty m_ScaleProp;

        static bool s_UniformScale;
        static bool s_WorldSpace;

        // Cached icons
        private Texture2D m_IconRevert;
        private Texture2D m_IconLocked;
        private Texture2D m_IconUnlocked;

        // UI element references
        private Vector3Row m_PositionRow;
        private Vector3Row m_RotationRow;
        private Vector3Row m_ScaleRow;
        private Button m_PosResetBtn;
        private Button m_RotResetBtn;
        private Button m_ScaleResetBtn;
        private HelpBox m_PositionWarning;
        private Button m_LockButton;
        private VisualElement m_SpaceToggleContainer;
        private Button m_LocalBtn;
        private Button m_WorldBtn;
        private Label m_SpaceLabel;

        // Rotation euler caching to prevent drift
        private EulerCache m_LocalEulerCache;
        private EulerCache m_WorldEulerCache;

        private bool HasParent => ((Transform)target).parent != null;
        private bool IsPrefabInstance => PrefabUtility.IsPartOfPrefabInstance(target);

        private void OnEnable()
        {
            m_PositionProp = serializedObject.FindProperty("m_LocalPosition");
            m_RotationProp = serializedObject.FindProperty("m_LocalRotation");
            m_ScaleProp = serializedObject.FindProperty("m_LocalScale");

            m_IconLocked = LoadIcon("icon_locked.png");
            m_IconUnlocked = LoadIcon("icon_unlocked.png");

            bool isProSkin = EditorGUIUtility.isProSkin;
            m_IconRevert = LoadIcon(isProSkin ? "icon_revert_pro.png" : "icon_revert.png");

            var t = (Transform)target;
            m_LocalEulerCache.Set(t.localRotation, t.localEulerAngles);
        }

        static Texture2D LoadIcon(string filename)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(k_IconPath + filename);
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            var stylesheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(k_StylePath + "TransformEditorStyles.uss");
            if (stylesheet != null)
                root.styleSheets.Add(stylesheet);

            root.Add(BuildSpaceToggle());
            root.Add(BuildPositionRow());
            root.Add(BuildRotationRow());
            root.Add(BuildScaleRow());

            // Position warning
            m_PositionWarning = new HelpBox(
                "Due to floating-point precision limitations, it is recommended to bring the world coordinates of the GameObject within a smaller range.",
                HelpBoxMessageType.Warning);
            m_PositionWarning.AddToClassList("position-warning");
            m_PositionWarning.style.display = DisplayStyle.None;
            root.Add(m_PositionWarning);

            // Utility toolbar (plugin system)
            var utilityToolbar = new TransformUtilityToolbar(() => System.Array.ConvertAll(targets, o => (Transform)o));
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

        private VisualElement BuildSpaceToggle()
        {
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

            return m_SpaceToggleContainer;
        }

        private Vector3Row BuildPositionRow()
        {
            m_PositionRow = new Vector3Row("Position");
            m_PositionRow.OnValueChanged += OnPositionChanged;
            m_PosResetBtn = CreateIconButton(m_IconRevert, "Reset position", ResetPosition);
            m_PositionRow.AddPrefix(CreateSpacer());
            m_PositionRow.AddSuffix(m_PosResetBtn);
            return m_PositionRow;
        }

        private Vector3Row BuildRotationRow()
        {
            m_RotationRow = new Vector3Row("Rotation");
            m_RotationRow.OnValueChanged += OnRotationChanged;
            m_RotResetBtn = CreateIconButton(m_IconRevert, "Reset rotation", ResetRotation);
            m_RotationRow.AddPrefix(CreateSpacer());
            m_RotationRow.AddSuffix(m_RotResetBtn);
            return m_RotationRow;
        }

        private Vector3Row BuildScaleRow()
        {
            m_ScaleRow = new Vector3Row("Scale");
            m_ScaleRow.OnValueChanged += OnScaleChanged;
            m_LockButton = CreateIconButton(
                s_UniformScale ? m_IconLocked : m_IconUnlocked,
                s_UniformScale ? "Unlock scale" : "Lock scale",
                ToggleUniformScale);
            m_ScaleRow.AddPrefix(m_LockButton);
            m_ScaleResetBtn = CreateIconButton(m_IconRevert, "Reset scale", ResetScale);
            m_ScaleRow.AddSuffix(m_ScaleResetBtn);
            UpdateScaleLockState();
            return m_ScaleRow;
        }

        // --- Reset methods ---

        private void ResetPosition()
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
        }

        private void ResetRotation()
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
                m_WorldEulerCache.Set(t.rotation, t.eulerAngles);
            else
                m_LocalEulerCache.Set(t.localRotation, t.localEulerAngles);

            m_RotationProp.serializedObject.SetIsDifferentCacheDirty();
            serializedObject.Update();
            UpdateFields();
        }

        private void ResetScale()
        {
            if (s_WorldSpace && HasParent)
            {
                Undo.RecordObjects(targets, "Reset Scale");
                foreach (var obj in targets)
                {
                    var tr = (Transform)obj;
                    var parent = tr.parent;
                    if (parent)
                        tr.localScale = TransformMath.WorldToLocalScale(Vector3.one, parent.lossyScale);
                    else
                        tr.localScale = Vector3.one;
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
        }

        // --- Field updates ---

        private void UpdateFields()
        {
            serializedObject.Update();
            var t = (Transform)target;
            bool world = s_WorldSpace && HasParent;

            m_SpaceToggleContainer.style.display = HasParent ? DisplayStyle.Flex : DisplayStyle.None;

            // Position
            m_PositionRow.SetValueWithoutNotify(world ? t.position : m_PositionProp.vector3Value);

            // Rotation — cache euler to prevent quaternion->euler drift
            if (world)
                m_WorldEulerCache.TryUpdate(t.rotation);
            else
                m_LocalEulerCache.TryUpdate(t.localRotation);

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

            m_RotationRow.SetValueWithoutNotify(world ? m_WorldEulerCache.Euler : m_LocalEulerCache.Euler);
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

        // --- Change handlers ---

        private void OnPositionChanged(Vector3 newValue)
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

        private void OnRotationChanged(Vector3 newEuler)
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
                m_WorldEulerCache.Set(t.rotation, newEuler);
            else
                m_LocalEulerCache.Set(t.localRotation, newEuler);

            m_RotationProp.serializedObject.SetIsDifferentCacheDirty();
        }

        private void OnScaleChanged(Vector3 newValue)
        {
            if (s_WorldSpace && HasParent)
            {
                Undo.RecordObjects(targets, "Scale Changed");
                foreach (var obj in targets)
                {
                    var t = (Transform)obj;
                    var parent = t.parent;
                    t.localScale = parent ? TransformMath.WorldToLocalScale(newValue, parent.lossyScale) : newValue;
                }

                serializedObject.Update();
                return;
            }

            if (s_UniformScale)
            {
                var oldValue = m_ScaleProp.vector3Value;
                if (!Mathf.Approximately(newValue.x, oldValue.x) && !Mathf.Approximately(oldValue.x, 0f))
                {
                    float ratio = newValue.x / oldValue.x;
                    newValue.y = oldValue.y * ratio;
                    newValue.z = oldValue.z * ratio;
                }
                else if (!Mathf.Approximately(newValue.x, oldValue.x))
                {
                    newValue.y = newValue.x;
                    newValue.z = newValue.x;
                }
            }

            m_ScaleProp.vector3Value = newValue;
            serializedObject.ApplyModifiedProperties();
        }

        // --- UI state helpers ---

        private void SetWorldSpace(bool world)
        {
            s_WorldSpace = world;
            UpdateSpaceToggleState();
            UpdateFields();
        }

        private void UpdateSpaceToggleState()
        {
            m_LocalBtn.EnableInClassList("space-toggle__btn--active", !s_WorldSpace);
            m_WorldBtn.EnableInClassList("space-toggle__btn--active", s_WorldSpace);
            m_SpaceLabel.text = s_WorldSpace ? "World" : "Local";
        }

        private void ToggleUniformScale()
        {
            s_UniformScale = !s_UniformScale;
            UpdateScaleLockState();
        }

        private void UpdateScaleLockState()
        {
            m_ScaleRow.SetAxisEnabled(1, !s_UniformScale);
            m_ScaleRow.SetAxisEnabled(2, !s_UniformScale);

            var lockImg = m_LockButton.Q<Image>();
            if (lockImg != null)
                lockImg.image = s_UniformScale ? m_IconLocked : m_IconUnlocked;
            m_LockButton.tooltip = s_UniformScale ? "Unlock scale" : "Lock scale";
        }

        private void UpdateResetButtonVisibility(bool world, Transform t)
        {
            GetDefaultTransformValues(world, t,
                out Vector3 defaultPos, out Quaternion defaultRot, out Vector3 defaultScale,
                out Vector3 currentPos, out Quaternion currentRot, out Vector3 currentScale);

            SetButtonVisible(m_PosResetBtn, !TransformMath.IsApproxEqual(currentPos, defaultPos));
            SetButtonVisible(m_RotResetBtn, !TransformMath.IsApproxEqual(currentRot, defaultRot));
            SetButtonVisible(m_ScaleResetBtn, !TransformMath.IsApproxEqual(currentScale, defaultScale));
        }

        private static void GetDefaultTransformValues(bool world, Transform t,
            out Vector3 defaultPos, out Quaternion defaultRot, out Vector3 defaultScale,
            out Vector3 currentPos, out Quaternion currentRot, out Vector3 currentScale)
        {
            if (world)
            {
                currentPos = t.position;
                currentRot = t.rotation;
                currentScale = t.lossyScale;
                defaultPos = Vector3.zero;
                defaultRot = Quaternion.identity;
                defaultScale = Vector3.one;
            }
            else
            {
                var source = PrefabUtility.GetCorrespondingObjectFromSource(t);
                currentPos = t.localPosition;
                currentRot = t.localRotation;
                currentScale = t.localScale;
                if (source != null)
                {
                    defaultPos = source.localPosition;
                    defaultRot = source.localRotation;
                    defaultScale = source.localScale;
                }
                else
                {
                    defaultPos = Vector3.zero;
                    defaultRot = Quaternion.identity;
                    defaultScale = Vector3.one;
                }
            }
        }

        // --- Utility ---

        private static VisualElement CreateSpacer()
        {
            var spacer = new VisualElement
            {
                style =
                {
                    width = 20,
                    height = 20,
                    marginLeft = 1,
                    marginRight = 1
                }
            };
            return spacer;
        }

        private static Button CreateIconButton(Texture2D icon, string tooltip, System.Action onClick)
        {
            var btn = new Button(onClick);
            btn.AddToClassList("icon-button");
            btn.tooltip = tooltip;
            var img = new Image { image = icon };
            btn.Add(img);
            return btn;
        }

        private static void SetButtonVisible(VisualElement element, bool visible)
        {
            element.style.visibility = visible ? Visibility.Visible : Visibility.Hidden;
        }
    }
}