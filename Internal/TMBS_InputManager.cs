using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TileMapBuildSystem
{
    /// <summary>
    /// Input manager for TMBS (TileMap Build System).
    /// Handles all input detection and exposes clean events for consumers.
    /// </summary>
    public class TMBS_InputManager
    {
        // Public events
        public event Action<Vector2> OnDragStart;
        public event Action<Vector2> OnDragUpdate;
        public event Action<Vector2> OnDragEnd;
        public event Action<Vector2> OnClickStart;
        public event Action<Vector2> OnClickEnd;
        public event Action<bool> OnShiftStateChanged;
        public event Action<Vector2> OnPositionUpdate;

        // Input references for mouse input
        private readonly InputActionReference dragInput;
        private readonly InputActionReference clickInput;
        private readonly InputActionReference shiftInput;
        private readonly Mouse activeMouse;

        // Internal state
        private bool isDragHeld;
        private bool isShiftHeld;
        
        // Position tracking
        private Vector2 currentScreenPosition;
        private Vector2 previousScreenPosition;

        // Constructor
        /// <summary>
        /// Creates an input manager using Mouse device with Input System bindings.
        /// </summary>
        /// <param name="mouseRef">Mouse device to read input from.</param>
        /// <param name="dragInputRef">Input action for drag operations.</param>
        /// <param name="clickInputRef">Input action for click operations.</param>
        /// <param name="shiftInputRef">Input action for shift modifier.</param>
        public TMBS_InputManager(
            Mouse mouseRef,
            InputActionReference dragInputRef,
            InputActionReference clickInputRef,
            InputActionReference shiftInputRef)
        {
            activeMouse = mouseRef;
            dragInput = dragInputRef;
            clickInput = clickInputRef;
            shiftInput = shiftInputRef;

            currentScreenPosition = Vector2.zero;
            previousScreenPosition = Vector2.zero;

            BindInputActions();
        }

        // Initialization
        private void BindInputActions()
        {
            if (dragInput?.action != null)
            {
                dragInput.action.started += ctx => { isDragHeld = true; UpdateScreenPosition(); previousScreenPosition = currentScreenPosition; OnDragStart?.Invoke(currentScreenPosition); };
                dragInput.action.canceled += ctx => { if (isDragHeld) { isDragHeld = false; UpdateScreenPosition(); OnDragEnd?.Invoke(currentScreenPosition); } };
                dragInput.action.Enable();
            }

            if (clickInput?.action != null)
            {
                clickInput.action.started += ctx => { UpdateScreenPosition(); OnClickStart?.Invoke(currentScreenPosition); };
                clickInput.action.performed += ctx => { UpdateScreenPosition(); OnClickEnd?.Invoke(currentScreenPosition); };
                clickInput.action.Enable();
            }

            if (shiftInput?.action != null)
            {
                shiftInput.action.started += ctx => { isShiftHeld = true; OnShiftStateChanged?.Invoke(true); };
                shiftInput.action.canceled += ctx => { isShiftHeld = false; OnShiftStateChanged?.Invoke(false); };
                shiftInput.action.Enable();
            }
        }

        // Public methods
        /// <summary>
        /// Enables all input actions.
        /// Call this in MonoBehaviour.OnEnable().
        /// </summary>
        public void EnableInputs()
        {
            dragInput?.action?.Enable();
            clickInput?.action?.Enable();
            shiftInput?.action?.Enable();
        }

        /// <summary>
        /// Disables all input actions.
        /// Call this in MonoBehaviour.OnDisable().
        /// </summary>
        public void DisableInputs()
        {
            dragInput?.action?.Disable();
            clickInput?.action?.Disable();
            shiftInput?.action?.Disable();
        }

        /// <summary>
        /// Cleans up input bindings and disables actions.
        /// Call this in MonoBehaviour.OnDestroy().
        /// </summary>
        public void Cleanup()
        {
            DisableInputs();
        }

        /// <summary>
        /// Updates the input state. 
        /// Call this every frame in MonoBehaviour.Update().
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update()
        {
            UpdateScreenPosition();

            if (isDragHeld && currentScreenPosition != previousScreenPosition)
            {
                OnDragUpdate?.Invoke(currentScreenPosition);
                previousScreenPosition = currentScreenPosition;
            }
            
            OnPositionUpdate?.Invoke(currentScreenPosition);
        }

        // Utility
        /// <summary>
        /// Updates the current screen position from the mouse device.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateScreenPosition()
        {
            currentScreenPosition = activeMouse?.position.ReadValue() ?? Vector2.zero;
        }

        // Accessors
        /// <summary>
        /// Gets whether a drag operation is currently active.
        /// </summary>
        public bool IsDragging() => isDragHeld;
        
        /// <summary>
        /// Gets whether the shift modifier is currently held.
        /// </summary>
        public bool IsShiftHeld() => isShiftHeld;
        
        /// <summary>
        /// Gets the current screen position.
        /// </summary>
        public Vector2 GetCurrentScreenPosition() => currentScreenPosition;
        
        /// <summary>
        /// Gets the previous screen position from the last update.
        /// </summary>
        public Vector2 GetPreviousScreenPosition() => previousScreenPosition;
    }
}