using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using SensorsSDK.EditorUtilities;
using SensorsSDK.UnityUtilities;

namespace SensorsSDK.WmrUtilities
{
    public enum LsrPlaneOrientation
    {
        FaceCamera,
        FaceForward,
        FaceBackward,
        FaceUp,
        FaceDown
    }

    public enum LsrPlanePriority
    {
        Serious,
        Normal,
        Low
    }

    public interface IVelocityProvider
    {
        Vector3 GetVelocity();
    }

    public class ReprojectionTarget : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("How to align the normal for the reprojection plane.")]
        private LsrPlaneOrientation _orientation = LsrPlaneOrientation.FaceCamera;

        [SerializeField]
        [Tooltip("How important the object is for LSR. Selection cascades down. Multiple 'Serious' priority objects will trigger a warning.")]
        private LsrPlanePriority _priority = LsrPlanePriority.Normal;

        [SerializeField]
        [Tooltip("The two dimensional angle around the camera forward ray you want to search for this objects transform in.")]
        private Vector2 _relevantAngle = new Vector2(45, 45);

        [SerializeField]
        private Renderer _dependentRenderer = null;

        [SerializeField]
        private bool _applyVelocity = true;

        [SerializeField]
        //[CanBeNull]
        [RequireInterface(typeof(IVelocityProvider))]
        private MonoBehaviour _velocityProvider = null;

        private Vector3 _lastPosition = Vector3.zero;
        private Vector3 _calculatedVelocity = Vector3.zero;
        private Coroutine _velocityUpdateCoroutine = null;

        // TODO: Consider doing averaging of content across the screen.

        // TODO: Consider implementing a bounds test for objects that directly have a renderer.
        //  Grab the Renderer.Bounds of the object.
        //  Project the 8 corners of the AABB into NDC space.
        //  Calculate coverage.
        //  Calculate Sort by coverage.

        // TODO: Consider a ray cast based solution.

        public Vector3 Velocity
        {
            get
            {
                if (_applyVelocity)
                {
                    if (_velocityUpdateCoroutine != null)
                    {
                        return _calculatedVelocity;
                    }

                    var pv = _velocityProvider as IVelocityProvider;
                    if (pv != null)
                    {
                        return pv.GetVelocity();
                    }
                }

                return Vector3.zero;
            }
        }

        public LsrPlaneOrientation Orientation
        {
            get { return _orientation; }
        }

        public LsrPlanePriority Priority
        {
            get { return _priority; }
        }

        public Vector2 RelevantAngle
        {
            get { return _relevantAngle; }
        }

        public bool ApplyVelocity
        {
            get { return _applyVelocity; }
        }

        public bool IsValid
        {
            get { return this.gameObject.activeInHierarchy && (_dependentRenderer == null || _dependentRenderer.enabled) && this.enabled == true; }
        }

        private void OnEnable()
        {
            if (ReprojectionManager.IsAvailable)
            {
                ReprojectionManager.Instance.AddReprojectionTarget(this);
            }

            if (_applyVelocity && _velocityProvider == null)
            {
                _lastPosition = transform.position;
                _velocityUpdateCoroutine = StartCoroutine(UpdateCalculatedVelocity());
            }
        }

        private void OnDisable()
        {
            if (ReprojectionManager.IsAvailable)
            {
                ReprojectionManager.Instance.RemoveReprojectionTarget(this);
            }

            if (_velocityUpdateCoroutine != null)
            {
                StopCoroutine(_velocityUpdateCoroutine);
                _velocityUpdateCoroutine = null;
            }
            _calculatedVelocity = Vector3.zero;
            _lastPosition = Vector3.zero;
        }

        private IEnumerator UpdateCalculatedVelocity()
        {
            while (true)
            {
                _calculatedVelocity = (transform.position - _lastPosition) / Time.deltaTime;
                _lastPosition = transform.position;
                yield return null;
            }
        }
    }
}
