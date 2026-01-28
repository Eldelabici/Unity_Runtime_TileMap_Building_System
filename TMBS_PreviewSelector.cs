using System;
using UnityEngine;
using TMBS_GridExtensions;
using System.Runtime.CompilerServices;

namespace TileMapBuildSystem
{
    /// <summary>
    /// Handles tilemap grid cell selection state.
    /// Converts screen positions into grid-based coordinates.
    /// No longer manages events - state container only.
    /// </summary>
    public sealed class TMBS_PreviewSelector
    {
        public int Height;
        public Vector2Int StartCell;
        public Vector2Int EndCell;
        public bool ShiftAction;
        public bool IsDragging;
        public bool IsDragFinished;
        public bool IsClickFinished;

        private Grid buildGrid;
        private Camera targetCamera;
        private TMBS_InputManager inputManager;

        public TMBS_PreviewSelector(Grid grid, Camera camera, TMBS_InputManager inputMgr)
        {
            buildGrid = grid;
            targetCamera = camera;
            inputManager = inputMgr;
            ResetSelection();
            
            // Subscribe to input manager events
            if (inputManager != null)
            {
                inputManager.OnDragStart += HandleDragStart;
                inputManager.OnDragUpdate += HandleDragUpdate;
                inputManager.OnDragEnd += HandleDragEnd;
                inputManager.OnClickStart += HandleClick;
                inputManager.OnShiftStateChanged += HandleShiftChanged;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Cleanup()
        {
            // Unsubscribe from input manager events
            if (inputManager != null)
            {
                inputManager.OnDragStart -= HandleDragStart;
                inputManager.OnDragUpdate -= HandleDragUpdate;
                inputManager.OnDragEnd -= HandleDragEnd;
                inputManager.OnClickStart -= HandleClick;
                inputManager.OnShiftStateChanged -= HandleShiftChanged;
            }

            buildGrid = null;
            targetCamera = null;
            inputManager = null;
            IsDragging = false;
        }

        // Input handlers - update internal state only
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HandleDragStart(Vector2 screenPosition)
        {
            if (buildGrid == null || targetCamera == null)
                return;

            IsDragging = true;
            IsDragFinished = false;

            StartCell = buildGrid.UIPositionToCell(targetCamera, screenPosition);
            EndCell = StartCell;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HandleDragUpdate(Vector2 screenPosition)
        {
            if (!IsDragging || buildGrid == null || targetCamera == null)
                return;

            Vector2Int cell = buildGrid.UIPositionToCell(targetCamera, screenPosition);
            if (cell == EndCell)
                return;

            EndCell = cell;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HandleDragEnd(Vector2 screenPosition)
        {
            if (!IsDragging)
                return;

            IsDragging = false;
            IsDragFinished = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HandleClick(Vector2 screenPosition)
        {
            if (buildGrid == null || targetCamera == null)
                return;

            Vector2Int cell = buildGrid.UIPositionToCell(targetCamera, screenPosition);
            StartCell = cell;
            EndCell = cell;

            IsClickFinished = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HandleShiftChanged(bool isPressed)
        {
            ShiftAction = isPressed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResetSelection()
        {
            StartCell = Vector2Int.zero;
            EndCell = Vector2Int.zero;
            IsDragFinished = false;
            IsClickFinished = false;
            IsDragging = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2Int GetSelectionSize()
        {
            return new Vector2Int(
                Mathf.Abs(EndCell.x - StartCell.x) + 1,
                Mathf.Abs(EndCell.y - StartCell.y) + 1
            );
        }
    }
}