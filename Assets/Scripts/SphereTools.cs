using UnityEngine;

public static class SphereTools
{
    /// </summary>
    /// <param name="gazeSphereTransform">Transform of the first sphere (gaze sphere)</param>
    /// <param name="targetSphereTransform">Transform of the second sphere (target sphere)</param>
    /// <returns>True if spheres collide, false otherwise</returns>
    public static bool AreSphereColliding(Transform gazeSphereTransform, Transform targetSphereTransform)
    {
        // Get positions of both spheres
        Vector3 gazeSpherePosition = gazeSphereTransform.position;
        Vector3 targetSpherePosition = targetSphereTransform.position;

        // Calculate combined radius (half of diameter)
        float gazeSphereRadius = gazeSphereTransform.lossyScale.x/2;
        float targetSphereRadius = targetSphereTransform.lossyScale.x/2;
        float combinedRadius = gazeSphereRadius + targetSphereRadius;

        // Calculate distance between sphere centers
        float distanceBetweenCenters = Vector3.Distance(gazeSpherePosition, targetSpherePosition);

        // Check if the distance is less than or equal to the combined radii
        return distanceBetweenCenters <= combinedRadius;
    }
}