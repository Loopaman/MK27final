#pragma once

#include <Windows.h>
#include <thread>
#include <atomic>
#include <memory>
#include <cstring>

// ============================================================================
// NAMED PIPE IPC SERVER
// Allows the C# executor to send scripts to the DLL for execution
// ============================================================================

const wchar_t* PIPE_NAME = L"\\\\.\\pipe\\Minkio_Script_Server";
const size_t MAX_SCRIPT_LENGTH = 4096;  // Maximum script size per message
const size_t BUFFER_SIZE = MAX_SCRIPT_LENGTH + 256;

// Global state
std::atomic<bool> g_PipeServerRunning(false);
std::unique_ptr<std::thread> g_PipeServerThread;
HANDLE g_PipeHandle = INVALID_HANDLE_VALUE;

// Script execution callback (to be implemented elsewhere)
typedef void (*ScriptExecutionCallback)(const char* script);
ScriptExecutionCallback g_ScriptCallback = nullptr;

/// <summary>
/// Set the function that will be called when a script is received.
/// </summary>
void SetScriptExecutionCallback(ScriptExecutionCallback callback)
{
    g_ScriptCallback = callback;
}

/// <summary>
/// Named pipe server thread function.
/// Listens for incoming script messages and executes them.
/// </summary>
void PipeServerThreadProc()
{
    __try
    {
        while (g_PipeServerRunning)
        {
            __try
            {
                // Create named pipe for listening
                g_PipeHandle = CreateNamedPipeW(
                    PIPE_NAME,
                    PIPE_ACCESS_INBOUND,                    // One-way pipe (receive only)
                    PIPE_TYPE_MESSAGE | PIPE_READMODE_MESSAGE | PIPE_WAIT,
                    1,                                      // Single instance
                    BUFFER_SIZE,                            // Output buffer size
                    BUFFER_SIZE,                            // Input buffer size
                    0,                                      // Timeout
                    NULL                                    // Security
                );

                if (g_PipeHandle == INVALID_HANDLE_VALUE)
                {
                    OutputDebugStringW(L"[Pipe] Failed to create named pipe\n");
                    Sleep(1000);
                    continue;
                }

                // Wait for a client to connect
                if (!ConnectNamedPipe(g_PipeHandle, NULL))
                {
                    // Client connected before ConnectNamedPipe was called
                    if (GetLastError() != ERROR_PIPE_CONNECTED)
                    {
                        OutputDebugStringW(L"[Pipe] Connect failed\n");
                        CloseHandle(g_PipeHandle);
                        g_PipeHandle = INVALID_HANDLE_VALUE;
                        Sleep(1000);
                        continue;
                    }
                }

                // Connected! Read script data
                char buffer[BUFFER_SIZE] = { 0 };
                DWORD bytesRead = 0;

                if (ReadFile(g_PipeHandle, buffer, BUFFER_SIZE - 1, &bytesRead, NULL))
                {
                    if (bytesRead > 0)
                    {
                        buffer[bytesRead] = '\0';

                        // Execute the script through callback
                        if (g_ScriptCallback)
                        {
                            __try
                            {
                                g_ScriptCallback(buffer);
                            }
                            __except (EXCEPTION_EXECUTE_HANDLER)
                            {
                                OutputDebugStringW(L"[Pipe] Exception in script callback\n");
                            }
                        }

                        // Send acknowledgment back to client
                        const char* ack = "OK";
                        DWORD bytesWritten = 0;
                        WriteFile(g_PipeHandle, ack, strlen(ack), &bytesWritten, NULL);
                    }
                }

                // Close connection and wait for next client
                FlushFileBuffers(g_PipeHandle);
                DisconnectNamedPipe(g_PipeHandle);
                CloseHandle(g_PipeHandle);
                g_PipeHandle = INVALID_HANDLE_VALUE;
            }
            __except (EXCEPTION_EXECUTE_HANDLER)
            {
                OutputDebugStringW(L"[Pipe] Exception in server loop\n");
                if (g_PipeHandle != INVALID_HANDLE_VALUE)
                {
                    CloseHandle(g_PipeHandle);
                    g_PipeHandle = INVALID_HANDLE_VALUE;
                }
                Sleep(1000);
            }
        }
    }
    __except (EXCEPTION_EXECUTE_HANDLER)
    {
        OutputDebugStringW(L"[Pipe] Critical exception in server thread\n");
        g_PipeServerRunning = false;
    }
}

/// <summary>
/// Initialize the named pipe server.
/// Called during DLL initialization.
/// </summary>
void InitializeNamedPipeServer()
{
    if (g_PipeServerRunning)
        return;  // Already initialized

    __try
    {
        g_PipeServerRunning = true;

        // Create the server thread
        g_PipeServerThread = std::unique_ptr<std::thread>(
            new std::thread(PipeServerThreadProc)
        );

        OutputDebugStringW(L"[Pipe] Named pipe server initialized\n");
    }
    __except (EXCEPTION_EXECUTE_HANDLER)
    {
        g_PipeServerRunning = false;
        OutputDebugStringW(L"[Pipe] Exception during initialization\n");
    }
}

/// <summary>
/// Shutdown the named pipe server.
/// Called when DLL is unloaded.
/// </summary>
void ShutdownNamedPipeServer()
{
    if (!g_PipeServerRunning)
        return;

    __try
    {
        g_PipeServerRunning = false;

        // Close the pipe handle if it's open
        if (g_PipeHandle != INVALID_HANDLE_VALUE)
        {
            FlushFileBuffers(g_PipeHandle);
            DisconnectNamedPipe(g_PipeHandle);
            CloseHandle(g_PipeHandle);
            g_PipeHandle = INVALID_HANDLE_VALUE;
        }

        // Wait for server thread to finish
        if (g_PipeServerThread && g_PipeServerThread->joinable())
        {
            g_PipeServerThread->join();
        }

        OutputDebugStringW(L"[Pipe] Named pipe server shut down\n");
    }
    __except (EXCEPTION_EXECUTE_HANDLER)
    {
        OutputDebugStringW(L"[Pipe] Exception during shutdown\n");
    }
}

// ============================================================================
// CLIENT-SIDE FUNCTIONS (for C# use)
// ============================================================================

/// <summary>
/// C# has its own implementation, but here's the concept:
/// 
/// // C# Client Code (in executor)
/// public class PipeClient
/// {
///     public static bool SendScript(string script)
///     {
///         try
///         {
///             using (var pipeClient = new NamedPipeClientStream(".", "Minkio_Script_Server"))
///             {
///                 pipeClient.Connect(1000);
///                 
///                 byte[] buffer = Encoding.UTF8.GetBytes(script);
///                 pipeClient.Write(buffer, 0, buffer.Length);
///                 pipeClient.Flush();
///                 
///                 // Read ACK
///                 byte[] ackBuffer = new byte[10];
///                 int bytesRead = pipeClient.Read(ackBuffer, 0, ackBuffer.Length);
///                 
///                 return bytesRead > 0;
///             }
///         }
///         catch
///         {
///             return false;
///         }
///     }
/// }
/// </summary>

#endif // NAMED_PIPE_SERVER_HPP
