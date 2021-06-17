//
// Copyright (C) Microsoft. All rights reserved.
//

using UnityEngine;
using System;

namespace SensorsSDK.UnityUtilities
{
    /// <summary>
    /// Holoshop Mathf library extension.
    /// </summary>
    public static class MathfExtension
    {
        public const float SmallNumber = 0.0000001f;

        public static double LerpD(double from, double to, float f)
        {
            return from + ((to - from) * f);
        }

        public static double ClampD(double val, double min, double max)
        {
            return Math.Min(Math.Max(val, min), max);
        }

        /// <summary>
        /// Projects the two vectors onto a plane, defined by the normal, before finding the signed
        /// angle between them. If either are degenerate after the projection (length 0), then 0 is
        /// returned by definition.  Note that this function assumes a plane that intersects the
        /// origin. It is also significantly slower than the version that takes an axis index, so if
        /// you're using a normal aligned with a major axis, use the other function instead.
        /// </summary>
        public static float SignedProjectedAngle(Vector3 from, Vector3 to, Vector3 normal)
        {
            // Project onto a plane.
            from = from - Vector3.Dot(from, normal) * normal;
            to = to - Vector3.Dot(to, normal) * normal;

            // If either case is degenerate, the projected angle is 0.
            if (from.sqrMagnitude < MathfExtension.SmallNumber || to.sqrMagnitude < MathfExtension.SmallNumber)
            {
                return 0;
            }

            return Vector3.SignedAngle(from, to, normal);
        }

        /// <summary>
        /// Projects the two vectors onto a plane defined by a primary axis, before finding the
        /// signed angle between them. If either are degenerate after the projection (length 0),
        /// then 0 is returned by definition.  Note that this function assumes a plane that
        /// intersects the origin. It is also significantly faster than the general version. If
        /// you can use this one, do so.
        /// </summary>
        public static float SignedProjectedAngle(Vector3 from, Vector3 to, int axisIndex)
        {
            // Project onto a plane.
            from[axisIndex] = 0;
            to[axisIndex] = 0;

            // If either case is degenerate, the projected angle is 0.
            if (from.sqrMagnitude < MathfExtension.SmallNumber || to.sqrMagnitude < MathfExtension.SmallNumber)
            {
                return 0;
            }

            Vector3 axis = Vector3.zero;
            axis[axisIndex] = 1;

            return Vector3.SignedAngle(from, to, axis);
        }

        /// <summary>
        /// Prevents current from being clampValue degrees away from basis on the plane defined by the planeNormalAxis.
        /// </summary>
        /// <param name="basis">The vector around which to clamp. 'current' will be clamped relative to this angle based on the clampAngle.</param>
        /// <param name="current">The vector that will be clamped.</param>
        /// <param name="clampValue">After projecting onto a plane, current will be clamped to be no more than clampValue degrees away from basis.</param>
        /// <param name="planeNormalAxis">The index of the major axis that defines the plane to project onto. A general version of this will work but will be more expensive.</param>
        /// <returns></returns>
        public static Vector3 ClampVectorByAngleOnPlane(Vector3 basis, Vector3 current, float clampValue, int planeNormalAxis)
        {
            float delta = MathfExtension.SignedProjectedAngle(basis, current, planeNormalAxis);
            if (Mathf.Abs(delta) > clampValue + 0.0001f)
            {
                Vector3 euler = Vector3.zero;
                euler[planeNormalAxis] = (Mathf.Sign(delta) * clampValue) - delta;
                current = Quaternion.Euler(euler) * current;
            }

            return current;
        }

        /// <summary>
        /// Prevents current from being clampValue degrees away from basis on the plane defined by the planeNormalAxis.
        /// </summary>
        /// <param name="basis">The vector around which to clamp. 'current' will be clamped relative to this angle based on the clampAngle.</param>
        /// <param name="current">The vector that will be clamped.</param>
        /// <param name="clampValue">After projecting onto a plane, current will be clamped to be no more than clampValue degrees away from basis.</param>
        /// <param name="normal">Normal that defines the plane to project onto.</param>
        /// <returns></returns>
        public static Vector3 ClampVectorByAngleOnPlane(Vector3 basis, Vector3 current, float clampValue, Vector3 normal)
        {
            float delta = MathfExtension.SignedProjectedAngle(basis, current, normal);
            if (Mathf.Abs(delta) > clampValue + 0.0001f)
            {
                current = Quaternion.AngleAxis((Mathf.Sign(delta) * clampValue) - delta, normal) * current;
            }
            return current;
        }

