using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TileMapBuildSystem
{
    /// <summary>
    /// Manages the preview tilemap for the build system.
    /// Creates a semi-transparent clone of the base tilemap to visualize tile placement.
    /// </summary>
    public class TMBS_PreviewTileMap
    {
        // properties
        public Tilemap ActivePreview { get; private set; }
        public bool IsActive { get; private set; }

        // Main event
        public event Action<Tilemap> OnPreviewCreated;

        // Constants
        private const string PREVIEW_SUFFIX = "_Preview";
        // The preview are supposed to be transparent
        private static readonly Color PREVIEW_COLOR = new Color(1f, 1f, 1f, 0.5f);

        // References
        private readonly Tilemap sourceTilemap;
        private readonly Transform parentTransform;
        private GameObject previewContainer;
        private TilemapRenderer previewRenderer;

        // Constructor
        public TMBS_PreviewTileMap(Tilemap source)
        {
            sourceTilemap = source;
            parentTransform = sourceTilemap.transform.parent;
            CreatePreview();
        }

        // Lifecycle methods
        public void Show()
        {
            if (previewContainer == null) return;
            IsActive = true;
            previewContainer.SetActive(true);
        }

        public void Hide()
        {
            if (previewContainer == null) return;
            IsActive = false;
            previewContainer.SetActive(false);
        }

        // Reset

        /// <summary>
        /// Clear all the preview Tilemap.
        /// </summary>
        public void ClearTiles()
        {
            ActivePreview?.ClearAllTiles();
        }
            
        // Note, no clear tiles, this is to remove some garbage
        public void Cleanup()
        {
            if (previewContainer != null)
            {
                UnityEngine.Object.Destroy(previewContainer);
                previewContainer = null;
            }

            ActivePreview = null;
            previewRenderer = null;
            OnPreviewCreated = null;
        }

        // Creator of the preview
        /// <summary>
        /// Create a transparent version of the base tilemap.
        /// <see cref="ActivePreview" />Is the reference of the preview
        /// </summary>
        private void CreatePreview()
        {
            previewContainer = new GameObject(sourceTilemap.name + PREVIEW_SUFFIX);
            previewContainer.transform.SetParent(parentTransform, false);
            previewContainer.SetActive(false);

            ActivePreview = previewContainer.AddComponent<Tilemap>();
            previewRenderer = previewContainer.AddComponent<TilemapRenderer>();

            ActivePreview.tileAnchor = sourceTilemap.tileAnchor;
            ActivePreview.orientation = sourceTilemap.orientation;
            ActivePreview.orientationMatrix = sourceTilemap.orientationMatrix;
            ActivePreview.color = PREVIEW_COLOR;

            BoundsInt bounds = sourceTilemap.cellBounds;
            TileBase[] tiles = sourceTilemap.GetTilesBlock(bounds);
            ActivePreview.SetTilesBlock(bounds, tiles);

            previewRenderer.sortingOrder = 10; // Probably this would be variable o changeable.

            OnPreviewCreated?.Invoke(ActivePreview);
        }
    }
}