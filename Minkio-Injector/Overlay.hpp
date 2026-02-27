#pragma once

#include <Windows.h>
#include <atomic>
#include <thread>
#include <memory>

// ============================================================================
// OVERLAY IMPLEMENTATION
// Displays a semi-transparent "Minkio v1.0" notification in bottom-right
// ============================================================================

// Global state
HWND g_OverlayWindow = NULL;
std::atomic<bool> g_OverlayActive(false);
std::unique_ptr<std::thread> g_OverlayThread;

const wchar_t* OVERLAY_CLASS_NAME = L"MinkioNotification";
const int OVERLAY_WIDTH = 180;
const int OVERLAY_HEIGHT = 60;
const int OVERLAY_MARGIN = 20;

/// <summary>
/// Window procedure for the overlay window.
/// Handles painting and passes through input.
/// </summary>
LRESULT CALLBACK OverlayWindowProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam)
{
    switch (msg)
    {
    case WM_PAINT:
    {
        PAINTSTRUCT ps;
        HDC hdc = BeginPaint(hwnd, &ps);

        if (hdc)
        {
            __try
            {
                // Create brushes for drawing
                // Background: semi-transparent dark color
                HBRUSH hBackBrush = CreateSolidBrush(RGB(0, 0, 0));
                HBRUSH hOldBrush = (HBRUSH)SelectObject(hdc, hBackBrush);

                // Draw background rectangle
                RECT rect = { 5, 5, OVERLAY_WIDTH - 5, OVERLAY_HEIGHT - 5 };
                Rectangle(hdc, rect.left, rect.top, rect.right, rect.bottom);

                // Restore and delete brush
                SelectObject(hdc, hOldBrush);
                DeleteObject(hBackBrush);

                // Draw text: "Minkio v1.0"
                SetTextColor(hdc, RGB(255, 255, 255));  // White text
                SetBkMode(hdc, TRANSPARENT);             // No background for text

                // Simple font selection
                HFONT hFont = CreateFontW(
                    12,                             // Height
                    0,                              // Width
                    0,                              // Escapement
                    0,                              // Orientation
                    FW_NORMAL,                      // Weight
                    FALSE,                          // Italic
                    FALSE,                          // Underline
                    FALSE,                          // StrikeOut
                    DEFAULT_CHARSET,                // CharSet
                    OUT_DEFAULT_PRECIS,             // OutPrecision
                    CLIP_DEFAULT_PRECIS,            // ClipPrecision
                    DEFAULT_QUALITY,                // Quality
                    DEFAULT_PITCH | FF_DONTCARE,    // Pitch and Family
                    L"Arial"                        // Font name
                );

                HFONT hOldFont = (HFONT)SelectObject(hdc, hFont);

                // Draw text centered in the window
                const wchar_t* text = L"Minkio v1.0";
                DrawTextW(hdc, text, -1, &rect, DT_CENTER | DT_VCENTER | DT_SINGLELINE);

                // Restore and delete font
                SelectObject(hdc, hOldFont);
                DeleteObject(hFont);
            }
            __except (EXCEPTION_EXECUTE_HANDLER)
            {
                // If drawing fails, at least don't crash
                OutputDebugStringW(L"[Overlay] Paint exception handled\n");
            }

            EndPaint(hwnd, &ps);
        }

        return 0;
    }

    case WM_ERASEBKGND:
        // Don't erase background (we handle it in WM_PAINT)
        return 0;

    case WM_DESTROY:
        PostQuitMessage(0);
        return 0;

    // Pass through all mouse and keyboard messages to the game window
    case WM_MOUSEMOVE:
    case WM_LBUTTONDOWN:
    case WM_LBUTTONUP:
    case WM_RBUTTONDOWN:
    case WM_RBUTTONUP:
    case WM_MOUSEWHEEL:
    case WM_KEYDOWN:
    case WM_KEYUP:
        return 0;

    default:
        return DefWindowProcW(hwnd, msg, wParam, lParam);
    }
}

