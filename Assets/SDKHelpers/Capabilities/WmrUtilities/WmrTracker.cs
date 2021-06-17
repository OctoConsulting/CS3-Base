using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.XR.WSA;

// Not stoked on this. 
using SensorsSDK.UnityUtilities;

#if ENABLE_WINMD_SUPPORT
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
    public class WmrTracker : Singleton<WmrTracker>
    {
        public static bool DebugWmrTracker { get; set; } = false;
        private StringBuilder builder = new StringBuilder();

        private DateTime nowTime;

        public bool NotifyOnSignificantChanges { get; set; } = false;

        public void ToggleNotifications()
        {
            NotifyOnSignificantChanges = !NotifyOnSignificantChanges;
        }

        #region Private Members

#pragma warning disable CS0649
        private struct InternalHoloLensFrameStructure
        {
            public uint VersionNumber;
            public uint MaxNumberOfCameras;
            public IntPtr ISpatialCoordinateSystemPtr;
            public IntPtr IHolographicFramePtr;
            public IntPtr IHolographicCameraPtr;
        }
#pragma warning restore CS0649

#pragma warning disable CS0414
        private bool needsRigTransform = true;
        private bool needsEyeTransforms = true;
#pragma warning restore CS0414

        private PoseHistoryTracker unityCameraHistory = new PoseHistoryTracker();

        #endregion
        #region Public Interface

        // When bool == true, the system has 6dof tracking -- otherwise false
        public static event System.Action<bool> OnPositionalTrackingStateChanged = null;

        public static bool Has6DOF
        {
            get
            {
#if UNITY_WSA
                return WorldManager.state == PositionalLocatorState.Active;
#else
                return true;
#endif
            }
        }

        /// <summary>
        /// Location of the rendering 'left eye' in camera space.
        /// </summary>
        public Matrix4x4 CameraFromLeftEye { get; private set; }

        /// <summary>
        /// Location of the rendering 'right eye' in camera space.
        /// </summary>
        public Matrix4x4 CameraFromRightEye { get; private set; }

        public Matrix4x4 CameraSpaceFromRigSpace { get; private set; }

        /// <summary>
        /// Transform that gets us from 3dof space into world space. 3dof space is gravity aligned
        /// with an arbitrary direction for forward. It is located at the base of the user's neck
        /// (the pivot point for their head). 3dof space is useful because the camera can be tracked
        /// in this space in any lighting conditions.
        /// </summary>
        public Matrix4x4 WorldSpaceFromThreeDofSpace { get; private set; }

        /// <summary>
        /// Transform that gets us from world space into 3dof. 3dof space is gravity aligned
        /// with an arbitrary direction for forward. It is located at the base of the user's neck
        /// (the pivot point for their head). 3dof space is useful because the camera can be tracked
        /// in this space in any lighting conditions.
        /// </summary>
        public Matrix4x4 ThreeDofFromWorld { get; private set; }

        /// <summary>
        /// The position of the camera in threeDof space.
        /// </summary>
        public Matrix4x4 ThreeDofSpaceFromCameraSpace { get; private set; }

        public bool ValidFrame { get; private set; }

        public TimeSpan SystemRelativeTargetTime
        {
            get
            {
#if ENABLE_WINMD_SUPPORT
                return WmrTracker.Instance.CurrentFrameTimestamp.SystemRelativeTargetTime;
#else
                return TimeSpan.FromTicks(Win32Utilities.Win32Utilities.Query100NanoPerformanceCounter()) + TimeSpan.FromMilliseconds(32);
#endif
            }
        }

        public TimeSpan PredictionAmmount
        {
            get
            {
#if ENABLE_WINMD_SUPPORT
                return WmrTracker.Instance.CurrentFrameTimestamp.PredictionAmount;
#else
                return TimeSpan.FromMilliseconds(32);
#endif
            }
        }

        /// <summary>
        /// The location of the 'rig' in world space. The rig is a concept of a "well known location
        /// on a WMR headset". It is used by internal teams for things like calibration. This
        /// function calculates the transform at some historical point in time in the current
        /// frame's world space.
        /// </summary>
        public bool CalculateWorldFromRig(TimeSpan nowTicks, TimeSpan systemRelativeTicks, ref Matrix4x4 worldFromHistoricalRig)
        {
            Matrix4x4 worldFromCamera = Matrix4x4.identity;
            if (CalculateWorldFromCamera(nowTicks, systemRelativeTicks, ref worldFromCamera))
            {
                worldFromHistoricalRig = worldFromCamera * CameraSpaceFromRigSpace;
                return true;
            }
            return false;
        }

        /// <summary>
        /// This function calculates the transform of the camera at some historical point in time
        /// in the current frame's world space. Note that the camera position on any given frame is
        /// in the current frame's world space. Note that the camera position on any given frame is
        /// a *predicted value*, so simply tracking the unity camera positions does not work if you
        /// want to know the *actual* camera location at a point in history. This function will
        /// return a real value.
        /// </summary>
        public bool CalculateWorldFromCamera(TimeSpan nowTicks, TimeSpan systemRelativeTicks, ref Matrix4x4 worldFromHistoricalCamera)
        {
#if ENABLE_WINMD_SUPPORT
            bool success = false;
            try
            {
                if (ValidFrame)
                {
                    PerceptionTimestamp historicalTimestamp = PerceptionTimestampHelper.FromSystemRelativeTargetTime(systemRelativeTicks);
                    SpatialPointerPose historicalPose = SpatialPointerPose.TryGetAtTimestamp(CurrentDefaultAttachedCoordinateSystem, historicalTimestamp);

                    // When transitioning between IT/VT, we'll see the coordinate system from *this*
                    // frame be disjoint from the historical one and historicalPose will be null. In
                    // This case, try creating a new coordinate system from the same time as the
                    // historical stamp. We should be able to locate that one.
                    if (historicalPose == null)
                    {
                        SpatialCoordinateSystem tempCoordinateSystem = DefaultAttachedFrameOfReference.GetStationaryCoordinateSystemAtTimestamp(historicalTimestamp);
                        if (tempCoordinateSystem == null)
                        {
                            throw new Exception($"Failed to create historical coordinate system at time {systemRelativeTicks.ToString()}. Age: {(nowTicks - systemRelativeTicks).TotalMilliseconds} ms");
                        }

                        historicalPose = SpatialPointerPose.TryGetAtTimestamp(tempCoordinateSystem, historicalTimestamp);
                    }

                    if (historicalPose == null)
                    {
                        throw new Exception($"Failed to locate camera at time {systemRelativeTicks.ToString()}. Age: {(nowTicks - systemRelativeTicks).TotalMilliseconds} ms");
                    }

                    Matrix4x4 threeDofFromHistoricalCamera = WmrHelpers.ToUnity(historicalPose.Head);
                    worldFromHistoricalCamera = WorldSpaceFromThreeDofSpace * threeDofFromHistoricalCamera;
                    success = true;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            return success;
#else
            bool success = false;
            Vector3 cameraPosition = Vector3.zero;
            Quaternion cameraRotation = Quaternion.identity;
            DateTime dateTime = nowTime - (nowTicks - systemRelativeTicks);
            if (unityCameraHistory.LookupHistoricalPose(dateTime, ref cameraPosition, ref cameraRotation))
            {
                worldFromHistoricalCamera = Matrix4x4.TRS(cameraPosition, cameraRotation, Vector3.one);
                success = true;
            }
            return success;
#endif
        }

        public bool GetWorldFromCoordinateSystem(Guid coordID, ref Matrix4x4 worldFromCS)
        {
#if ENABLE_WINMD_SUPPORT
            bool success = false;
            SpatialCoordinateSystem qrCodeCoord = Windows.Perception.Spatial.Preview.SpatialGraphInteropPreview.CreateCoordinateSystemForNode(coordID);
            if (qrCodeCoord != null)
            {
                System.Numerics.Matrix4x4? qrToWorld = qrCodeCoord.TryGetTransformTo(RootCoordinateSystem);

                if (qrToWorld.HasValue)
                {
                    worldFromCS = WmrHelpers.ToUnity(qrToWorld.Value);
                    success = true;
                }
            }
            return success;
#else
            return false;
#endif
        }

#endregion
#region Unity Interop

        private void OnEnable()
        {
#if UNITY_WSA
            WorldManager.OnPositionalLocatorStateChanged += WorldManager_OnPositionalLocatorStateChanged;
#endif
            Update();
        }

        private void OnDisable()
        {
#if UNITY_WSA
            WorldManager.OnPositionalLocatorStateChanged -= WorldManager_OnPositionalLocatorStateChanged;
#endif
            ResetPerFrameData();
            ResetPerRunData();
        }

        private void Update()
        {
#if ENABLE_WINMD_SUPPORT
            ValidFrame = EnsureOneTimeInitialization()
                ? UpdatePerFrameObjects(CameraProvider.MainCamera.transform.localToWorldMatrix)
                : false;
#else
            // We're always valid in the editor. Fake everything out.
            ValidFrame = true;

            Camera camera = CameraProvider.MainCamera;
            Matrix4x4 mat = Matrix4x4.identity;

            Vector3 position = camera.transform.position * -1;
            mat.SetColumn(3, new Vector4(position.x, position.y, position.z, 1));
            ThreeDofFromWorld = mat;

            position = camera.transform.position;
            mat.SetColumn(3, new Vector4(position.x, position.y, position.z, 1));
            WorldSpaceFromThreeDofSpace = mat;

            ThreeDofSpaceFromCameraSpace = ThreeDofFromWorld * camera.transform.localToWorldMatrix;

            float assumedHalfIpd = 0.064f * 0.5f;
            Matrix4x4 worldFromCamera = transform.localToWorldMatrix;
            CameraFromLeftEye = Matrix4x4.TRS(new Vector3(-1 * assumedHalfIpd, 0, 0), Quaternion.identity, Vector3.one);
            CameraFromRightEye = Matrix4x4.TRS(new Vector3(1 * assumedHalfIpd, 0, 0), Quaternion.identity, Vector3.one);
            CameraSpaceFromRigSpace = new Matrix4x4(
                new Vector4(0.01286f, -0.99911f, -0.04006f, 0),
                new Vector4(0.99967f, 0.01196f, 0.02277f, 0),
                new Vector4(-0.02227f, -0.04034f, 0.99894f, 0),
                new Vector4(-0.04982f, 0.04711f, 0.03686f, 1));

            // UTCNow is way cheaper than Now, and this is editor only
            nowTime = DateTime.UtcNow;
            unityCameraHistory.AddPose(nowTime, CameraProvider.MainCamera.transform);
#endif
        }

#endregion
#if ENABLE_WINMD_SUPPORT
#region Per-Frame Objects

        private HolographicFrame CurrentHolographicFrame { get; set; }
        private HolographicFramePrediction CurrentFramePrediction { get; set; }
        public PerceptionTimestamp CurrentFrameTimestamp { get; set; }
        public HolographicCameraPose CurrentCameraPose { get; set; }
        public SpatialCoordinateSystem CurrentDefaultAttachedCoordinateSystem { get; set; }
        private SpatialCoordinateSystem CurrentRigAttachedCoordinateSystem { get; set; }

        private bool UpdatePerFrameObjects(Matrix4x4 worldFromCamera)
        {
            bool success = false;
            try
            {
                //
                // Read data out of the unity frame structure.
                //
                IntPtr nativeStruct = UnityEngine.XR.XRDevice.GetNativePtr();
                if (nativeStruct == IntPtr.Zero)
                {
                    throw new Exception("Failed to get XRDevice native pointer");
                }

                InternalHoloLensFrameStructure s = Marshal.PtrToStructure<InternalHoloLensFrameStructure>(nativeStruct);
                if (s.IHolographicFramePtr == IntPtr.Zero)
                {
                    throw new Exception("Failed to get IHolographicFramePtr");
                }

                CurrentHolographicFrame = Marshal.GetObjectForIUnknown(s.IHolographicFramePtr) as HolographicFrame;
                if (CurrentHolographicFrame == null)
                {
                    throw new Exception("Could not get holographicframe");
                }

                CurrentFramePrediction = CurrentHolographicFrame.CurrentPrediction;
                CurrentFrameTimestamp = CurrentFramePrediction.Timestamp;

                //
                // Update the three dof transforms for the frame.
                //
                CurrentDefaultAttachedCoordinateSystem = DefaultAttachedFrameOfReference.GetStationaryCoordinateSystemAtTimestamp(CurrentFrameTimestamp);
                SpatialPointerPose defaultCameraPose = SpatialPointerPose.TryGetAtTimestamp(CurrentDefaultAttachedCoordinateSystem, CurrentFrameTimestamp);

                // This should always work. If is doesn't, then our locators have gone "bad". May be
                // a platform bug, but for now, we just recreate everything and try again.
                if (defaultCameraPose == null)
                {
                    Debug.LogWarning("Invalid locators detected. Recreating them.");

                    ResetPerRunData();
                    EnsureOneTimeInitialization();

                    CurrentDefaultAttachedCoordinateSystem = DefaultAttachedFrameOfReference.GetStationaryCoordinateSystemAtTimestamp(CurrentFrameTimestamp);
                    defaultCameraPose = SpatialPointerPose.TryGetAtTimestamp(CurrentDefaultAttachedCoordinateSystem, CurrentFrameTimestamp);
                    if (defaultCameraPose == null)
                    {
                        throw new Exception("Failed to get spatialpointerpose for default attached coordinate system. This shouldn't happen in regular operation. Typically indicates a HUP crash.");
                    }
                }

                ThreeDofSpaceFromCameraSpace = WmrHelpers.ToUnity(defaultCameraPose.Head);
                Matrix4x4 cameraFromDefaultAttached = ThreeDofSpaceFromCameraSpace.inverse;
                WorldSpaceFromThreeDofSpace = worldFromCamera * cameraFromDefaultAttached;
                ThreeDofFromWorld = WorldSpaceFromThreeDofSpace.inverse;

                //
                // Update eye transforms if needed.
                // Cache this to avoid the per-frame cost. ET could change these, so this isn't great.
                //
                if (needsEyeTransforms)
                {
                    IReadOnlyList<HolographicCameraPose> poses = CurrentFramePrediction.CameraPoses;
                    if (poses.Count == 0)
                    {
                        throw new Exception("Unexpected camera pose count");
                    }

                    CurrentCameraPose = poses[0];
                    HolographicStereoTransform? transform = poses[0].TryGetViewTransform(CurrentDefaultAttachedCoordinateSystem);
                    if (transform == null)
                    {
                        throw new Exception("No rendering transforms");
                    }

                    HolographicStereoTransform transformValue = transform.Value;
                    Matrix4x4 left = WmrHelpers.ToUnity(transformValue.Left);
                    Matrix4x4 right = WmrHelpers.ToUnity(transformValue.Right);
                    Matrix4x4 invMiddle = WmrHelpers.ToUnity(System.Numerics.Matrix4x4.Lerp(transformValue.Left, transformValue.Right, 0.5f)).inverse;
                    CameraFromLeftEye = invMiddle * left;
                    CameraFromRightEye = invMiddle * right;
                    // needsEyeTransforms = false;
                }

                //
                // Update the rig transform if needed.
                // Cache this to avoid the per-frame cost. Should we move this to per-run data?
                //
                if (needsRigTransform)
                {
                    // Get the cameraFromRig transform. This is a constant value (that we should cache),
                    SpatialLocation rigPose = RigLocator.TryLocateAtTimestamp(CurrentFrameTimestamp, CurrentDefaultAttachedCoordinateSystem);
                    if (rigPose == null)
                    {
                        throw new Exception("No rig transforms");
                    }
                    Matrix4x4 cameraFromWorld = worldFromCamera.inverse;
                    Matrix4x4 defaultAttachedFromRig = WmrHelpers.ToUnity(rigPose, needsYFlip: true);
                    CameraSpaceFromRigSpace = cameraFromDefaultAttached * defaultAttachedFromRig;
                    // needsRigTransform = false;
                }

                // Debug logs if enabled.                
                if (DebugWmrTracker)
                {
                    PrintDebugLogs();
                }

                success = true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                ResetPerFrameData();
            }

            return success;
        }

        private void PrintDebugLogs()
        {
            builder.Clear();
            builder.AppendLine($"Wmr Tracker State for frame {Time.frameCount}:");
            builder.AppendLine($"    PredictionAmmount: {CurrentFrameTimestamp.PredictionAmount.TotalMilliseconds} ms");

            List<KeyValuePair<string, Matrix4x4>> printTargets = new List<KeyValuePair<string, Matrix4x4>>()
            {
                new KeyValuePair<string, Matrix4x4>("worldFromThreeDof", WorldSpaceFromThreeDofSpace),
                new KeyValuePair<string, Matrix4x4>("threeDofFromCamera", ThreeDofSpaceFromCameraSpace),
                new KeyValuePair<string, Matrix4x4>("CameraFromLeftEye", CameraFromLeftEye),
                new KeyValuePair<string, Matrix4x4>("CameraFromRightEye", CameraFromRightEye),
                new KeyValuePair<string, Matrix4x4>("CameraFromRig", CameraSpaceFromRigSpace),
            };

            for (int i = 0; i < printTargets.Count; ++i)
            {
                Quaternion rot = printTargets[i].Value.rotation;
                Vector3 pos = printTargets[i].Value.GetColumn(3);
                builder.AppendLine($"    {printTargets[i].Key}: [{pos.x:0.0000} {pos.y:0.0000} {pos.z:0.0000}] [{rot.x:0.0000} {rot.y:0.0000} {rot.z:0.0000} {rot.w:0.0000}]");
            }
            Debug.LogWarning(builder.ToString());
        }

        private void ResetPerFrameData()
        {
            CurrentHolographicFrame = null;
            CurrentFramePrediction = null;
            CurrentCameraPose = null;
            CurrentFrameTimestamp = null;
            CurrentDefaultAttachedCoordinateSystem = null;
            CurrentRigAttachedCoordinateSystem = null;

            CameraFromLeftEye = Matrix4x4.identity;
            CameraFromRightEye = Matrix4x4.identity;
            CameraSpaceFromRigSpace = Matrix4x4.identity;

            WorldSpaceFromThreeDofSpace = Matrix4x4.identity;
            ThreeDofFromWorld = Matrix4x4.identity;
        }

#endregion
#region Per-Run Objects

        // TODO: Make this not public.
        public SpatialCoordinateSystem RootCoordinateSystem { get; set; }
        private SpatialLocator RigLocator { get; set; }
        private SpatialLocator DefaultLocator { get; set; }
        private SpatialLocatorAttachedFrameOfReference RigAttachedFrameOfReference { get; set; }
        public  SpatialLocatorAttachedFrameOfReference DefaultAttachedFrameOfReference { get; set; }

        private bool EnsureOneTimeInitialization()
        {
            try
            {
#pragma warning disable 1701
                if (RootCoordinateSystem == null)
                {
                    IntPtr coordinateSystemPtr = UnityEngine.XR.WSA.WorldManager.GetNativeISpatialCoordinateSystemPtr();
                    RootCoordinateSystem = Marshal.GetObjectForIUnknown(coordinateSystemPtr) as SpatialCoordinateSystem;
                }

                if (RigLocator == null)
                {
                    Guid rigDynamicNodeId = GetRigDynamicNodeID();
                    Debug.LogFormat("Found rig dynamic node ID: {0}", rigDynamicNodeId.ToString());
                    RigLocator = SpatialGraphInteropPreview.CreateLocatorForNode(rigDynamicNodeId);
                }

                if (RigAttachedFrameOfReference == null && RigLocator != null)
                {
                    RigAttachedFrameOfReference = RigLocator.CreateAttachedFrameOfReferenceAtCurrentHeading(new System.Numerics.Vector3(0, 0, 0));
                }

                if (DefaultLocator == null)
                {
                    DefaultLocator = SpatialLocator.GetDefault();
                }

                if (DefaultAttachedFrameOfReference == null && DefaultLocator != null)
                {
                    DefaultAttachedFrameOfReference = DefaultLocator.CreateAttachedFrameOfReferenceAtCurrentHeading(new System.Numerics.Vector3(0, 0, 0));
                }
#pragma warning restore 1701
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            return RootCoordinateSystem != null && RigLocator != null && RigAttachedFrameOfReference != null && DefaultLocator != null && DefaultAttachedFrameOfReference != null;
        }

        private void ResetPerRunData()
        {
            RootCoordinateSystem = null;
            RigLocator = null;
            DefaultLocator = null;
            RigAttachedFrameOfReference = null;
            DefaultAttachedFrameOfReference = null;
            needsRigTransform = true;
            needsEyeTransforms = true;
        }

#endregion
#else
        private void ResetPerFrameData()
        {
            CameraFromLeftEye = Matrix4x4.identity;
            CameraFromRightEye = Matrix4x4.identity;
            CameraSpaceFromRigSpace = Matrix4x4.identity;

            WorldSpaceFromThreeDofSpace = Matrix4x4.identity;
            ThreeDofFromWorld = Matrix4x4.identity;
        }

        private void ResetPerRunData()
        { }
#endif

#if UNITY_WSA
        private void WorldManager_OnPositionalLocatorStateChanged(PositionalLocatorState oldState, PositionalLocatorState newState)
        {
            // The two states should almost exclusively be switching between Inhibited and Active.
            // Might go from Activating --> Active the first time 6dof tracking is gained

            Debug.Log($"WorldManager_OnPositionalLocatorStateChanged: {oldState.ToString("G")} => {newState.ToString("G")}");

            if (newState == PositionalLocatorState.Active)
            {
                OnPositionalTrackingStateChanged?.Invoke(true);
            }
            else
            {
                OnPositionalTrackingStateChanged?.Invoke(false);
            }
        }
#endif

        [DllImport("GetRigDynamicNodeID", CallingConvention = CallingConvention.StdCall)]
        public static extern Guid GetRigDynamicNodeID();

    }
}
