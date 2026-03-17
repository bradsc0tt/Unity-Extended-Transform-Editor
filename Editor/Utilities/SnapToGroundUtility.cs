using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace BS3D.ExtendedTransform
{
    public class SnapToGroundUtility : TransformUtility
    {
        public override string DisplayName => "Snap To Ground";
        public override string Tooltip => "Snap selected objects to the ground below them";
        public override int Order => 0;

        static float s_SnapOffset;
        static LayerMask s_LayerMask = ~0;

        public override VisualElement CreatePanelContent(Func<Transform[]> getTargets)
        {
            var row = new VisualElement();
            row.AddToClassList("utilities-row");

            var btn = new Button(() =>
            {
                var targets = getTargets();
                int mask = s_LayerMask;
                ExecuteOnTargets(targets, "Snap to Ground", t => t.SnapToGround(s_SnapOffset, mask));
            }) { text = "Snap To Ground" };
            row.Add(btn);

            var fields = new VisualElement();
            fields.AddToClassList("utilities-row__fields");

            var offsetField = new FloatField("Offset") { value = s_SnapOffset };
            offsetField.RegisterValueChangedCallback(evt => s_SnapOffset = evt.newValue);
            fields.Add(offsetField);

            var layerField = new LayerMaskField("Layers") { value = s_LayerMask };
            layerField.RegisterValueChangedCallback(evt => s_LayerMask = evt.newValue);
            fields.Add(layerField);

            row.Add(fields);

            return row;
        }
    }
}
