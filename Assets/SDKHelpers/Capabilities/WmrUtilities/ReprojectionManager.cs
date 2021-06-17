using System;
using System.Collections.Generic;
using UnityEngine;

using SensorsSDK.UnityUtilities;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SensorsSDK.WmrUtilities
{
    public class ReprojectionManager : Singleton<ReprojectionManager>
    {
        [SerializeField]
        private GameObject debugPlane = null;

        [SerializeField]
        private float minimumPlaneDistance = 0.2f;

        [SerializeField]
        private float maximumAngleFacingAwayFromCamera = 85;

        private List<ReprojectionTarget> seriousTargets = new List<ReprojectionTarget>();
        private List<ReprojectionTarget> normalTargets = new List<ReprojectionTarget>();
        private List<ReprojectionTarget> lowTargets = new List<ReprojectionTarget>();

        private bool debugPlaneActive = false;
        
        // also invoked by voice command
        public void ToggleDebugPlane()
        {
            debugPlaneActive = !debugPlaneActive;
        }

        public void AddReprojectionTarget(ReprojectionTarget t)
        {
            switch (t.Priority)
            {
                case LsrPlanePriority.Serious:
                    seriousTargets.Add(t);
                    break;
                case LsrPlanePriority.Normal:
                    normalTargets.Add(t);
                    break;
                case LsrPlanePriority.Low:
                    lowTargets.Add(t);
                    break;
            }
        }

        public void RemoveReprojectionTarget(ReprojectionTarget t)
        {
            switch (t.Priority)
            {
                case LsrPlanePriority.Serious:
                    seriousTargets.Remove(t);
                    break;
                case LsrPlanePriority.Normal:
                    normalTargets.Remove(t);
                    break;
                case LsrPlanePriority.Low:
                    lowTargets.Remove(t);
                    break;
            }
        }



#if UNITY_WSA
        int mode = 2;

        Vector3 old2m = Vector3.zero;
        Vector3 old1000m = Vector3.zero;
        private void LateUpdate()
        {
            if (CameraProvider.MainCamera == null)
            {
                return;
            }

            // 0 - world locked on reprojection targets
            // 1 - world locked 2m
            // 2 - body locked
            // 3 - display locked - broken. not in input rotation.
            //if (Input.GetKeyDown(KeyCode.Alpha6))
            //{
            //    mode = (mode == 2) ? 0 : (mode + 1);
            //    ToastNotificationManager.Instance.SetNotification($"Reprojection Mode: {mode}", 1);
            //}

            //if (Input.GetKeyDown(KeyCode.Alpha7))
            //{
            //    ToggleDebugPlane();
            //    ToastNotificationManager.Instance.SetNotification($"Toggle Debug Plane", 1);
            //}


            // Preference on correct sensor visualization. This is mission critical.
            bool planeSet = false;

            if (mode == 0)
            {
                planeSet = TrySetPlaneOnTargetSet(seriousTargets) ||
                    TrySetPlaneOnTargetSet(normalTargets) ||
                    TrySetPlaneOnTargetSet(lowTargets);

                if (!planeSet)
                {
                    UnityEngine.XR.WSA.HolographicSettings.SetFocusPointForFrame(
                        CameraProvider.MainCamera.transform.InverseTransformPoint(new Vector3(0, 0, 1000)),
                        CameraProvider.MainCamera.transform.forward * -1,
                        Vector3.zero);
                }
            }
            else if (mode == 1)
            {

            }
            else if (mode == 2)
            {
                UnityEngine.XR.WSA.HolographicSettings.SetFocusPointForFrame(
                    CameraProvider.MainCamera.transform.InverseTransformPoint(new Vector3(0, 0, 1000)),
                    CameraProvider.MainCamera.transform.forward * -1,
                    Vector3.zero);
            }
            else if (mode == 3)
            {
                // This is broken. Need to figure out how to do display locked...
                Vector3 new2m = CameraProvider.MainCamera.transform.InverseTransformPoint(new Vector3(0, 0, 2));
                UnityEngine.XR.WSA.HolographicSettings.SetFocusPointForFrame(
                    new2m,
                    CameraProvider.MainCamera.transform.forward * -1,
                    (new2m - old2m) / Time.deltaTime);
                old2m = new2m;
            }
            else if (mode == 4)

            debugPlane.SetActive(debugPlaneActive && planeSet);
        }
#endif

            private bool TrySetPlaneOnTargetSet(List<ReprojectionTarget> targets)
        {
            if (targets.Count > 0)
            {
                targets.Sort((a, b) =>
                {
                    float distanceSqrA = (CameraProvider.MainCamera.transform.position - a.transform.position).sqrMagnitude;
                    float distanceSqrB = (CameraProvider.MainCamera.transform.position - b.transform.position).sqrMagnitude;
                    return distanceSqrA.CompareTo(distanceSqrB);
                });
                foreach (ReprojectionTarget t in targets)
                {
                    if (TrySetPlaneOnTarget(t))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool TrySetPlaneOnTarget(ReprojectionTarget target)
        {
            if (target == null || !target.IsValid)
            {
                return false;
            }

            Vector3 direction = Vector3.forward;
            switch (target.Orientation)
            {
                case LsrPlaneOrientation.FaceCamera:
                    direction = CameraProvider.MainCamera.transform.forward * -1;
                    break;
                case LsrPlaneOrientation.FaceForward:
                    direction = target.transform.forward;
                    break;
                case LsrPlaneOrientation.FaceBackward:
                    direction = target.transform.forward * -1;
                    break;
                case LsrPlaneOrientation.FaceUp:
                    direction = target.transform.up;
                    break;
                case LsrPlaneOrientation.FaceDown:
                    direction = target.transform.up * -1;
                    break;
            }

            // TODO: There are better ways to do this. Consider doing things like bounds checking.
            // Atm, relevant angle depends on viewing distance, while bounds can be meaningfully
            // interpretted at any distance.
            Vector3 cameraRelativePosition = CameraProvider.MainCamera.transform.InverseTransformPoint(target.transform.position);
            if (!IsCameraLookingAtTransform(cameraRelativePosition, target.RelevantAngle))
            {
                return false;
            }

            // If the angle becomes too incorrect relative to the camera, we start to see massive
            // artifacts. Avoid this by rejecting planes outside of a given range.
            if (Vector3.Angle(CameraProvider.MainCamera.transform.forward * -1, direction) > maximumAngleFacingAwayFromCamera)
            {
                return false;
            }

            // We've already paid the cost for a cameraRelativePosition, so reuse it. To do this,
            // flip the direction.
            float distanceFromPlane = Vector3.Dot(cameraRelativePosition, CameraProvider.MainCamera.transform.InverseTransformDirection(direction * -1));
            if (distanceFromPlane < minimumPlaneDistance)
            {
                return false;
            }

            UnityEngine.XR.WSA.HolographicSettings.SetFocusPointForFrame(target.transform.position, direction, target.Velocity);

            if (debugPlane != null && debugPlaneActive)
            {
                debugPlane.transform.position = target.transform.position;
                debugPlane.transform.forward = direction * -1; // Unity quad faces backward.
            }

            return true;
        }

        bool IsCameraLookingAtTransform(Vector3 cameraRelativePosition, Vector2 thresholdDregrees)
        {
            Vector3 yzDirection = new Vector3(0, cameraRelativePosition.y, cameraRelativePosition.z).normalized;
            float pitch = (yzDirection == Vector3.zero) ? 0 : Vector3.Angle(Vector3.forward, yzDirection);

            Vector3 xzDirection = new Vector3(cameraRelativePosition.x, 0, cameraRelativePosition.z).normalized;
            float yaw = (xzDirection == Vector3.zero) ? 0 : Vector3.Angle(Vector3.forward, xzDirection);

            return pitch < thresholdDregrees.x && yaw < thresholdDregrees.y;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ReprojectionManager))]
        public class ReprojectionManagerEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();

                var rm = target as ReprojectionManager;
                if (rm != null && Application.isEditor && Application.isPlaying)
                {
                    EditorGUILayout.Separator();

                    if (rm.debugPlaneActive)
                    {
                        if (GUILayout.Button("Disable Reprojection Debug View"))
                        {
                            rm.ToggleDebugPlane();
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Enable Reprojection Debug View"))
                        {
                            rm.ToggleDebugPlane();
                        }
                    }
                }
            }
        }
#endif
    }
}
