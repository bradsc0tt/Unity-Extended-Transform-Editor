using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BS3D.ExtendedTransform
{
    public class SnapToGridUtility : TransformUtility
    {
        public override string DisplayName => "Snap To Grid";
        public override string Tooltip => "Snap position, rotation, and scale to Unity's grid settings";
        public override int Order => 2;

        bool m_SnapPosition = true;
        bool m_SnapRotation = true;
        bool m_SnapScale = true;

        public override VisualElement CreateSettingsContent()
        {
            var fields = new VisualElement();
            fields.AddToClassList("utilities-row__fields");

            fields.Add(BuildSnapRow("Position", m_SnapPosition,
                new Vector3Field(),
                toggle =>
                {
                    m_SnapPosition = toggle;
                },
                field =>
                {
                    var v3 = (Vector3Field)field;
                    v3.value = new Vector3(
                        EditorPrefs.GetFloat("MoveSnapX", 1f),
                        EditorPrefs.GetFloat("MoveSnapY", 1f),
                        EditorPrefs.GetFloat("MoveSnapZ", 1f));
                    v3.RegisterValueChangedCallback(evt =>
                    {
                        EditorPrefs.SetFloat("MoveSnapX", evt.newValue.x);
                        EditorPrefs.SetFloat("MoveSnapY", evt.newValue.y);
                        EditorPrefs.SetFloat("MoveSnapZ", evt.newValue.z);
                    });
                }));

            fields.Add(BuildSnapRow("Rotation", m_SnapRotation,
                new FloatField(),
                toggle => m_SnapRotation = toggle,
                field =>
                {
                    var f = (FloatField)field;
                    f.value = EditorPrefs.GetFloat("RotationSnap", 15f);
                    f.RegisterValueChangedCallback(evt => EditorPrefs.SetFloat("RotationSnap", evt.newValue));
                }));

            fields.Add(BuildSnapRow("Scale", m_SnapScale,
                new FloatField(),
                toggle => m_SnapScale = toggle,
                field =>
                {
                    var f = (FloatField)field;
                    f.value = EditorPrefs.GetFloat("ScaleSnap", 1f);
                    f.RegisterValueChangedCallback(evt => EditorPrefs.SetFloat("ScaleSnap", evt.newValue));
                }));

            return fields;
        }

        static VisualElement BuildSnapRow(string label, bool enabled, VisualElement field,
            System.Action<bool> onToggle, System.Action<VisualElement> initField)
        {
            var row = new VisualElement();
            row.AddToClassList("snap-grid-row");

            var toggle = new Toggle { value = enabled };
            toggle.AddToClassList("snap-grid-row__toggle");

            var toggleLabel = new Label(label);
            toggleLabel.AddToClassList("snap-grid-row__label");

            var toggleGroup = new VisualElement();
            toggleGroup.AddToClassList("snap-grid-row__toggle-group");
            toggleGroup.Add(toggle);
            toggleGroup.Add(toggleLabel);
            row.Add(toggleGroup);

            field.AddToClassList("snap-grid-row__field");
            field.SetEnabled(enabled);
            initField(field);
            row.Add(field);

            toggle.RegisterValueChangedCallback(evt =>
            {
                onToggle(evt.newValue);
                field.SetEnabled(evt.newValue);
            });

            return row;
        }

        public override void Apply(Transform target)
        {
            var posSnap = m_SnapPosition
                ? new Vector3(
                    EditorPrefs.GetFloat("MoveSnapX", 1f),
                    EditorPrefs.GetFloat("MoveSnapY", 1f),
                    EditorPrefs.GetFloat("MoveSnapZ", 1f))
                : Vector3.zero;

            float rotSnap = m_SnapRotation ? EditorPrefs.GetFloat("RotationSnap", 15f) : 0f;
            float scaleSnap = m_SnapScale ? EditorPrefs.GetFloat("ScaleSnap", 1f) : 0f;

            target.SnapToGrid(posSnap, rotSnap, scaleSnap);
        }
    }
}