        /// <summary>
        /// Projects a point on to a Plane and returns the projected point.
        /// </summary>
        /// <param name="plane"></param>
        /// <param name="point"></param>
        /// <returns>Projected point on the plane</returns>
        public static Vector3 ProjectPointToPlane(Vector3 point, Plane plane)
        {
            //Find the distance from point to the plane.
            float pointToPlaneDistance = plane.GetDistanceToPoint(point);

            // Remove the distance along the plane normal
            return point - pointToPlaneDistance * plane.normal;
        }

        /// <summary>
        /// Intersects the Ray with the Plane and outputs the intersection point.
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="plane"></param>
        /// <param name="point"></param>
        /// <returns>True if there is an intersection, false if the ray is parallel to the plane.</returns>
        public static bool IntersectRayPlane(Ray ray, Plane plane, out Vector3 point)
        {
            float intersectionDistance;

            // Find the intersection of ray and plane
            if (plane.Raycast(ray, out intersectionDistance))
            {
                // Get the point along the ray.
                point = ray.GetPoint(intersectionDistance);
                return true;
            }

            // Ray is parallel to the plane, set a dummy value to the point.
            point = Vector3.zero;
            return false;
        }

        /// <summary>
        /// Calculates the intersection between a ray and a triangle, returning false if they don't
        /// intersect.  If they do, we return true and set the value of 'distance' such that
        /// "ray.origin + ray.direction*distance" is a point on the input triangle
        /// Based on Moller and Trumbore's "Fast, Minimum Storage Ray/Triangle Intersection"
        /// </summary>
        public static bool IntersectRayTriangle(Ray ray, Vector3 v0, Vector3 v1, Vector3 v2, out float distance)
        {
            // This epsilon value is used to eliminate rays parallel to the triangle. This value is
            //  the one recommended by Moller and Trumbore.
            const float epsilon = 1e-06f;
            Vector3 edge1 = v1 - v0;
            Vector3 edge2 = v2 - v0;

            // Calculate the determinant -- if it is near zero, the ray is in the plane of the
            //  triangle
            Vector3 pvec = Vector3.Cross(ray.direction, edge2);
            float det = Vector3.Dot(edge1, pvec);
            if ((det > -epsilon) && (det < epsilon))
            {
                distance = -1.0f;
                return false;
            }

            float invDet = 1.0f / det;

            // Calculate 'u' parameter and test bounds
            Vector3 tvec = ray.origin - v0;
            float u = Vector3.Dot(tvec, pvec) * invDet;
            if ((u < 0.0f) || (u > 1.0f))
            {
                distance = -1.0f;
                return false;
            }

            // Calculate 'v' parameter and test bounds
            Vector3 qvec = Vector3.Cross(tvec, edge1);
            float v = Vector3.Dot(ray.direction, qvec) * invDet;
            if ((v < 0.0f) || (u + v > 1.0f))
            {
                distance = -1.0f;
                return false;
            }

            // Calculate distance; ray intersects triangle
            distance = Vector3.Dot(edge2, qvec) * invDet;
            return true;
        }

        public static Vector3 FindXZYConstrainedForwardVector(Matrix4x4 transform, float epsilon = 0.00001f)
        {
            Vector3 forward = transform.GetColumn(2);
            bool tiltedUp = (forward.y > 0);

            // Constrain to the XZ plane.
            forward.y = 0;

            // Make sure we were not looking straight up or straight down.
            float sqrLength = forward.sqrMagnitude;
            if (sqrLength > epsilon)
            {
                return (forward / Mathf.Sqrt(sqrLength));
            }

            // If the user was looking straight up, use the down vector. If the user was looking straight up, use the up vector.
            forward = transform.GetColumn(1) * (tiltedUp ? -1 : 1);
            forward.y = 0;
            return forward.normalized;
        }

        /// <summary>
        /// Calculates the intersection between a ray and a sphere
        /// </summary>
        public static bool IntersectRaySphere(Ray ray, Vector3 sphereCenter, float sphereRadius, out Vector3 intersection)
        {
            Vector3 m = ray.origin - sphereCenter;
            float b = Vector3.Dot(m, ray.direction);
            float c = Vector3.Dot(m, m) - sphereRadius * sphereRadius;

            // if ray origin is outside sphere and is pointing away from sphere, early out
            if (c > 0.0f && b > 0.0f)
            {
                intersection = Vector3.zero;
                return false;
            }

            float discr = b * b - c;
            // If discriminant is < 0 then ray missed the sphere
            if (discr < 0.0f)
            {
                intersection = Vector3.zero;
                return false;
            }

            float t = -b - Mathf.Sqrt(discr);
            // if t is negative ray started inside sphere so clamp to zero
            if (t < 0.0f)
            {
                t = 0.0f;
            }

            intersection = ray.origin + t * ray.direction;
            return true;
        }

