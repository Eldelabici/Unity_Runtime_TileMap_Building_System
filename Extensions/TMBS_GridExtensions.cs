using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TMBS_GridExtensions
{
    /// <summary>
    /// Provides extension methods for Unity <see cref="Grid"/> components,
    /// allowing conversion from screen or input positions to grid cell coordinates.
    /// 
    /// These utilities are designed to support both axis-aligned and rotated cameras,
    /// automatically selecting the most appropriate projection method.
    /// </summary>
    public static class TMBS_GridExtensions
    {
        /// <summary>
        /// Converts the current mouse screen position into a grid cell coordinate.
        /// </summary>
        /// <param name="grid">Target grid used for cell conversion.</param>
        /// <param name="camera">Camera responsible for rendering the grid.</param>
        /// <param name="mouse">Mouse input device providing the screen position.</param>
        /// <returns>
        /// The grid cell position corresponding to the mouse location,
        /// or <see cref="Vector2Int.zero"/> if any required reference is null.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Int MouseToCellPosition(this Grid grid, Camera camera, Mouse mouse)
        {
            if (grid == null || camera == null || mouse == null) return Vector2Int.zero;

            return ScreenToCellPosition(grid, camera, mouse.position.ReadValue());
        }

        /// <summary>
        /// Converts a UI or screen-space position into a grid cell coordinate.
        /// </summary>
        /// <param name="grid">Target grid used for cell conversion.</param>
        /// <param name="camera">Camera responsible for rendering the grid.</param>
        /// <param name="screenPosition">Screen-space position in pixels.</param>
        /// <returns>
        /// The grid cell position corresponding to the screen position,
        /// or <see cref="Vector2Int.zero"/> if the grid or camera is null.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Int UIPositionToCell(this Grid grid, Camera camera, Vector2 screenPosition)
        {
            if (grid == null || camera == null)
                return Vector2Int.zero;

            return ScreenToCellPosition(grid, camera, screenPosition);
        }

        /// <summary>
        /// Converts a screen position into a grid cell coordinate using a raycast
        /// against a plane in world space.
        /// 
        /// This method is suitable for rotated, isometric, or perspective cameras.
        /// </summary>
        /// <param name="grid">Target grid used for cell conversion.</param>
        /// <param name="camera">Camera used to generate the screen ray.</param>
        /// <param name="screenPosition">Screen-space position in pixels.</param>
        /// <param name="planeNormal">
        /// Optional normal of the intersection plane. Defaults to <see cref="Vector3.forward"/>.
        /// </param>
        /// <param name="planeDistance">
        /// Distance of the plane from the origin along its normal.
        /// </param>
        /// <returns>
        /// The grid cell intersected by the ray,
        /// or <see cref="Vector2Int.zero"/> if the ray does not hit the plane
        /// or required references are null.
        /// </returns>
        public static Vector2Int ScreenToCellRaycast(
            this Grid grid,
            Camera camera,
            Vector2 screenPosition,
            Vector3? planeNormal = null,
            float planeDistance = 0f)
        {
            if (grid == null || camera == null)
                return Vector2Int.zero;

            Vector3 normal = planeNormal ?? Vector3.forward;
            Plane plane = new Plane(normal, -planeDistance);

            Ray ray = camera.ScreenPointToRay(screenPosition);

            if (!plane.Raycast(ray, out float dist))
                return Vector2Int.zero;

            Vector3 worldPos = ray.GetPoint(dist);
            Vector3Int cell = grid.WorldToCell(worldPos);
            return new Vector2Int(cell.x, cell.y);
        }

        /// <summary>
        /// Generates a rectangular selection of grid cell coordinates
        /// defined by two opposing corner cells.
        /// </summary>
        /// <param name="grid">Target grid.</param>
        /// <param name="a">First corner cell.</param>
        /// <param name="b">Opposite corner cell.</param>
        /// <returns>
        /// A two-dimensional array containing all grid cells within the defined box.
        /// Returns an empty array if the grid reference is null.
        /// </returns>
        public static Vector2Int[,] SelectedBoxCells(this Grid grid, Vector2Int a, Vector2Int b)
        {
            if (grid == null)
                return new Vector2Int[0, 0];

            int minX = Mathf.Min(a.x, b.x);
            int maxX = Mathf.Max(a.x, b.x);
            int minY = Mathf.Min(a.y, b.y);
            int maxY = Mathf.Max(a.y, b.y);

            int w = maxX - minX + 1;
            int h = maxY - minY + 1;

            Vector2Int[,] result = new Vector2Int[w, h];

            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    result[x, y] = new Vector2Int(minX + x, minY + y);

            return result;
        }

        /// <summary>
        /// Determines the appropriate method to convert a screen position
        /// into a grid cell, based on the camera's rotation state.
        /// </summary>
        /// <param name="grid">Target grid.</param>
        /// <param name="camera">Camera used for projection.</param>
        /// <param name="screenPosition">Screen-space position.</param>
        /// <returns>The corresponding grid cell coordinate.</returns>
        private static Vector2Int ScreenToCellPosition(Grid grid, Camera camera, Vector2 screenPosition)
        {
            return IsCameraRotated(camera)
                ? grid.ScreenToCellRaycast(camera, screenPosition)
                : ScreenToCellDirect(grid, camera, screenPosition);
        }

        /// <summary>
        /// Converts a screen position directly into a grid cell coordinate
        /// using <see cref="Camera.ScreenToWorldPoint"/>.
        /// 
        /// This method assumes the camera is axis-aligned and not rotated.
        /// </summary>
        /// <param name="grid">Target grid.</param>
        /// <param name="camera">Camera used for projection.</param>
        /// <param name="screenPosition">Screen-space position.</param>
        /// <returns>The corresponding grid cell coordinate.</returns>
        private static Vector2Int ScreenToCellDirect(Grid grid, Camera camera, Vector2 screenPosition)
        {
            float depth = camera.orthographic
                ? camera.nearClipPlane
                : Mathf.Abs(camera.transform.position.z);

            Vector3 world = camera.ScreenToWorldPoint(
                new Vector3(screenPosition.x, screenPosition.y, depth)
            );

            Vector3Int cell = grid.WorldToCell(world);
            return new Vector2Int(cell.x, cell.y);
        }

        /// <summary>
        /// Determines whether the camera is rotated away from the world axes
        /// beyond a small tolerance.
        /// </summary>
        /// <param name="camera">Camera to evaluate.</param>
        /// <returns>
        /// <c>true</c> if the camera has any significant rotation;
        /// otherwise, <c>false</c>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsCameraRotated(Camera camera)
        {
            const float tol = 0.01f;
            Vector3 e = camera.transform.eulerAngles;

            return Mathf.Abs(Norm(e.x)) > tol ||
                   Mathf.Abs(Norm(e.y)) > tol ||
                   Mathf.Abs(Norm(e.z)) > tol;
        }

        /// <summary>
        /// Normalizes an angle value to the range [-180, 180] degrees.
        /// </summary>
        /// <param name="a">Angle in degrees.</param>
        /// <returns>The normalized angle.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Norm(float a)
        {
            while (a > 180f) a -= 360f;
            while (a < -180f) a += 360f;
            return a;
        }
    }
}
