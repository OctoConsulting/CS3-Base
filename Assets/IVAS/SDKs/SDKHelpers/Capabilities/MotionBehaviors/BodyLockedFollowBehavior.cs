using System;
using UnityEngine;
using SensorsSDK.UnityUtilities;
using SensorsSDK.WmrUtilities;

namespace SensorsSDK.MotionBehaviors
{
    /// <summary>
    /// Locks a transform to a sphere around the camera. Transforms are maintained in 3dof space to ensure smoothnes while moving. 
    /// </summary>
    public class BodyLockedFollowBehavior : MonoBehaviour, IVelocityProvider
    {
        public enum Mode
        {
            Sphere,
            Belt
        }

        public enum LookMode
        {
            NoLook,
            XZRotate,
            BillboardNoRoll,
            BillboardWithRoll
        }

        [SerializeField]
        private BodyLockedFollowBehaviorConfiguration configuration = null;

        private Transform transformToMove = null;
        private Transform transformToFollow = null;
        private Vector3 velocity = Vector3.zero;
        private float rotationalVelocity = 0;

        private Vector3 currentDirection = Vector3.forward;
        private bool needsAlignment = false;

        public event Action<Vector3> OnUpdateFollowPosition = null;
        public event Action<Vector3> OnUpdateFollowDirection = null;

        public BodyLockedFollowBehaviorConfiguration Configuration
        {
            get { return configuration; }
            set { configuration = value; }
        }

        public Vector3 GetVelocity()
        {
            return velocity;
        }

        private void OnEnable()
        {
            needsAlignment = true;
        }

        private void Update()
        {
            UpdatePositioning();
        }

        private void UpdatePositioning()
        {
            if (EnsureTransforms())
            {
                if (needsAlignment)
                {
                    AlignToCenter();
                    needsAlignment = false;
                }
                else
                {
                    UpdateDirection();
                }

                UpdateTransformToMove();
            }
        }

        private bool EnsureTransforms()
        {
            if (transformToFollow == null)
            {
                transformToFollow = CameraProvider.MainCamera.transform;
            }

            if (transformToMove == null)
            {
                transformToMove = this.transform;
            }

            return transformToFollow != null && transformToMove != null;
        }

        public void AlignToCenter()
        {
            if (!WmrTracker.Instance.ValidFrame)
            {
                return;
            }

            Vector3 forward;
            switch (configuration.FollowMode)
            {
                default:
                case Mode.Sphere:
                    forward = transformToFollow.forward;
                    break;

                case Mode.Belt:
                    forward = Vector3.ProjectOnPlane(transformToFollow.forward, Vector3.up);
                    if (forward.sqrMagnitude == 0)
                    {
                        forward = Vector3.forward;
                    }
                    break;
            }

            this.currentDirection = WmrTracker.Instance.ThreeDofFromWorld.MultiplyVector(forward);
            this.velocity = Vector3.zero;
            this.rotationalVelocity = 0;
        }

