#if UNITY_STANDALONE_WIN

using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace MMate.Avatar
{
    /// <summary>
    /// Manages the Unity window as a desktop overlay using Windows GDI Color Key.
    /// IMPORTANT: Disable "Use DXGI Flip Mode Swapchain for D3D11" in Player Settings > Resolution and Presentation,
    /// otherwise GDI color key transparency will not work in standalone builds.
    /// </summary>
    public class DesktopWindowManager : MonoBehaviour
    {
        #region Windows API

        private const uint WS_CAPTION = 0x00C00000;
        private const uint WS_THICKFRAME = 0x00040000;
        private const uint WS_SYSMENU = 0x00080000;
        private const uint WS_MAXIMIZEBOX = 0x00010000;
        private const uint WS_MINIMIZEBOX = 0x00020000;
        private const uint WS_POPUP = 0x80000000;

        private const uint WS_EX_LAYERED = 0x00080000;
        private const uint WS_EX_TRANSPARENT = 0x00000020;

        private const uint LWA_COLORKEY = 0x00000001;

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        private const uint SWP_FRAMECHANGED = 0x0020;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint SWP_NOACTIVATE = 0x0010;

        private const int GWL_STYLE = -16;
        private const int GWL_EXSTYLE = -20;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        #endregion

        #region Serialized Fields

        [Header("Target Camera")]
        [Tooltip("Leave empty to use Camera.main")]
        [SerializeField] private Camera targetCamera;

        [Header("Window Style")]
        [SerializeField] private bool borderless = true;
        [SerializeField] private bool topmost = true;

        [Header("Transparency")]
        [Tooltip("Camera background and window color key. Pixels with this exact RGB become transparent. Choose a color NOT used by the avatar.")]
        [SerializeField] private Color transparentColor = Color.black;

        [Header("Click Through")]
        [SerializeField] private bool clickThrough = false;
        [Tooltip("Hotkey to toggle click-through mode at runtime.")]
        [SerializeField] private KeyCode clickThroughToggleKey = KeyCode.T;

        [Header("Diagnostic")]
        [SerializeField] private bool showDebugLogs = true;

        #endregion

        #region Private Fields

        private IntPtr _hwnd;
        private bool _initialized;
        private Camera _camera;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            ResolveCamera();
            Invoke(nameof(InitializeWindow), 0.5f);
        }

        private void Update()
        {
            if (!_initialized)
                return;

            if (Input.GetKeyDown(clickThroughToggleKey))
                ToggleClickThrough();
        }

        private void LateUpdate()
        {
            if (_camera != null)
            {
                // Force background color every frame to prevent overwrites
                _camera.backgroundColor = transparentColor;
            }
        }

        #endregion

        #region Public Methods

        public void ToggleClickThrough()
        {
            clickThrough = !clickThrough;
            ApplyClickThrough();
        }

        public void SetTopmost(bool enable)
        {
            if (!_initialized) return;
            IntPtr zOrder = enable ? HWND_TOPMOST : HWND_NOTOPMOST;
            SetWindowPos(_hwnd, zOrder, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW | SWP_NOACTIVATE);
        }

        #endregion

        #region Private Methods

        private void Log(string message)
        {
            if (showDebugLogs)
                Debug.Log($"[DesktopWindowManager] {message}");
        }

        private void ResolveCamera()
        {
            _camera = targetCamera != null ? targetCamera : Camera.main;
            if (_camera == null)
            {
                Debug.LogError("[DesktopWindowManager] No target camera found.");
                return;
            }

            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = transparentColor;

            Log($"Camera resolved: {_camera.name}");
            Log($"ClearFlags = {_camera.clearFlags}");
            Log($"BackgroundColor = {_camera.backgroundColor} (R={_camera.backgroundColor.r}, G={_camera.backgroundColor.g}, B={_camera.backgroundColor.b})");
        }

        private void InitializeWindow()
        {
            _hwnd = GetActiveWindow();
            if (_hwnd == IntPtr.Zero)
            {
                Debug.LogError("[DesktopWindowManager] Failed to get window handle.");
                return;
            }

            Log($"HWND = {_hwnd}");

            if (borderless)
                ApplyBorderless();

            if (topmost)
                ApplyTopmost();

            ApplyTransparentBackground();

            if (clickThrough)
                ApplyClickThrough();

            _initialized = true;
            Log("Initialization complete.");
        }

        private void ApplyBorderless()
        {
            uint style = GetWindowLong(_hwnd, GWL_STYLE);
            style &= ~(WS_CAPTION | WS_THICKFRAME | WS_SYSMENU | WS_MAXIMIZEBOX | WS_MINIMIZEBOX);
            style |= WS_POPUP;
            SetWindowLong(_hwnd, GWL_STYLE, style);

            SetWindowPos(_hwnd, IntPtr.Zero, 0, 0, 0, 0,
                SWP_FRAMECHANGED | SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW | SWP_NOACTIVATE);

            Log("Borderless applied.");
        }

        private void ApplyTopmost()
        {
            SetWindowPos(_hwnd, HWND_TOPMOST, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW | SWP_NOACTIVATE);
            Log("Topmost applied.");
        }

        private void ApplyTransparentBackground()
        {
            uint exStyle = GetWindowLong(_hwnd, GWL_EXSTYLE);
            exStyle |= WS_EX_LAYERED;
            SetWindowLong(_hwnd, GWL_EXSTYLE, exStyle);

            uint colorKey = ColorToWin32Color(transparentColor);
            bool success = SetLayeredWindowAttributes(_hwnd, colorKey, 255, LWA_COLORKEY);

            if (!success)
            {
                int error = Marshal.GetLastWin32Error();
                Debug.LogError($"[DesktopWindowManager] SetLayeredWindowAttributes failed, error = {error}");
            }
            else
            {
                Log($"ColorKey applied: 0x{colorKey:X8} (R={transparentColor.r:F4}, G={transparentColor.g:F4}, B={transparentColor.b:F4})");
            }
        }

        private void ApplyClickThrough()
        {
            uint exStyle = GetWindowLong(_hwnd, GWL_EXSTYLE);
            if (clickThrough)
                exStyle |= WS_EX_TRANSPARENT;
            else
                exStyle &= ~WS_EX_TRANSPARENT;
            SetWindowLong(_hwnd, GWL_EXSTYLE, exStyle);
            Log($"ClickThrough = {clickThrough}");
        }

        private uint ColorToWin32Color(Color color)
        {
            byte r = (byte)(color.r * 255f);
            byte g = (byte)(color.g * 255f);
            byte b = (byte)(color.b * 255f);
            return (uint)(r | (g << 8) | (b << 16));
        }

        #endregion
    }
}

#endif
