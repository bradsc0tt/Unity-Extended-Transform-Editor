using UnityEngine;

namespace BS3D.ExtendedTransform
{
    public enum SnapOrigin
    {
        Pivot,
        BoundsMin
    }

    public static class TransformUtilities
    {
        public static void SnapToGround(this Transform transform, float offset = 0f, int layerMask = -1,
            SnapOrigin origin = SnapOrigin.Pivot)
        {
            float bottomOffset = 0f;
            if (origin == SnapOrigin.BoundsMin)
            {
                var renderer = transform.GetComponentInChildren<Renderer>();
                if (renderer != null)
                    bottomOffset = transform.position.y - renderer.bounds.min.y;
            }

            var ray = new Ray(transform.position, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask))
            {
                transform.position = hit.point + Vector3.up * (offset + bottomOffset);
            }
        }

        public static void SnapToGrid(this Transform transform, Vector3 positionSnap, float rotationSnap, float scaleSnap)
        {
            if (positionSnap.x > 0f)
                transform.position = new Vector3(
                    Mathf.Round(transform.position.x / positionSnap.x) * positionSnap.x,
                    Mathf.Round(transform.position.y / positionSnap.y) * positionSnap.y,
                    Mathf.Round(transform.position.z / positionSnap.z) * positionSnap.z);

            if (rotationSnap > 0f)
                transform.eulerAngles = new Vector3(
                    Mathf.Round(transform.eulerAngles.x / rotationSnap) * rotationSnap,
                    Mathf.Round(transform.eulerAngles.y / rotationSnap) * rotationSnap,
                    Mathf.Round(transform.eulerAngles.z / rotationSnap) * rotationSnap);

            if (scaleSnap > 0f)
                transform.localScale = new Vector3(
                    Mathf.Round(transform.localScale.x / scaleSnap) * scaleSnap,
                    Mathf.Round(transform.localScale.y / scaleSnap) * scaleSnap,
                    Mathf.Round(transform.localScale.z / scaleSnap) * scaleSnap);
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
