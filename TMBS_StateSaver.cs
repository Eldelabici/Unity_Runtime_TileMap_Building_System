using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
/*
namespace TileMapBuildSystem
{
    /// <summary>
    /// Manages undo/redo state storage for tilemaps with limited history.
    /// </summary>
    public static class TMBS_StateSaver
    {
        // State storage per tilemap
        private static readonly Dictionary<Tilemap, Stack<TilemapState>> undoStacks = new();
        private static readonly Dictionary<Tilemap, Stack<TilemapState>> redoStacks = new();
        private static int maxStates = 252;

        // Public accessors
        public static int CurrentStateCount => GetTotalStateCount();
        
        /// <summary>
        /// Initializes the state saver for a specific tilemap.
        /// </summary>
        public static void Initialize(Tilemap tilemap, int max = 252)
        {
            maxStates = max;
            
            if (tilemap == null)
            {
                Debug.LogError("[TMBS_StateSaver] Tilemap is null during initialization");
                return;
            }

            // Create stacks if they don't exist
            if (!undoStacks.ContainsKey(tilemap))
                undoStacks[tilemap] = new Stack<TilemapState>();
            
            if (!redoStacks.ContainsKey(tilemap))
                redoStacks[tilemap] = new Stack<TilemapState>();

            // Save initial state
            SaveCurrentState(tilemap);
        }

        /// <summary>
        /// Saves the current tilemap state to the undo stack.
        /// </summary>
        public static void SaveCurrentState(Tilemap tilemap)
        {
            if (tilemap == null)
            {
                Debug.LogError("[TMBS_StateSaver] Cannot save state - tilemap is null");
                return;
            }

            // Ensure stacks exist
            if (!undoStacks.ContainsKey(tilemap))
                undoStacks[tilemap] = new Stack<TilemapState>();

            var stack = undoStacks[tilemap];

            // Capture current state
            TilemapState state = new TilemapState
            {
                bounds = tilemap.cellBounds,
                tiles = tilemap.GetTilesBlock(tilemap.cellBounds),
                timestamp = Time.time
            };

            stack.Push(state);

            // Clear redo stack when new action is performed
            if (redoStacks.ContainsKey(tilemap))
                redoStacks[tilemap].Clear();

            // Enforce max states limit
            if (stack.Count > maxStates)
            {
                var temp = new List<TilemapState>(stack);
                temp.RemoveAt(temp.Count - 1); // Remove oldest
                undoStacks[tilemap] = new Stack<TilemapState>(temp);
            }
        }

        /// <summary>
        /// Restores the previous tilemap state.
        /// </summary>
        public static bool Undo(Tilemap tilemap)
        {
            if (tilemap == null || !undoStacks.ContainsKey(tilemap))
                return false;

            var undoStack = undoStacks[tilemap];
            
            // Need at least 2 states (current + previous)
            if (undoStack.Count < 2)
                return false;

            // Save current state to redo stack
            var currentState = undoStack.Pop();
            if (!redoStacks.ContainsKey(tilemap))
                redoStacks[tilemap] = new Stack<TilemapState>();
            
            redoStacks[tilemap].Push(currentState);

            // Restore previous state
            var previousState = undoStack.Peek();
            RestoreState(tilemap, previousState);
            
            return true;
        }

        /// <summary>
        /// Re-applies a previously undone state.
        /// </summary>
        public static bool Redo(Tilemap tilemap)
        {
            if (tilemap == null || !redoStacks.ContainsKey(tilemap))
                return false;

            var redoStack = redoStacks[tilemap];
            
            if (redoStack.Count == 0)
                return false;

            // Get state to redo
            var stateToRedo = redoStack.Pop();
            
            // Push current state back to undo
            if (!undoStacks.ContainsKey(tilemap))
                undoStacks[tilemap] = new Stack<TilemapState>();
            
            undoStacks[tilemap].Push(stateToRedo);

            // Restore the redo state
            RestoreState(tilemap, stateToRedo);
            
            return true;
        }

        /// <summary>
        /// Checks if undo is available.
        /// </summary>
        public static bool CanUndo(Tilemap tilemap)
        {
            return tilemap != null 
                && undoStacks.ContainsKey(tilemap) 
                && undoStacks[tilemap].Count >= 2;
        }

        /// <summary>
        /// Checks if redo is available.
        /// </summary>
        public static bool CanRedo(Tilemap tilemap)
        {
            return tilemap != null 
                && redoStacks.ContainsKey(tilemap) 
                && redoStacks[tilemap].Count > 0;
        }

        /// <summary>
        /// Clears all saved states for all tilemaps.
        /// </summary>
        public static void ClearAllStates()
        {
            foreach (var kvp in undoStacks)
                kvp.Value.Clear();
            
            foreach (var kvp in redoStacks)
                kvp.Value.Clear();
            
            undoStacks.Clear();
            redoStacks.Clear();
        }

        // Private helpers
        private static void RestoreState(Tilemap tilemap, TilemapState state)
        {
            tilemap.ClearAllTiles();
            tilemap.SetTilesBlock(state.bounds, state.tiles);
        }

        private static int GetTotalStateCount()
        {
            int total = 0;
            foreach (var kvp in undoStacks)
                total += kvp.Value.Count;
            return total;
        }

        // Internal state structure
        private class TilemapState
        {
            public BoundsInt bounds;
            public TileBase[] tiles;
            public float timestamp;
        }
    }
}*/