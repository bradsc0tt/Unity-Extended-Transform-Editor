using UnityEngine;
using UnityEngine.UIElements;

namespace BS3D.ExtendedTransform
{
    public abstract class TransformUtility
    {
        public abstract string DisplayName { get; }
        public virtual string Tooltip => DisplayName;
        public virtual int Order => 0;

        /// <summary>
        /// Optional settings UI (parameter fields, checkboxes, etc.).
        /// The toolbar adds an Apply button automatically — don't create one here.
        /// </summary>
        public virtual VisualElement CreateSettingsContent() => null;

        /// <summary>
        /// Apply this utility to a single transform.
        /// Called once per selected object. Undo is recorded automatically.
        /// </summary>
        public abstract void Apply(Transform target);
    }
}
