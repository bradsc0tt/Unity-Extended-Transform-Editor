using UnityEngine;

namespace BS3D.ExtendedTransform
{
    struct EulerCache
    {
        public Quaternion Quaternion { get; private set; }
        public Vector3 Euler { get; private set; }

        /// <summary>
        /// Updates the cached euler angles only if the quaternion has changed,
        /// preventing representation drift from repeated quaternion-to-euler conversions.
        /// </summary>
        public bool TryUpdate(Quaternion current)
        {
            if (current == Quaternion)
                return false;

            Quaternion = current;
            Euler = current.eulerAngles;
            return true;
        }

        /// <summary>
        /// Stores exact euler angles (e.g. user-typed values) alongside their quaternion.
        /// </summary>
        public void Set(Quaternion q, Vector3 euler)
        {
            Quaternion = q;
            Euler = euler;
        }
    }
}