        /// <summary>
        /// Calculates the intersection between a ray and a transformed bounds.  The bounds space transform
        /// can optionally define the space that the bounds object is in.  If no transform is provided, bounds
        /// assumes the same sapace as ray
        /// </summary>
        /// <param name="ray">World space ray</param>
        /// <param name="bounds">Bounds that can optionally be in the space of boundsSpaceTransform</param>
        /// <param name="boundsSpaceTransform">Option transform that defines the space bounds exists in</param>
        public static bool IntersectRayBounds(Ray ray, Bounds bounds, Transform boundsSpaceTransform, out Vector3 intersection)
        {
            // If a bounds is specified, transform the ray so it is local to the bound space
            if (boundsSpaceTransform != null)
            {
                ray.origin = boundsSpaceTransform.InverseTransformPoint(ray.origin);
                ray.direction = boundsSpaceTransform.InverseTransformDirection(ray.direction);
            }

            // Intersect the bounds and determine if it actually hit
            float distance = 0;
            bool success = bounds.IntersectRay(ray, out distance);
            if (success)
            {
                // Calculate the actual intersection point on the bounds
                intersection = ray.origin + ray.direction * distance;

                // If a bounds is specified, transform the intersection point back into the original space
                if (boundsSpaceTransform != null)
                {
                    intersection = boundsSpaceTransform.TransformPoint(intersection);
                }
            }
            else
            {
                // No intersection
                intersection = Vector3.zero;
            }

            return success;
        }

        /// <summary>
        /// Interpolates smoothly to a target position.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="target"></param>
        /// <param name="deltaTime"></param>
        /// <param name="speed"></param>
        /// <returns>New interpolated position closer to target</returns>
        public static Vector3 NonLinearInterpolateTo(Vector3 start, Vector3 target, float deltaTime, float speed)
        {
            // If no interpolation speed, jump to target value.
            if (speed <= 0.0f)
            {
                return target;
            }

            Vector3 distance = (target - start);

            // When close enough, jump to the target
            if (distance.sqrMagnitude <= Mathf.Epsilon)
            {
                return target;
            }

            // Apply the delta, then clamp so we don't overshoot the target
            Vector3 deltaMove = distance * Mathf.Clamp(deltaTime * speed, 0.0f, 1.0f);

            return start + deltaMove;
        }

        /// <summary>
        /// Projects the point on to the given ray.
        /// </summary>
        public static Vector3 ProjectPointToRay(Vector3 point, Ray ray)
        {
            // Get the vector from point to the origin of the ray.
            Vector3 pointToRay = point - ray.origin;

            // Project the vector on to the ray to find the lenght of the projection.
            float projectedLength = Vector3.Dot(pointToRay, ray.direction);

            // The projected point is along the ray direction and projectedLength away from the ray origin.
            return ray.GetPoint(projectedLength);
        }

        /// <summary>
        /// Cubic Spline interpolation.
        /// </summary>
        public static Vector3 CubicInterpolation(Vector3 start, Vector3 end, Vector3 control1, Vector3 control2, float A)
        {
            float A2 = A * A;
            float A3 = A2 * A;

            return (((2 * A3) - (3 * A2) + 1) * start) + ((A3 - (2 * A2) + A) * control1) + ((A3 - A2) * control2) + (((-2 * A3) + (3 * A2)) * end);
        }

        /// <summary>
        /// Return a value between 0 to 1 relating where the given value falls between a min and a max
        /// </summary>
        public static float Scale01(float from, float to, float value)
        {
            value = Mathf.Clamp(value, from, to);
            value = (value - from) / (to - from);
            return value;
        }

