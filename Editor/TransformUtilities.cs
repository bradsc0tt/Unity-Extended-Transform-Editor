using UnityEngine;

namespace BS3D.ExtendedTransform
{
    public static class TransformUtilities
    {
        public static void SnapToGround(this Transform transform, float offset = 0f, int layerMask = -1)
        {
            var ray = new Ray(transform.position, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask))
            {
                transform.position = hit.point + Vector3.up * offset;
            }
        }

        public static void RandomiseRotation(this Transform transform, Vector3 minRotation, Vector3 maxRotation)
        {
            var rotation = new Vector3(
                Random.Range(minRotation.x, maxRotation.x),
                Random.Range(minRotation.y, maxRotation.y),
                Random.Range(minRotation.z, maxRotation.z)
            );
            transform.eulerAngles = rotation;
        }
    }
}
