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
        static SnapOrigin s_Origin = SnapOrigin.Pivot;

        public override VisualElement CreateSettingsContent()
        {
            var fields = new VisualElement();
            fields.AddToClassList("utilities-row__fields");

            var originField = new EnumField("Origin", s_Origin);
            originField.RegisterValueChangedCallback(evt => s_Origin = (SnapOrigin)evt.newValue);
            fields.Add(originField);

            var offsetField = new FloatField("Offset") { value = s_SnapOffset };
            offsetField.RegisterValueChangedCallback(evt => s_SnapOffset = evt.newValue);
            fields.Add(offsetField);

            var layerField = new LayerMaskField("Layers") { value = s_LayerMask };
            layerField.RegisterValueChangedCallback(evt => s_LayerMask = evt.newValue);
            fields.Add(layerField);

            return fields;
        }

        public override void Apply(Transform target)
        {
            target.SnapToGround(s_SnapOffset, s_LayerMask, s_Origin);
        }
    }
}