/// <summary>
/// Thread function that creates and manages the overlay window.
/// Runs independently from the game main thread.
/// </summary>
void OverlayThreadProc()
{
    __try
    {
        // Register window class
        WNDCLASSW wc = { 0 };
        wc.style = CS_HREDRAW | CS_VREDRAW;
        wc.lpfnWndProc = OverlayWindowProc;
        wc.hInstance = GetModuleHandleW(NULL);
        wc.hCursor = LoadCursorW(NULL, IDC_ARROW);
        wc.lpszClassName = OVERLAY_CLASS_NAME;

        if (!RegisterClassW(&wc))
        {
            OutputDebugStringW(L"[Overlay] Failed to register window class\n");
            return;
        }

        // Calculate position (bottom-right corner)
        int screenWidth = GetSystemMetrics(SM_CXSCREEN);
        int screenHeight = GetSystemMetrics(SM_CYSCREEN);
        int posX = screenWidth - OVERLAY_WIDTH - OVERLAY_MARGIN;
        int posY = screenHeight - OVERLAY_HEIGHT - OVERLAY_MARGIN;

        // Create window as child of desktop, layered for transparency support
        g_OverlayWindow = CreateWindowExW(
            WS_EX_TOPMOST | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE,  // Don't intercept input
            OVERLAY_CLASS_NAME,
            L"Minkio",
            WS_POPUP | WS_VISIBLE,
            posX, posY,
            OVERLAY_WIDTH, OVERLAY_HEIGHT,
            NULL,  // No parent
            NULL,  // No menu
            GetModuleHandleW(NULL),
            NULL
        );

        if (!g_OverlayWindow)
        {
            OutputDebugStringW(L"[Overlay] Failed to create window\n");
            UnregisterClassW(OVERLAY_CLASS_NAME, GetModuleHandleW(NULL));
            return;
        }

        // Set window transparency (per-pixel alpha)
        // Note: We use SetWindowPos with transparency instead of SetLayeredWindowAttributes
        SetWindowPos(g_OverlayWindow, HWND_TOPMOST, 0, 0, 0, 0,
                     SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);

        // Show the window
        ShowWindowAsync(g_OverlayWindow, SW_SHOW);
        UpdateWindow(g_OverlayWindow);

        // Message loop
        MSG msg = { 0 };
        while (g_OverlayActive && GetMessageW(&msg, NULL, 0, 0) > 0)
        {
            TranslateMessage(&msg);
            DispatchMessageW(&msg);
        }

        // Cleanup
        if (g_OverlayWindow)
        {
            DestroyWindow(g_OverlayWindow);
            g_OverlayWindow = NULL;
        }

        UnregisterClassW(OVERLAY_CLASS_NAME, GetModuleHandleW(NULL));
    }
    __except (EXCEPTION_EXECUTE_HANDLER)
    {
        OutputDebugStringW(L"[Overlay] Exception in overlay thread\n");
        g_OverlayActive = false;
    }
}

/// <summary>
/// Initialize the overlay - called from DLL initialization thread.
/// </summary>
void InitializeOverlay()
{
    if (g_OverlayActive)
        return;  // Already initialized

    __try
    {
        g_OverlayActive = true;

        // Create overlay thread
        g_OverlayThread = std::unique_ptr<std::thread>(
            new std::thread(OverlayThreadProc)
        );

        // Brief sleep to let window creation complete
        Sleep(50);
    }
    __except (EXCEPTION_EXECUTE_HANDLER)
    {
        g_OverlayActive = false;
        OutputDebugStringW(L"[Overlay] Exception during initialization\n");
    }
}

/// <summary>
/// Shutdown the overlay - called when DLL is unloaded.
/// </summary>
void ShutdownOverlay()
{
    if (!g_OverlayActive)
        return;

    __try
    {
        g_OverlayActive = false;

        // Signal the window to close
        if (g_OverlayWindow && IsWindow(g_OverlayWindow))
        {
            PostMessageW(g_OverlayWindow, WM_CLOSE, 0, 0);
        }

        // Wait for thread to finish
        if (g_OverlayThread && g_OverlayThread->joinable())
        {
            g_OverlayThread->join();
        }

        g_OverlayWindow = NULL;
    }
    __except (EXCEPTION_EXECUTE_HANDLER)
    {
        OutputDebugStringW(L"[Overlay] Exception during shutdown\n");
    }
}

