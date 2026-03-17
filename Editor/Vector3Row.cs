using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace BS3D.ExtendedTransform
{
    /// <summary>
    /// A row with a label, three float fields with axis icons, and optional buttons.
    /// Axis icons support click-drag scrubbing to adjust values.
    /// </summary>
    public class Vector3Row : VisualElement
    {
        const float k_ScrubSensitivity = 0.1f;
        const float k_ScrubSlowMultiplier = 0.1f;
        const float k_ScrubFastMultiplier = 10f;

        private readonly VisualElement m_PrefixContainer;
        private readonly FloatField[] m_Fields = new FloatField[3];
        private readonly VisualElement m_SuffixContainer;

        private Vector3 m_LastValue;
        private bool m_SuppressEvent;

        // Scrub state
        private bool m_Scrubbing;
        private int m_ScrubAxis;
        private float m_ScrubStartX;
        private float m_ScrubStartValue;

        public event Action<Vector3> OnValueChanged;

        private static readonly string[] k_AxisLabels = { "X", "Y", "Z" };
        private static readonly string[] k_AxisClasses = { "axis-label--x", "axis-label--y", "axis-label--z" };

        public Vector3Row(string label)
        {
            AddToClassList("transform-row");

            var mLabel = new Label(label);
            mLabel.AddToClassList("transform-row__label");
            Add(mLabel);

            m_PrefixContainer = new VisualElement();
            m_PrefixContainer.AddToClassList("transform-row__prefix");
            Add(m_PrefixContainer);

            var mFieldsContainer = new VisualElement();
            mFieldsContainer.AddToClassList("transform-row__fields");
            Add(mFieldsContainer);

            for (int i = 0; i < 3; i++)
            {
                var field = new FloatField();

                var axisLabel = new Label(k_AxisLabels[i]);
                axisLabel.AddToClassList("axis-label");
                axisLabel.AddToClassList(k_AxisClasses[i]);
                field.Insert(0, axisLabel);

                int axis = i;

                // Scrub: pointer down on axis label starts drag
                axisLabel.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (evt.button != 0 || !m_Fields[axis].enabledSelf) return;
                    m_Scrubbing = true;
                    m_ScrubAxis = axis;
                    m_ScrubStartX = evt.position.x;
                    m_ScrubStartValue = m_LastValue[axis];
                    axisLabel.CapturePointer(evt.pointerId);
                    evt.StopPropagation();
                });

                axisLabel.RegisterCallback<PointerMoveEvent>(evt =>
                {
                    if (!m_Scrubbing || m_ScrubAxis != axis) return;
                    float delta = evt.position.x - m_ScrubStartX;
                    float sensitivity = k_ScrubSensitivity;
                    if (evt.shiftKey) sensitivity *= k_ScrubSlowMultiplier;
                    if (evt.ctrlKey) sensitivity *= k_ScrubFastMultiplier;
                    float newAxisValue = m_ScrubStartValue + delta * sensitivity;
                    newAxisValue = (float)Math.Round(newAxisValue, 4);

                    var v = m_LastValue;
                    v[axis] = newAxisValue;
                    m_LastValue = v;
                    m_SuppressEvent = true;
                    m_Fields[axis].SetValueWithoutNotify(newAxisValue);
                    m_SuppressEvent = false;
                    OnValueChanged?.Invoke(v);
                    evt.StopPropagation();
                });

                axisLabel.RegisterCallback<PointerUpEvent>(evt =>
                {
                    if (!m_Scrubbing || m_ScrubAxis != axis) return;
                    m_Scrubbing = false;
                    axisLabel.ReleasePointer(evt.pointerId);
                    evt.StopPropagation();
                });

                field.RegisterValueChangedCallback(evt =>
                {
                    if (m_SuppressEvent) return;
                    var v = m_LastValue;
                    v[axis] = evt.newValue;
                    m_LastValue = v;
                    OnValueChanged?.Invoke(v);
                });

                m_Fields[i] = field;
                mFieldsContainer.Add(field);
            }

            m_SuffixContainer = new VisualElement();
            m_SuffixContainer.AddToClassList("transform-row__suffix");
            Add(m_SuffixContainer);
        }

        public void AddPrefix(VisualElement element)
        {
            m_PrefixContainer.Add(element);
        }

        public void AddSuffix(VisualElement element)
        {
            m_SuffixContainer.Add(element);
        }

        public void SetValueWithoutNotify(Vector3 value)
        {
            m_SuppressEvent = true;
            m_LastValue = value;
            m_Fields[0].SetValueWithoutNotify(value.x);
            m_Fields[1].SetValueWithoutNotify(value.y);
            m_Fields[2].SetValueWithoutNotify(value.z);
            m_SuppressEvent = false;
        }

        public void SetAxisEnabled(int axis, bool enabled)
        {
            m_Fields[axis].SetEnabled(enabled);
        }

        public void ShowMixedValue(bool mixed)
        {
            for (int i = 0; i < 3; i++)
                m_Fields[i].showMixedValue = mixed;
        }
    }
}