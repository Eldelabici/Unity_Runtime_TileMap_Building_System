using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using TileMapBuildSystem;
using TMBS_GridExtensions;
using System;

public class TMBS_InGameTileMapBuilding : MonoBehaviour
{
    [Header("Tilemap References")]
    public Tilemap baseTilemap;

    [Header("Build Settings")]
    public RuleTile buildTile;

    [Header("Grid Reference")]
    public Grid buildGrid;

    [Header("Input Configuration")]
    public InputActionReference dragInput;
    public InputActionReference clickInput;
    public InputActionReference shiftInput;

/*
    [Header("State Management")]
    public bool enableStateSaver = true;
    [Range(1, 2048)]
    public int maxUndoStates = 252;*/
    public bool autoApplyOnDragFinish = true;

#if UNITY_EDITOR
    [Header("Debug Display")]
    public bool showDebugOnScreen = true;
    public int fontSize = 12;
#endif

    // Core systems
    private Camera constructorCamera;
    private TMBS_PreviewSelector previewSelector;
    private TMBS_TilePlacementManager tilePlacementManager;
    private TMBS_InputManager inputManager;
    private TMBS_PreviewTileMap previewTileMap;
    private Mouse activeMouse;

    // State tracking
    private int totalSaveCount;
    private Vector2Int currentCell;
    private Vector2Int previousEndCell;
    private bool hasMovedDuringDrag;

#if UNITY_EDITOR
    private string currentMode = "Build";
    private string lastAction = "Idle";
    private GUIStyle debugStyle;
#endif

    // Lifecycle
    private void Start()
    {
        if (!ValidateReferences()) return;
        CacheReferences();
        InitializeSystems();
        SubscribeToEvents();
    }

    private void OnEnable() => inputManager?.EnableInputs();
    private void OnDisable() => inputManager?.DisableInputs();

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
        CleanupSystems();
    }

    private void Update()
    {
        inputManager?.Update();
        
        // Undo/Redo hotkeys
        //if (enableStateSaver && Keyboard.current != null)
        //{
        //    if (Keyboard.current.leftCtrlKey.isPressed && Keyboard.current.zKey.wasPressedThisFrame) PerformUndo();
        //    if (Keyboard.current.leftCtrlKey.isPressed && Keyboard.current.yKey.wasPressedThisFrame) PerformRedo();
        //}
    }

    // Initialization
    private void InitializeSystems()
    {
        inputManager = new TMBS_InputManager(activeMouse, dragInput, clickInput, shiftInput);
        previewSelector = new TMBS_PreviewSelector(buildGrid, constructorCamera, inputManager);
        previewTileMap = new TMBS_PreviewTileMap(baseTilemap);
        tilePlacementManager = new TMBS_TilePlacementManager(buildTile, previewTileMap.ActivePreview, baseTilemap);
        
        //if (enableStateSaver) TMBS_StateSaver.Initialize(baseTilemap, maxUndoStates);

#if UNITY_EDITOR
        debugStyle = new GUIStyle { fontSize = fontSize, normal = { textColor = Color.white } };
#endif
    }

    private void SubscribeToEvents()
    {
        if (inputManager == null) return;

        inputManager.OnDragStart += OnDragStarted;
        inputManager.OnDragUpdate += OnDragUpdated;
        inputManager.OnDragEnd += OnDragEnded;
        inputManager.OnClickStart += OnClickStarted;
        inputManager.OnClickEnd += OnClickEnded;
        inputManager.OnShiftStateChanged += OnShiftChanged;
        inputManager.OnPositionUpdate += OnPositionUpdated;
    }

    private void UnsubscribeFromEvents()
    {
        if (inputManager == null) return;

        inputManager.OnDragStart -= OnDragStarted;
        inputManager.OnDragUpdate -= OnDragUpdated;
        inputManager.OnDragEnd -= OnDragEnded;
        inputManager.OnClickStart -= OnClickStarted;
        inputManager.OnClickEnd -= OnClickEnded;
        inputManager.OnShiftStateChanged -= OnShiftChanged;
        inputManager.OnPositionUpdate -= OnPositionUpdated;
    }

    // Drag event handlers
    private void OnDragStarted(Vector2 screenPos)
    {
        hasMovedDuringDrag = false;
        previousEndCell = previewSelector.StartCell;
        previewTileMap.Show();
        
        // Draw initial preview tile
        tilePlacementManager.PlaceTilesInPreview(previewSelector.StartCell, previewSelector.EndCell);
        
#if UNITY_EDITOR
        lastAction = "Drag Started";
#endif
    }

    private void OnDragUpdated(Vector2 screenPos)
    {
        // Update preview as drag area changes
        if (previewSelector.EndCell != previousEndCell)
        {
            hasMovedDuringDrag = true;
            tilePlacementManager.PlaceTilesInPreview(previewSelector.StartCell, previewSelector.EndCell);
            previousEndCell = previewSelector.EndCell;
        }
    }

    private void OnDragEnded(Vector2 screenPos)
    {
        // Skip if no movement occurred
        if (!hasMovedDuringDrag)
        {
            previewTileMap.Hide();
#if UNITY_EDITOR
            lastAction = "Drag Cancelled";
#endif
            return;
        }

        // Apply or erase based on shift state
        if (autoApplyOnDragFinish)
        {
            if (inputManager.IsShiftHeld()) tilePlacementManager.EraseFromBase();
            else tilePlacementManager.ApplyPreviewToBase();
            
            previewTileMap.ClearTiles();
        }

        previewTileMap.Hide();

        //if (enableStateSaver)
        //{
        //    TMBS_StateSaver.SaveCurrentState(baseTilemap);
        //    totalSaveCount++;
#if UNITY_EDITOR
            lastAction = $"Drag Saved #{totalSaveCount}";
#endif
        //}

        hasMovedDuringDrag = false;
    }

    // Click event handlers
    private void OnClickStarted(Vector2 screenPos)
    {
        // Place single tile immediately
        tilePlacementManager.PlaceTileInPreview(previewSelector.StartCell);

#if UNITY_EDITOR
        lastAction = "Click Placed";
#endif
    }

    private void OnClickEnded(Vector2 screenPos)
    {
        /* if (enableStateSaver)
        {
            TMBS_StateSaver.SaveCurrentState(baseTilemap);
            totalSaveCount++;
#if UNITY_EDITOR
            lastAction = $"Click Saved #{totalSaveCount}";
#endif
        } */
        if(!inputManager.IsDragging()) tilePlacementManager.ApplyPreviewToBase();
        lastAction = $"Click performed";
    }

    // Other event handlers
    private void OnShiftChanged(bool isPressed)
    {
#if UNITY_EDITOR
        currentMode = isPressed ? "Erase" : "Build";
        lastAction = $"Mode: {currentMode}";
#endif
    }

    private void OnPositionUpdated(Vector2 screenPos)
    {
        // Track current cell for debug display
        Vector2Int newCell = buildGrid.UIPositionToCell(constructorCamera, screenPos);
        if (newCell != currentCell) currentCell = newCell;
    }

    // Undo/Redo operations
    //private void PerformUndo()
    //{
    //    if (TMBS_StateSaver.Undo(baseTilemap))
    //    {
    //        totalSaveCount--;
