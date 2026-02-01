using UnityEngine;
using UnityEngine.Tilemaps;
using System;

namespace TileMapBuildSystem
{
    /// <summary>
    /// Handles tile placement and erasure logic during build interactions.
    /// Operates directly on the preview and base tilemaps.
    /// </summary>
    public class TMBS_TilePlacementManager
    {
        public RuleTile BuildedTile { get; set; }

        private readonly Tilemap previewTilemap;    // Instance of the preview
        private readonly Tilemap baseTilemap;       // Original tilemap to build

        private TileBase[] tileBuffer;               // Reusable tile buffer

        private Vector3Int BlockPosition;            // Pivot of the BoundsInt used to build the block
        private Vector3Int BlockSize;                // Size of the block
        private BoundsInt currentBounds;
        private BoundsInt previousBounds;
        private BoundsInt previewCellBounds;

        public TMBS_TilePlacementManager(RuleTile tile, Tilemap preview, Tilemap baseMap)
        {
            BuildedTile = tile;
            previewTilemap = preview;
            baseTilemap = baseMap;

            if (BuildedTile == null) Debug.LogError("TMBS_TilePlacementManager: Build tile is null.");

            tileBuffer = null;

            BlockPosition = Vector3Int.zero;
            BlockSize = Vector3Int.zero;
            currentBounds = new BoundsInt();
            previousBounds = new BoundsInt();
            previewCellBounds = new BoundsInt();
        }

        public void Cleanup()
        {
            tileBuffer = null;
        }

        public void PlaceTileInPreview(in Vector2Int cell)
        {
            // temporaly just a 0 height
            Vector3Int cell3d = new Vector3Int(cell.x, cell.y,0);
            baseTilemap.SetTile(cell3d, BuildedTile);
        }

        /// <summary>
        /// Places preview tiles between the provided start and end positions (already validated externally).
        /// </summary>
        public void PlaceTilesInPreview(in Vector2Int start, in Vector2Int end)
        {
            //if (previewTilemap == null || BuildedTile == null) return;

            int minX = Mathf.Min(start.x, end.x);
            int minY = Mathf.Min(start.y, end.y);
            int maxX = Mathf.Max(start.x, end.x);
            int maxY = Mathf.Max(start.y, end.y);

            BlockPosition.Set(minX, minY, 0);

            BlockSize.Set(
                maxX - minX + 1,
                maxY - minY + 1,
                1
            );

            int requiredCount = BlockSize.x * BlockSize.y;

            // Grow buffer only if needed
            if (tileBuffer == null || tileBuffer.Length < requiredCount) tileBuffer = new TileBase[requiredCount];

            // Fill only the required amount
            for (int i = 0; i < requiredCount; i++) tileBuffer[i] = BuildedTile;

            currentBounds.position = BlockPosition;
            currentBounds.size = BlockSize;

            ClearPreviousArea();

            previewTilemap.SetTilesBlock(currentBounds, tileBuffer);

            previousBounds.position = currentBounds.position;
            previousBounds.size = currentBounds.size;
        }

        /// <summary>
        /// Commits the preview tiles to the base tilemap.
        /// </summary>
        public void ApplyPreviewToBase()
        {
            if (previewTilemap == null || baseTilemap == null) return;

            previewCellBounds = previewTilemap.cellBounds;

            foreach (Vector3Int pos in previewCellBounds.allPositionsWithin)
            {
                TileBase tile = previewTilemap.GetTile(pos);
                if (tile != null)
                    baseTilemap.SetTile(pos, tile);
            }
        }

        /// <summary>
        /// Erases tiles from the base tilemap where preview tiles exist.
        /// </summary>
        public void EraseFromBase()
        {
            if (previewTilemap == null || baseTilemap == null) return;

            previewCellBounds = previewTilemap.cellBounds;

            foreach (Vector3Int pos in previewCellBounds.allPositionsWithin)
            {
                TileBase tile = previewTilemap.GetTile(pos);
                if (tile != null)
                    baseTilemap.SetTile(pos, null);
            }
        }

        private void ClearPreviousArea()
        {
            if (previousBounds.size.x <= 0 || previousBounds.size.y <= 0)
                return;

            if (previousBounds.Equals(currentBounds))
                return;

            bool outOfBounds =
                previousBounds.xMin < currentBounds.xMin ||
                previousBounds.xMax > currentBounds.xMax ||
                previousBounds.yMin < currentBounds.yMin ||
                previousBounds.yMax > currentBounds.yMax;

            if (outOfBounds)
                previewTilemap.ClearAllTiles();
        }
    }
}