        private void UpdateDirection()
        {
            if (!WmrTracker.IsAvailable || !WmrTracker.Instance || !WmrTracker.Instance.ValidFrame)
            {
                return;
            }

            // NOTE: Because we do this in a parent space, there is degenerate behavior when using
            // an axis and the vectors are close to the axis. This is because small angles will
            // start to look very large when the major component is projected out of it.
            // Functionally, this is *very* noticeable when using Vector.right and rotationg
            // horizontally. The vertical adjustment will actually counteract the horizontal one and
            // you'll leave the object behind. Because of this, we use the basis angle's right
            // vector, which prevents this issue, but introduces a slide when looking up and down.
            // Trying to use followUp as part of the horizontal clamp prevents this bad behavior on
            // vertical movements, but introduces a vertical slide when rotating. NOT what we want.
            // We take this direction because you tend not to notice the negatives of each approach
            // due to how your head and world alignment act. Hopefully this doesn't come back to
            // bite us once people start crawling around on the ground...

            Matrix4x4 folowTransform3Dof = WmrTracker.Instance.ThreeDofFromWorld * transformToFollow.localToWorldMatrix;
            Vector3 followRight = folowTransform3Dof.GetColumn(0);
            // Vector3 followUp = folowTransform3Dof.GetColumn(1);
            Vector3 followForward = folowTransform3Dof.GetColumn(2);

            // Clamp to the rest angle to see where we're supposed to be.
            Vector3 targetDirection = currentDirection;
            switch (configuration.FollowMode)
            {
                default:
                case Mode.Sphere:
                    targetDirection = MathfExtension.ClampVectorByAngleOnPlane(followForward, targetDirection, configuration.HorizontalRestAngle, 1);
                    targetDirection = MathfExtension.ClampVectorByAngleOnPlane(followForward, targetDirection, configuration.VerticalRestAngle, followRight);
                    break;
                case Mode.Belt:
                    targetDirection = MathfExtension.ClampVectorByAngleOnPlane(followForward, targetDirection, configuration.HorizontalRestAngle, 1);
                    targetDirection = Vector3.ProjectOnPlane(targetDirection, Vector3.up);
                    if (targetDirection.sqrMagnitude == 0)
                    {
                        targetDirection = Vector3.forward;
                    }
                    break;
            }

            // If clamping has adjusted our target position, move to that position.
            if (currentDirection != targetDirection)
            {
                Vector3 lastCurrentDirection = this.currentDirection;

                if (configuration.TargetAnimationTime > 0)
                {
                    float delta = Vector3.Angle(currentDirection, targetDirection);
                    float newDelta = Mathf.SmoothDamp(0, delta, ref rotationalVelocity, configuration.TargetAnimationTime, Mathf.Infinity, Time.deltaTime);
                    this.currentDirection = Quaternion.AngleAxis(newDelta, Vector3.Cross(currentDirection, targetDirection)) * currentDirection;
                }
                else
                {
                    this.currentDirection = targetDirection;
                    this.rotationalVelocity = 0;
                }
                OnUpdateFollowDirection?.Invoke(this.currentDirection);
                // this.velocity = ((Quaternion.LookRotation(currentDirection, Vector3.up) * offsetFromFollowPoint) - (Quaternion.LookRotation(lastCurrentDirection, Vector3.up) * offsetFromFollowPoint)) / Time.deltaTime;
            }
            else
            {
                this.velocity = Vector3.zero;
                this.rotationalVelocity = 0;
            }
        }

        private void UpdateTransformToMove()
        {
            if (!WmrTracker.IsAvailable ||!WmrTracker.Instance.ValidFrame)
            {
                return;
            }

            Vector3 forward = WmrTracker.Instance.WorldSpaceFromThreeDofSpace.MultiplyVector(this.currentDirection);
            Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);
            Vector3 targetPosition = transformToFollow.position + rotation * configuration.OffsetFromFollowPoint;
            
            if (targetPosition != transformToMove.position)
            {
                transformToMove.position = targetPosition;
                OnUpdateFollowPosition?.Invoke(targetPosition);
            }

            switch (configuration.LookMode)
            {
                case LookMode.NoLook:
                    // Do nothing
                    break;
                case LookMode.XZRotate:
                    Vector3 dirToTransform = (transformToMove.position - transformToFollow.position).normalized;
                    dirToTransform = Vector3.ProjectOnPlane(dirToTransform, Vector3.up);
                    transformToMove.rotation = Quaternion.LookRotation(dirToTransform, Vector3.up);
                    break;
                case LookMode.BillboardNoRoll:
                    transformToMove.rotation = Quaternion.LookRotation((transformToMove.position - transformToFollow.position).normalized, Vector3.up);
                    break;
                case LookMode.BillboardWithRoll:
                    transformToMove.rotation = transformToFollow.rotation;
                    break;
            }
        }
    }
}