        /// <summary>
        /// Determines the dominant axis or primary axis of a vector
        /// </summary>
        public static Vector3 FindDominantAxis(Vector3 direction, Transform localSpaceTransform = null)
        {
            Vector3 dominantAxis = Vector3.zero;

            if (localSpaceTransform)
            {
                direction = localSpaceTransform.InverseTransformDirection(direction);
            }

            float absX, absY, absZ;
            absX = Mathf.Abs(direction.x);
            absY = Mathf.Abs(direction.y);
            absZ = Mathf.Abs(direction.z);

            if (absX > absY && absX > absZ)
            {
                dominantAxis = new Vector3(Mathf.Sign(direction.x), 0.0f, 0.0f);
            }
            else if (absY > absX && absY > absZ)
            {
                dominantAxis = new Vector3(0.0f, Mathf.Sign(direction.y), 0.0f);
            }
            else
            {
                dominantAxis = new Vector3(0.0f, 0.0f, Mathf.Sign(direction.z));
            }

            if (localSpaceTransform)
            {
                dominantAxis = localSpaceTransform.TransformDirection(dominantAxis);
            }

            return dominantAxis;
        }

        private static Vector3[] boundsCache = new Vector3[8];

        /// <summary>
        /// Expands a bounding box to include all points from a child bounds
        /// </summary>
        public static void ExpandChildBounds(ref Bounds bounds, Bounds otherBounds, Transform otherTransform)
        {
            Vector3 min = otherBounds.min;
            Vector3 max = otherBounds.max;

            boundsCache[0] = new Vector3(min.x, min.y, min.z);
            boundsCache[1] = new Vector3(min.x, min.y, max.z);
            boundsCache[2] = new Vector3(max.x, min.y, min.z);
            boundsCache[3] = new Vector3(max.x, min.y, max.z);
            boundsCache[4] = new Vector3(min.x, max.y, min.z);
            boundsCache[5] = new Vector3(min.x, max.y, max.z);
            boundsCache[6] = new Vector3(max.x, max.y, min.z);
            boundsCache[7] = new Vector3(max.x, max.y, max.z);

            for (int i = 0; i < boundsCache.Length; i++)
            {
                boundsCache[i] = (otherTransform.localRotation * Vector3.Scale(boundsCache[i], otherTransform.localScale)) + otherTransform.localPosition;
                bounds.Encapsulate(boundsCache[i]);
            }
        }

        /// <summary>
        /// Expands a bounding box to include all points from a child mesh
        /// </summary>
        public static void ExpandChildBounds(ref Bounds bounds, Vector3[] vertices, Transform meshTransform)
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 localPoint = vertices[i];
                localPoint = (meshTransform.localRotation * Vector3.Scale(localPoint, meshTransform.localScale)) + meshTransform.localPosition;
                bounds.Encapsulate(localPoint);
            }
        }

        /// <summary>
        /// Returns if the bounds object is fully contained within another bounds object
        /// </summary>
        public static bool AreBoundsInBounds(Bounds boundsA, Bounds boundsB)
        {
            return boundsA.Contains(boundsB.min) && boundsA.Contains(boundsB.max);
        }

        public static bool AlmostEquals(this float one, float two)
        {
            return (Mathf.Abs(one - two) < Mathf.Epsilon);
        }

        public static bool AlmostEquals(this float one, float two, float threshhold)
        {
            return (Mathf.Abs(one - two) < threshhold);
        }

        public static float Map(float val, float startMin, float startMax, float newMin, float newMax)
        {
            return (val - startMin) * (newMax - newMin) / (startMax - startMin) + newMin;
        }

        public static bool PointInRange(this Vector3 p1, Vector3 p2, float range)
        {
            return ((p2 - p1).sqrMagnitude <= range * range);
        }

        public static Color Rainbow(float progress)
        {
            float div = (Math.Abs(progress % 1) * 6);
            float ascending = (div % 1);
            float descending = 1 - ascending;

            switch ((int)div)
            {
                case 0:
                    return new Color(1, 1, ascending, 0);
                case 1:
                    return new Color(1, descending, 1, 0);
                case 2:
                    return new Color(1, 0, 1, ascending);
                case 3:
                    return new Color(1, 0, descending, 1);
                case 4:
                    return new Color(1, ascending, 0, 1);
                default: // case 5:
                    return new Color(1, 1, 0, descending);
            }
        }

        public static int ToArgb(Color value)
        {
            Color32 c = (Color32)value;
            return
                ((int)c.r << 16) |
                ((int)c.g << 8) |
                ((int)c.b << 0) |
                ((int)c.a << 24);
        }

        public static Color FromArgb(int value)
        {
            return new Color32(
                       (byte)((value >> 16) & 0xFF),
                       (byte)((value >> 8) & 0xFF),
                       (byte)((value >> 0) & 0xFF),
                       (byte)((value >> 24) & 0xFF));
        }

        public static Vector4 ToVector(this Plane p)
        {
            Vector3 n = p.normal;
            return new Vector4(n.x, n.y, n.z, p.distance);
        }
    }
}
