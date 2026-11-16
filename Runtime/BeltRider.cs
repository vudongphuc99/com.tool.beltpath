using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Tool.BeltPath
{
    /// <summary>
    /// Moves this object along the path of a <see cref="BeltView"/> and turns it to face the travel direction in the
    /// space of the belt. When the BeltView sits under a tilted plane root (the reference game's PlaneRoot), the duck
    /// inherits that tilt, so <c>Tilt Degrees</c> stays 0; with a flat BeltView set it to
    /// <see cref="BeltCameraFacing.DefaultTiltDegrees"/> to lean the duck toward the camera.
    /// Put the duck model on a child. A model whose front is -Z (such as yazi_01) needs a local rotation of
    /// Y = 180 on that child, so its beak points along the belt.
    /// The speed is in grid cells per second, like the reference game's BeltMoveCellsPerSecond (24).
    /// </summary>
    public sealed class BeltRider : MonoBehaviour
    {
        /// <summary>Normal belt speed of the reference game (its BeltMoveCellsPerSecond); the game adds up to +8% on loose belts.</summary>
        public const float DefaultSpeedCellsPerSecond = 24f;

        [SerializeField] private BeltView beltView;
        [SerializeField] private float distance;
        [SerializeField, FormerlySerializedAs("speed")] private float speedCellsPerSecond = DefaultSpeedCellsPerSecond;
        [SerializeField] private float tiltDegrees;
        [SerializeField] private Vector3 worldOffset;

        /// <summary>Distance along the belt in world units; wraps on closed belts, clamps on open ones.</summary>
        public float Distance
        {
            get => distance;
            set => distance = value;
        }

        private Quaternion lastRotation = Quaternion.identity;

        private void Awake()
        {
            if (beltView == null)
            {
                Debug.LogException(new InvalidOperationException("BeltRider has no BeltView assigned."), this);
            }
        }

        private void LateUpdate()
        {
            if (beltView == null || beltView.Path == null)
            {
                return;
            }

            distance += speedCellsPerSecond * beltView.Shape.CellSize * Time.deltaTime;
            ApplyPose();
        }

        private void ApplyPose()
        {
            var path = beltView.Path;
            var belt = beltView.transform;
            var position = belt.TransformPoint(path.Position(distance)) + worldOffset;
            if (BeltCameraFacing.TryGetRotation(path.Forward(distance), tiltDegrees, out var localRotation))
            {
                lastRotation = belt.rotation * localRotation;
            }

            transform.SetPositionAndRotation(position, lastRotation);
        }
    }
}
