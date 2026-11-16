using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

namespace Tool.BeltPath
{
    /// <summary>
    /// Lets the belt of a <see cref="BeltView"/> be edited with Unity's spline tool. The spline holds one knot per
    /// path node; the channel and water are built again from those nodes after every edit, in edit mode too.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BeltView))]
    [RequireComponent(typeof(SplineContainer))]
    [AddComponentMenu("BeltPath/Belt Path Authoring")]
    public sealed class BeltPathAuthoring : MonoBehaviour
    {
        private const int MinKnotCount = 2;
        private const float PathGizmoStep = 0.25f;
        private const float PathGizmoLift = 0.05f;

        private static readonly List<BeltPathAuthoring> ActiveInstances = new List<BeltPathAuthoring>();

        [SerializeField] private bool liveEdit = true;

        private BeltView view;
        private SplineContainer container;
        private bool loading;
        private bool rebuildPending;

        /// <summary>Enabled instances; the editor updater walks this list to apply pending rebuilds.</summary>
        public static IReadOnlyList<BeltPathAuthoring> Instances => ActiveInstances;

        private void OnEnable()
        {
            view = GetComponent<BeltView>();
            container = GetComponent<SplineContainer>();
            if (!ActiveInstances.Contains(this))
            {
                ActiveInstances.Add(this);
            }

            Spline.Changed += HandleSplineChanged;
            view.FileLoaded += HandleFileLoaded;
            RestoreOrLoad();
        }

        private void OnDisable()
        {
            Spline.Changed -= HandleSplineChanged;
            if (view != null)
            {
                view.FileLoaded -= HandleFileLoaded;
            }

            ActiveInstances.Remove(this);
        }

        /// <summary>Puts the nodes of a path into the spline, replacing whatever was there.</summary>
        public void LoadPath(BeltPathData data)
        {
            loading = true;
            BeltNodeSplineConverter.Populate(container.Spline, data.Nodes, data.IsClosed, view.Shape);
            loading = false;
            rebuildPending = false;
        }

        /// <summary>The path described by the spline now.</summary>
        public BeltPathData ToPathData()
        {
            var spline = container.Spline;
            return new BeltPathData(BeltNodeSplineConverter.ToNodes(spline, view.Shape), spline.Closed);
        }

        /// <summary>Builds the belt from the spline; does nothing while the spline has fewer than two knots.</summary>
        public void RebuildFromSpline()
        {
            if (container.Spline.Count < MinKnotCount)
            {
                return;
            }

            view.Build(ToPathData());
        }

        public void RequestRebuild()
        {
            rebuildPending = liveEdit;
        }

        public void ApplyPendingRebuild()
        {
            if (!rebuildPending)
            {
                return;
            }

            rebuildPending = false;
            RebuildFromSpline();
        }

        private void RestoreOrLoad()
        {
            if (container.Spline.Count >= MinKnotCount)
            {
                // Edits saved in the scene win over the file path that the view just built.
                RebuildFromSpline();
                return;
            }

            if (view.BuiltData != null)
            {
                LoadPath(view.BuiltData);
            }
        }

        private void HandleFileLoaded(BeltPathData data)
        {
            LoadPath(data);
        }

        private void HandleSplineChanged(Spline spline, int knotIndex, SplineModification modification)
        {
            if (loading || container == null || !ReferenceEquals(spline, container.Spline))
            {
                return;
            }

            RequestRebuild();
        }

        // Shows the path the channel really follows (rounded corners included) next to the spline's own curve.
        private void OnDrawGizmosSelected()
        {
            if (view == null || view.Path == null)
            {
                return;
            }

            var path = view.Path;
            Gizmos.color = Color.green;
            Gizmos.matrix = view.transform.localToWorldMatrix;
            var lift = Vector3.up * PathGizmoLift;
            var previous = path.Position(0f) + lift;
            for (var distance = PathGizmoStep; distance < path.Length; distance += PathGizmoStep)
            {
                var point = path.Position(distance) + lift;
                Gizmos.DrawLine(previous, point);
                previous = point;
            }

            Gizmos.DrawLine(previous, path.Position(path.Length) + lift);
        }
    }
}
