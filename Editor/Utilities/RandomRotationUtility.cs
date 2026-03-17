using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace BS3D.ExtendedTransform
{
    public class RandomRotationUtility : TransformUtility
    {
        public override string DisplayName => "Random Rotation";
        public override string Tooltip => "Randomise rotation within a range";
        public override int Order => 1;

        static Vector3 s_MinRotation;
        static Vector3 s_MaxRotation = new Vector3(360, 360, 360);

        public override VisualElement CreatePanelContent(Func<Transform[]> getTargets)
        {
            var row = new VisualElement();
            row.AddToClassList("utilities-row");

            var btn = new Button(() =>
            {
                var targets = getTargets();
                ExecuteOnTargets(targets, "Random Rotation",
                    t => t.RandomiseRotation(s_MinRotation, s_MaxRotation));
            }) { text = "Random Rotation" };
            btn.AddToClassList("random-rotation-btn");
            row.Add(btn);

            var fields = new VisualElement();
            fields.AddToClassList("utilities-row__fields");

            var minField = new Vector3Field("Min") { value = s_MinRotation };
            minField.RegisterValueChangedCallback(evt => s_MinRotation = evt.newValue);
            fields.Add(minField);

            var maxField = new Vector3Field("Max") { value = s_MaxRotation };
            maxField.RegisterValueChangedCallback(evt => s_MaxRotation = evt.newValue);
            fields.Add(maxField);

            row.Add(fields);
            return row;
        }
    }
}
