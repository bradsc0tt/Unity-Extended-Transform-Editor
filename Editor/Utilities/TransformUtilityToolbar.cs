using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BS3D.ExtendedTransform
{
    public class TransformUtilityToolbar : VisualElement
    {
        const string k_ActiveUtilityKey = "BS3D_ExtendedTransform_ActiveUtility";

        readonly Func<Transform[]> m_GetTargets;
        readonly VisualElement m_ButtonRow;
        readonly VisualElement m_Panel;
        readonly Dictionary<string, VisualElement> m_PanelCache = new Dictionary<string, VisualElement>();

        string m_ActiveTypeName;

        public TransformUtilityToolbar(Func<Transform[]> getTargets)
        {
            m_GetTargets = getTargets;
            AddToClassList("utility-toolbar");

            m_ButtonRow = new VisualElement();
            m_ButtonRow.AddToClassList("utility-toolbar__buttons");
            Add(m_ButtonRow);

            m_Panel = new VisualElement();
            m_Panel.AddToClassList("utility-toolbar__panel");
            Add(m_Panel);

            m_ActiveTypeName = EditorPrefs.GetString(k_ActiveUtilityKey, "");

            Rebuild();
        }

        void Rebuild()
        {
            m_ButtonRow.Clear();
            m_Panel.Clear();
            m_PanelCache.Clear();

            var enabled = TransformUtilityRegistry.GetEnabled();

            // Check if previously active utility is still enabled
            bool activeStillValid = false;
            foreach (var u in enabled)
            {
                if (u.GetType().FullName == m_ActiveTypeName)
                {
                    activeStillValid = true;
                    break;
                }
            }
            if (!activeStillValid)
                m_ActiveTypeName = "";

            foreach (var utility in enabled)
            {
                var typeName = utility.GetType().FullName;
                var btn = new Button() { text = utility.DisplayName, tooltip = utility.Tooltip };
                btn.AddToClassList("utility-toolbar__btn");

                // Capture for closure
                var capturedName = typeName;
                var capturedUtility = utility;
                btn.clicked += () => ToggleUtility(capturedName, capturedUtility);

                if (typeName == m_ActiveTypeName)
                    btn.AddToClassList("utility-toolbar__btn--active");

                m_ButtonRow.Add(btn);

                // Create and cache panel content
                var panelContent = BuildUtilityPanel(capturedUtility);
                panelContent.style.display = typeName == m_ActiveTypeName
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
                m_PanelCache[typeName] = panelContent;
                m_Panel.Add(panelContent);
            }

            // Gear button
            var gearBtn = new Button(ShowSettingsMenu) { text = "\u2699" };
            gearBtn.AddToClassList("utility-toolbar__settings");
            gearBtn.tooltip = "Utility settings";
            m_ButtonRow.Add(gearBtn);
        }

        void ToggleUtility(string typeName, TransformUtility utility)
        {
            if (m_ActiveTypeName == typeName)
            {
                // Collapse
                m_ActiveTypeName = "";
                EditorPrefs.SetString(k_ActiveUtilityKey, "");
            }
            else
            {
                m_ActiveTypeName = typeName;
                EditorPrefs.SetString(k_ActiveUtilityKey, typeName);
            }

            // Update button states and panel visibility
            int i = 0;
            var enabled = TransformUtilityRegistry.GetEnabled();
            foreach (var u in enabled)
            {
                var name = u.GetType().FullName;
                var btn = m_ButtonRow[i];
                btn.EnableInClassList("utility-toolbar__btn--active", name == m_ActiveTypeName);

                if (m_PanelCache.TryGetValue(name, out var panel))
                    panel.style.display = name == m_ActiveTypeName
                        ? DisplayStyle.Flex
                        : DisplayStyle.None;
                i++;
            }
        }

        VisualElement BuildUtilityPanel(TransformUtility utility)
        {
            var panel = new VisualElement();
            panel.AddToClassList("utilities-row");

            var settings = utility.CreateSettingsContent();
            if (settings != null)
            {
                var settingsContainer = new VisualElement();
                settingsContainer.AddToClassList("utilities-row__settings");
                settingsContainer.Add(settings);
                panel.Add(settingsContainer);
            }

            var applyBtn = new Button(() =>
            {
                var targets = m_GetTargets();
                Undo.RecordObjects(targets, utility.DisplayName);
                foreach (var t in targets)
                    utility.Apply(t);
            });
            applyBtn.text = utility.DisplayName;
            applyBtn.AddToClassList("utility-toolbar__apply-btn");
            panel.Add(applyBtn);

            return panel;
        }

        void ShowSettingsMenu()
        {
            var menu = new GenericMenu();
            var all = TransformUtilityRegistry.GetAll();
            foreach (var utility in all)
            {
                var type = utility.GetType();
                bool isEnabled = TransformUtilityRegistry.IsEnabled(type);
                var capturedType = type;
                menu.AddItem(new GUIContent(utility.DisplayName), isEnabled, () =>
                {
                    TransformUtilityRegistry.SetEnabled(capturedType, !isEnabled);
                    Rebuild();
                });
            }
            menu.ShowAsContext();
        }
    }
}
