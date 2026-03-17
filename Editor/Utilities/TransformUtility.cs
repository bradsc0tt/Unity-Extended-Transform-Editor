using System;
using UnityEditor;
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
        /// Build the expandable panel UI. getTargets returns current selection at call time.
        /// </summary>
        public abstract VisualElement CreatePanelContent(Func<Transform[]> getTargets);

        /// <summary>
        /// Iterate targets with Undo.RecordObject, then invoke action on each.
        /// </summary>
        protected static void ExecuteOnTargets(Transform[] targets, string undoName, Action<Transform> action)
        {
            foreach (var t in targets)
            {
                Undo.RecordObject(t, undoName);
                action(t);
            }
        }
    }
}
