using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SensorsSDK.MotionBehaviors
{
    [CreateAssetMenu(menuName = "SensorsSDK/Body Locked Follow Behavior Configuration")]
    public class BodyLockedFollowBehaviorConfiguration : ScriptableObject
    {
        [SerializeField]
        private BodyLockedFollowBehavior.Mode followMode = BodyLockedFollowBehavior.Mode.Sphere;

        [Header("Position")]
        [SerializeField]
        public Vector3 offsetFromFollowPoint = new Vector3(0, 0, 1);

        [SerializeField]
        private float horizontalRestAngle = 20.0f;

        [SerializeField]
        private float verticalRestAngle = 15.0f;

        [Header("Rotation")]
        [SerializeField]
        [Tooltip("If true, the object is rotated to face the user. Otherwise only the position is 'carried' and the rotation is untouched.")]
        private BodyLockedFollowBehavior.LookMode lookMode = BodyLockedFollowBehavior.LookMode.BillboardNoRoll;

        [Header("Animation")]
        [SerializeField]
        private float targetAnimationTime = 0.25f;

        public float LocalDistanceFromFollowPoint => offsetFromFollowPoint.magnitude;

        public BodyLockedFollowBehavior.Mode FollowMode => followMode;

        public Vector3 OffsetFromFollowPoint => offsetFromFollowPoint;

        public float HorizontalRestAngle => horizontalRestAngle;

        public float VerticalRestAngle => verticalRestAngle;

        public float TargetAnimationTime => targetAnimationTime;

        public BodyLockedFollowBehavior.LookMode LookMode => lookMode;

#if UNITY_EDITOR
        [CustomEditor(typeof(BodyLockedFollowBehaviorConfiguration))]
        public class MyScriptEditor : Editor
        {
            override public void OnInspectorGUI()
            {
                // Mimic the visual style of the default inspector.
                BodyLockedFollowBehaviorConfiguration behavior = null;
                using (new EditorGUI.DisabledScope(true))
                {
                    behavior = EditorGUILayout.ObjectField("Script", target, typeof(BodyLockedFollowBehaviorConfiguration), false) as BodyLockedFollowBehaviorConfiguration;
                }

                behavior.followMode = (BodyLockedFollowBehavior.Mode)EditorGUILayout.EnumPopup("Follow Mode", behavior.followMode);

                EditorGUILayout.Separator();
                EditorGUILayout.LabelField("Position Configuration", EditorStyles.boldLabel);
                behavior.offsetFromFollowPoint = EditorGUILayout.Vector3Field("Offset From Follow Point", behavior.offsetFromFollowPoint);
                behavior.horizontalRestAngle = EditorGUILayout.FloatField("Horizontal Rest Angle", behavior.horizontalRestAngle);
                using (new EditorGUI.DisabledScope(behavior.followMode == BodyLockedFollowBehavior.Mode.Belt))
                {
                    behavior.verticalRestAngle = EditorGUILayout.FloatField("Vertical Rest Angle", behavior.verticalRestAngle);
                }

                EditorGUILayout.Separator();
                EditorGUILayout.LabelField("Rotation Configuration", EditorStyles.boldLabel);
                behavior.lookMode = (BodyLockedFollowBehavior.LookMode)EditorGUILayout.EnumPopup("Rotate Towards Follow Point", behavior.lookMode);

                EditorGUILayout.Separator();
                EditorGUILayout.LabelField("Animation Configuration", EditorStyles.boldLabel);
                behavior.targetAnimationTime = EditorGUILayout.FloatField("Target Animation Time", behavior.targetAnimationTime);

                EditorUtility.SetDirty(behavior);
            }
        }
#endif
    }
}
