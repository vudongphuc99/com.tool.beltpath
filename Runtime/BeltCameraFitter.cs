using UnityEngine;

namespace Tool.BeltPath
{
    /// <summary>
    /// Keeps a top-down orthographic camera framed on the belt. Call <see cref="SnapTo"/> with the world bounds of
    /// the belt; the frame is solved again when the screen size changes or the settings are edited during play.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class BeltCameraFitter : MonoBehaviour
    {
        private const float DefaultPaddingWorldUnits = 1f;
        private const float DefaultMinOrthoSize = 3f;

        [Tooltip("World units kept between the belt and each screen edge")]
        [SerializeField, Min(0f)] private float padding = DefaultPaddingWorldUnits;
        [Tooltip("The camera never zooms in further than this orthographic size")]
        [SerializeField, Min(0.01f)] private float minOrthoSize = DefaultMinOrthoSize;

        private Camera targetCamera;
        private Bounds lastContent;
        private bool hasContent;
        private int lastScreenWidth;
        private int lastScreenHeight;

        private void Awake()
        {
            CacheComponents();
        }

        // Two int compares per frame; SnapTo stores the new size.
        private void Update()
        {
            if (hasContent && (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight))
            {
                SnapTo(lastContent);
            }
        }

        private void OnValidate()
        {
            if (Application.isPlaying && hasContent)
            {
                SnapTo(lastContent);
            }
        }

        /// <summary>Frames the world-space bounds immediately.</summary>
        public void SnapTo(Bounds worldBounds)
        {
            CacheComponents();
            lastContent = worldBounds;
            hasContent = true;
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;

            var content = BeltCameraFitMath.ProjectToScreenPlane(worldBounds);
            var fit = BeltCameraFitMath.Solve(content, targetCamera.aspect, padding, minOrthoSize);
            targetCamera.orthographicSize = fit.OrthoSize;
            var position = transform.position;
            position.x = fit.Center.x;
            position.z = fit.Center.y;
            transform.position = position;
        }

        private void CacheComponents()
        {
            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>();
            }
        }
    }
}
