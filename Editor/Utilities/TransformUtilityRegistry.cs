using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace BS3D.ExtendedTransform
{
    public static class TransformUtilityRegistry
    {
        const string k_DisabledKey = "BS3D_ExtendedTransform_DisabledUtilities";

        static List<TransformUtility> s_Utilities;

        static void EnsureInitialized()
        {
            if (s_Utilities != null)
                return;

            s_Utilities = new List<TransformUtility>();
            var types = TypeCache.GetTypesDerivedFrom<TransformUtility>();
            foreach (var type in types)
            {
                if (type.IsAbstract)
                    continue;
                try
                {
                    var instance = (TransformUtility)Activator.CreateInstance(type);
                    s_Utilities.Add(instance);
                }
                catch
                {
                    // Skip types that fail to instantiate
                }
            }

            s_Utilities.Sort((a, b) =>
            {
                int cmp = a.Order.CompareTo(b.Order);
                return cmp != 0 ? cmp : string.Compare(a.DisplayName, b.DisplayName, StringComparison.Ordinal);
            });
        }

        public static IReadOnlyList<TransformUtility> GetAll()
        {
            EnsureInitialized();
            return s_Utilities;
        }

        public static IReadOnlyList<TransformUtility> GetEnabled()
        {
            EnsureInitialized();
            return s_Utilities.Where(u => IsEnabled(u.GetType())).ToList();
        }

        public static bool IsEnabled(Type utilityType)
        {
            var disabled = EditorPrefs.GetString(k_DisabledKey, "");
            var fullName = utilityType.FullName;
            return !disabled.Split(';').Contains(fullName);
        }

        public static void SetEnabled(Type utilityType, bool enabled)
        {
            var disabled = EditorPrefs.GetString(k_DisabledKey, "");
            var set = new HashSet<string>(
                disabled.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));

            var fullName = utilityType.FullName;
            if (enabled)
                set.Remove(fullName);
            else
                set.Add(fullName);

            EditorPrefs.SetString(k_DisabledKey, string.Join(";", set));
        }
    }
}