#if UNITY_EDITOR
    //        lastAction = "Undo";
#endif
    //    }
    //}

    //private void PerformRedo()
    //{
    //    if (TMBS_StateSaver.Redo(baseTilemap))
    //    {
    //        totalSaveCount++;
#if UNITY_EDITOR
    //        lastAction = "Redo";
#endif
    //    }
    //}

    // Setup and validation
    private bool ValidateReferences()
    {
        if (baseTilemap == null || buildTile == null || clickInput == null) return false;
        if (buildGrid == null) 
        try
        {
            buildGrid = baseTilemap.layoutGrid;
        } catch(NullReferenceException)
        {
            Debug.Log("No Grid reference found. The base Tilemap must be a child of a Grid component.");
        }
        return buildGrid != null;
    }

    private void CacheReferences()
    {
        constructorCamera = Camera.main;
        activeMouse = Mouse.current;
    }

    private void CleanupSystems()
    {
        inputManager?.Cleanup();
        previewSelector?.Cleanup();
        tilePlacementManager?.Cleanup();
        previewTileMap?.Cleanup();
        
        //if (enableStateSaver) TMBS_StateSaver.ClearAllStates();
    }

#if UNITY_EDITOR
    private void OnGUI()
    {
        if (!showDebugOnScreen) return;

        GUILayout.BeginArea(new Rect(150, 100, 300, 240), GUI.skin.box);
        GUILayout.Label("TILEMAP BUILD DEBUG", debugStyle);
        GUILayout.Label($"Mode: {currentMode}", debugStyle);
        GUILayout.Label($"Dragging: {inputManager?.IsDragging()}", debugStyle);
        GUILayout.Label($"Moved: {hasMovedDuringDrag}", debugStyle);
        GUILayout.Label($"Saves: {totalSaveCount}", debugStyle);
        //GUILayout.Label($"States: {(enableStateSaver ? TMBS_StateSaver.CurrentStateCount : 0)}", debugStyle);
        //GUILayout.Label($"Can Undo: {TMBS_StateSaver.CanUndo(baseTilemap)}", debugStyle);
        //GUILayout.Label($"Can Redo: {TMBS_StateSaver.CanRedo(baseTilemap)}", debugStyle);
        GUILayout.Label($"Last Action: {lastAction}", debugStyle);
        GUILayout.Label($"Current Cell: {currentCell}", debugStyle);
        GUILayout.EndArea();
    }
#endif
}