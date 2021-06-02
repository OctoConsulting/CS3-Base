using UnityEngine;
using SensorsSDK.UnityUtilities;
#if ENABLE_WINMD_SUPPORT
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;

using Windows.Perception;
using Windows.Perception.Spatial;
using Windows.Perception.Spatial.Preview;
using Windows.Graphics.Holographic;
using Windows.UI.Input.Spatial;
using Windows.Perception.People;
#endif

namespace SensorsSDK.WmrUtilities
{
    public static class WmrHelpers
    {
        public static Matrix4x4 ZFlip { get; } = new Matrix4x4(new Vector4(+1, 0, 0, 0), new Vector4(0, +1, 0, 0), new Vector4(0, 0, -1, 0), new Vector4(0, 0, 0, +1));
        public static Matrix4x4 YFlip { get; } = new Matrix4x4(new Vector4(+1, 0, 0, 0), new Vector4(0, -1, 0, 0), new Vector4(0, 0, +1, 0), new Vector4(0, 0, 0, +1));

        static public void PrintMatrix(string name, Matrix4x4 mat)
        {
            Debug.LogWarningFormat("{16}:\n    {0,7:0.0000} {1,7:0.0000} {2,7:0.0000} {3,7:0.0000}\n    {4,7:0.0000} {5,7:0.0000} {6,7:0.0000} {7,7:0.0000}\n    {8,7:0.0000} {9,7:0.0000} {10,7:0.0000} {11,7:0.0000}\n    {12,7:0.0000} {13,7:0.0000} {14,7:0.0000} {15,7:0.0000}\n",
                mat.m00, mat.m01, mat.m02, mat.m03,
                mat.m10, mat.m11, mat.m12, mat.m13,
                mat.m20, mat.m21, mat.m22, mat.m23,
                mat.m30, mat.m31, mat.m32, mat.m33,
                name);
        }

        static public void PrintVector(string name, Vector4 vec)
        {
            Debug.LogWarningFormat("{0}: {1,7:0.0000} {2,7:0.0000} {3,7:0.0000} {4,7:0.0000}",
                name, vec.x, vec.y, vec.z, vec.w);
        }

        // TODO: Deprecate.
        static public void ConcatenateTransform(Vector3 bPosition, Quaternion bRotation, Vector3 aPosition, Quaternion aRotation, out Vector3 outPosition, out Quaternion outRotation)
        {
            outPosition = bPosition + (bRotation * aPosition);
            outRotation = bRotation * aRotation;
        }


        // TODO: Deprecate.
        static public Vector3 ApplyTransform(Vector3 aPosition, Quaternion aRotation, Vector3 input)
        {
            return aPosition + (aRotation * input);
        }


        // TODO: Deprecate.
        static public void InverseTransform(Vector3 aPosition, Quaternion aRotation, out Vector3 outPosition, out Quaternion outRotation)
        {
            outRotation = Quaternion.Inverse(aRotation);
            outPosition = (outRotation * aPosition) * -1;
        }


        // TODO: Deprecate.
        static public void TransformRelativeTo(Vector3 basePosition, Quaternion baseRotation, Vector3 position, Quaternion rotation, out Vector3 outPosition, out Quaternion outRotation)
        {
            Vector3 invBasePosition;
            Quaternion invBaseRotation;
            InverseTransform(basePosition, baseRotation, out invBasePosition, out invBaseRotation);
            ConcatenateTransform(invBasePosition, invBaseRotation, position, rotation, out outPosition, out outRotation);
        }

#if ENABLE_WINMD_SUPPORT
        public static Matrix4x4 ToUnity(SpatialLocation location, bool needsYFlip = false)
        {
#if true
            // Numerics uses row-vectors, so we go from left to right to rotate, then translate.
            return ToUnity(
                System.Numerics.Matrix4x4.CreateFromQuaternion(location.Orientation) *
                System.Numerics.Matrix4x4.CreateTranslation(location.Position),
                needsYFlip);
#else
            return Matrix4x4.TRS(ToUnity(location.Position), ToUnity(location.Orientation), Vector3.one);
#endif
        }

        // Convert a system.numerics row-vector notation transformation matrix into a Unity column
        // vector notation transformation matrix. Handle the rh v. lh switch as well.
        public static Matrix4x4 ToUnity(System.Numerics.Matrix4x4 windowsMat, bool needsYFlip = false)
        {
            // Transpose the matrix to convert from a row vector matrix to a column vector matrix.
            Vector4 col0 = new Vector4(windowsMat.M11, windowsMat.M12, windowsMat.M13, windowsMat.M14);
            Vector4 col1 = new Vector4(windowsMat.M21, windowsMat.M22, windowsMat.M23, windowsMat.M24);
            Vector4 col2 = new Vector4(windowsMat.M31, windowsMat.M32, windowsMat.M33, windowsMat.M34);
            Vector4 col3 = new Vector4(windowsMat.M41, windowsMat.M42, windowsMat.M43, windowsMat.M44);
            Matrix4x4 mat = new Matrix4x4(col0, col1, col2, col3);
            return ZFlip * mat * (needsYFlip ? YFlip : ZFlip);
        }

        public static Vector3 ToUnity(System.Numerics.Vector3 fromMirage)
        {
            return new Vector3(
	            fromMirage.X,
	            fromMirage.Y,
	            -fromMirage.Z);
        }

        public static Quaternion ToUnity(System.Numerics.Quaternion fromMirage)
        {
            return new Quaternion(
                -fromMirage.X,
                -fromMirage.Y,
                fromMirage.Z,
                fromMirage.W);
        }

        public static Matrix4x4 ToUnity(HeadPose pose)
        {
            // TODO: Bypass the TRS... This is faster but needs to be validated.
#if false
            Vector3 forward = ToUnity(pose.ForwardDirection);
            Vector3 up = ToUnity(pose.ForwardDirection);
            Vector3 right = Vector3.Cross(up, forward);
            Vector3 translation = ToUnity(pose.Position);
            return new Matrix4x4(
                new Vector4(right.x, right.y, right.z, 1),
                new Vector4(up.x, up.y, up.z, 1),
                new Vector4(forward.x, forward.y, forward.z, 1),
                new Vector4(translation.x, translation.y, translation.z, 1));
#else
            Vector3 translation = ToUnity(pose.Position);
            Quaternion rotation = Quaternion.LookRotation(
                ToUnity(pose.ForwardDirection), 
                ToUnity(pose.UpDirection));

            return Matrix4x4.TRS(translation, rotation, Vector3.one);
#endif
        }
#endif
    }
}
