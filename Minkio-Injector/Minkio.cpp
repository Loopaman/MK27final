#include <Windows.h>
#include <thread>
#include <atomic>
#include <memory>
#include <cstdio>

// Forward declarations for modules (to be implemented in separate headers)
void InitializeOverlay();
void ShutdownOverlay();
void InitializeNamedPipeServer();
void ShutdownNamedPipeServer();

// Global state management
std::atomic<bool> g_DLLRunning(false);
std::unique_ptr<std::thread> g_InitThread;

/// <summary>
/// Main initialization function that runs on a separate thread.
/// This ensures the game's main thread is never blocked.
/// </summary>
void InitializationThread()
{
    __try
    {
        // Small delay to ensure injection is complete
        Sleep(100);

        // Initialize the overlay (displays "Minkio v1.0" notification)
        InitializeOverlay();

        // Initialize the named pipe server (for IPC with executor)
        InitializeNamedPipeServer();

        // Signal that initialization is complete
        g_DLLRunning = true;
    }
    __except (EXCEPTION_EXECUTE_HANDLER)
    {
        // Catch absolutely any exception during initialization
        // This prevents crashes even if something goes catastrophically wrong
        g_DLLRunning = false;
        OutputDebugStringW(L"[Minkio] Exception during initialization!\n");
    }
}

/// <summary>
/// DLL entry point - called when the DLL is loaded into the process.
/// This is the ONLY function that must be in the main DLL file.
/// </summary>
BOOL APIENTRY DllMain(
    HMODULE hModule,
    DWORD ul_reason_for_call,
    LPVOID lpReserved)
{
    __try
    {
        switch (ul_reason_for_call)
        {
        case DLL_PROCESS_ATTACH:
        {
            // Prevent the loader lock from being held by thread notifications
            // This improves performance and reduces deadlock risk
            DisableThreadLibraryCalls(hModule);

            // Create a separate thread for all initialization
            // This prevents blocking the game's main thread
            try
            {
                g_InitThread = std::unique_ptr<std::thread>(
                    new std::thread(InitializationThread));
            }
            catch (const std::exception&)
            {
                // If thread creation fails, we still want to return TRUE
                // (partial functionality is better than a crash)
                return TRUE;
            }

            break;
        }

        case DLL_PROCESS_DETACH:
        {
            // Clean shutdown when the DLL is unloaded
            if (g_DLLRunning)
            {
                g_DLLRunning = false;

                // Shutdown all subsystems
                ShutdownNamedPipeServer();
                ShutdownOverlay();

                // Wait for init thread to finish if it's still running
                if (g_InitThread && g_InitThread->joinable())
                {
                    g_InitThread->join();
                }
            }

            break;
        }

        case DLL_THREAD_ATTACH:
        case DLL_THREAD_DETACH:
        {
            // We don't need thread notifications since we disabled them
            // in DLL_PROCESS_ATTACH with DisableThreadLibraryCalls
            break;
        }
        }

        return TRUE;
    }
    __except (EXCEPTION_EXECUTE_HANDLER)
    {
        // Absolute last resort exception handler
        // If something goes wrong in DllMain itself, we still want to return TRUE
        // to avoid crashing the entire host process
        OutputDebugStringW(L"[Minkio] Critical exception in DllMain!\n");
        return TRUE;
    }
}

