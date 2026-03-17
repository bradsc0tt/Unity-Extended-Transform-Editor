using UnityEngine;

namespace BS3D.ExtendedTransform
{
    public static class TransformMath
    {
        public static Vector3 WorldToLocalScale(Vector3 worldScale, Vector3 parentLossyScale)
        {
            return new Vector3(
                Mathf.Approximately(parentLossyScale.x, 0f) ? 0f : worldScale.x / parentLossyScale.x,
                Mathf.Approximately(parentLossyScale.y, 0f) ? 0f : worldScale.y / parentLossyScale.y,
                Mathf.Approximately(parentLossyScale.z, 0f) ? 0f : worldScale.z / parentLossyScale.z);
        }

        public static bool IsApproxEqual(Vector3 a, Vector3 b, float epsilon = 0.0001f)
        {
            return Mathf.Abs(a.x - b.x) < epsilon
                && Mathf.Abs(a.y - b.y) < epsilon
                && Mathf.Abs(a.z - b.z) < epsilon;
        }

        public static bool IsApproxEqual(Quaternion a, Quaternion b, float threshold = 0.01f)
        {
            return Quaternion.Angle(a, b) <= threshold;
        }
    }
}
