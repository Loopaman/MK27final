using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace MinkioAPI
{
    /// <summary>
    /// MinkioInjector - Injects Minkio.dll into RobloxPlayerBeta.exe using Windows API
    /// Uses CreateRemoteThread + LoadLibraryA for direct process memory injection
    /// </summary>
    public static class MinkioInjector
    {
        private const string TARGET_PROCESS = "RobloxPlayerBeta";
        private const string DLL_NAME = "Minkio.dll";

        // ===== Windows API Imports =====

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(
            ProcessAccessFlags dwDesiredAccess,
            bool bInheritHandle,
            int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr VirtualAllocEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            uint dwSize,
            AllocationType flAllocationType,
            MemoryProtection flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualFreeEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            uint dwSize,
            FreeType dwFreeType);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            byte[] lpBuffer,
            uint nSize,
            out UIntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateRemoteThread(
            IntPtr hProcess,
            IntPtr lpThreadAttributes,
            uint dwStackSize,
            IntPtr lpStartAddress,
            IntPtr lpParameter,
            uint dwCreationFlags,
            out uint lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetExitCodeThread(IntPtr hThread, out uint lpExitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        // ===== Public API =====

        /// <summary>
        /// Find the process ID of RobloxPlayerBeta.exe
        /// </summary>
        /// <returns>Process ID, or 0 if not found</returns>
        public static int GetRobloxPID()
        {
            try
            {
                Process[] processes = Process.GetProcessesByName(TARGET_PROCESS);
                if (processes.Length > 0)
                {
                    int pid = processes[0].Id;
                    Debug.WriteLine($"[MinkioInjector] Found Roblox PID: {pid}");
                    return pid;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MinkioInjector] Error finding Roblox: {ex.Message}");
            }

            return 0;
        }

        /// <summary>
        /// Inject Minkio.dll into target process via CreateRemoteThread + LoadLibraryA
        /// </summary>
        /// <param name="processID">Target process ID (RobloxPlayerBeta.exe)</param>
        /// <param name="dllPath">Full path to Minkio.dll</param>
        /// <returns>True if injection succeeded, false otherwise</returns>
        public static bool InjectDLL(int processID, string dllPath)
        {
            if (processID == 0 || string.IsNullOrEmpty(dllPath))
            {
                Debug.WriteLine("[MinkioInjector] Invalid parameters");
                return false;
            }

            IntPtr hProcess = IntPtr.Zero;
            IntPtr hThread = IntPtr.Zero;
            IntPtr allocatedMemory = IntPtr.Zero;

            try
            {
                // Step 1: Open target process with necessary rights
                hProcess = OpenProcess(ProcessAccessFlags.All, false, processID);
                if (hProcess == IntPtr.Zero)
                {
                    Debug.WriteLine($"[MinkioInjector] Failed to open process {processID}. Error: {Marshal.GetLastWin32Error()}");
                    return false;
                }

                Debug.WriteLine($"[MinkioInjector] Opened process {processID}");

                // Step 2: Allocate memory in target process for DLL path
                byte[] dllPathBytes = Encoding.ASCII.GetBytes(dllPath);
                uint pathLength = (uint)dllPathBytes.Length + 1;

                allocatedMemory = VirtualAllocEx(hProcess, IntPtr.Zero, pathLength, 
                    AllocationType.Reserve | AllocationType.Commit, MemoryProtection.ReadWrite);
                if (allocatedMemory == IntPtr.Zero)
                {
                    Debug.WriteLine($"[MinkioInjector] Failed to allocate memory. Error: {Marshal.GetLastWin32Error()}");
                    return false;
                }

                Debug.WriteLine($"[MinkioInjector] Allocated {pathLength} bytes at 0x{allocatedMemory.ToInt64():X}");

                // Step 3: Write DLL path to allocated memory
                if (!WriteProcessMemory(hProcess, allocatedMemory, dllPathBytes, pathLength, out UIntPtr bytesWritten))
                {
                    Debug.WriteLine($"[MinkioInjector] Failed to write DLL path. Error: {Marshal.GetLastWin32Error()}");
                    return false;
                }

                Debug.WriteLine($"[MinkioInjector] Wrote {bytesWritten} bytes to target memory");

                // Step 4: Get address of LoadLibraryA
                IntPtr hKernel32 = GetModuleHandle("kernel32.dll");
                if (hKernel32 == IntPtr.Zero)
                {
                    Debug.WriteLine("[MinkioInjector] Failed to get kernel32.dll handle");
                    return false;
                }

                IntPtr pLoadLibraryA = GetProcAddress(hKernel32, "LoadLibraryA");
                if (pLoadLibraryA == IntPtr.Zero)
                {
                    Debug.WriteLine("[MinkioInjector] Failed to get LoadLibraryA address");
                    return false;
                }

                Debug.WriteLine($"[MinkioInjector] LoadLibraryA at 0x{pLoadLibraryA.ToInt64():X}");

                // Step 5: Create remote thread that calls LoadLibraryA
                uint threadID;
                hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, pLoadLibraryA, allocatedMemory, 0, out threadID);

                if (hThread == IntPtr.Zero)
                {
                    Debug.WriteLine($"[MinkioInjector] Failed to create remote thread. Error: {Marshal.GetLastWin32Error()}");
                    return false;
                }

                Debug.WriteLine($"[MinkioInjector] Created remote thread {threadID}");

                // Step 6: Wait for remote thread to complete
                uint waitResult = WaitForSingleObject(hThread, 10000);  // 10 second timeout
                if (waitResult == 0x102)  // WAIT_TIMEOUT
                {
                    Debug.WriteLine("[MinkioInjector] Remote thread timeout");
                    // DLL may still be loading, continue
                }

                // Step 7: Check thread exit code (should be HMODULE if successful)
                if (GetExitCodeThread(hThread, out uint exitCode))
                {
                    if (exitCode != 0)
                    {
                        Debug.WriteLine($"[MinkioInjector] SUCCESS! Module handle: 0x{exitCode:X}");
                        return true;
                    }
                    else
                    {
                        Debug.WriteLine("[MinkioInjector] Thread returned 0 - injection failed");
                        return false;
                    }
                }

                Debug.WriteLine("[MinkioInjector] Failed to get thread exit code");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MinkioInjector] Exception: {ex.Message}");
                return false;
            }
            finally
            {
                // Cleanup handles
                if (allocatedMemory != IntPtr.Zero)
                    VirtualFreeEx(hProcess, allocatedMemory, 0, FreeType.Release);

                if (hThread != IntPtr.Zero)
                    CloseHandle(hThread);

                if (hProcess != IntPtr.Zero)
                    CloseHandle(hProcess);
            }
        }
    }

    // ===== Windows API Enums =====

    /// <summary>
    /// Process access flags for OpenProcess
    /// </summary>
    [Flags]
    public enum ProcessAccessFlags : uint
    {
        All = 0x001F0FFF,
        Terminate = 0x00000001,
        CreateThread = 0x00000002,
        VirtualMemoryOperation = 0x00000008,
        VirtualMemoryRead = 0x00000010,
        VirtualMemoryWrite = 0x00000020,
        DuplicateHandle = 0x00000040,
        CreateProcess = 0x00000080,
        SetQuota = 0x00000100,
        SetInformation = 0x00000200,
        QueryInformation = 0x00000400,
        QueryLimitedInformation = 0x00001000,
        Synchronize = 0x00100000
    }

    /// <summary>
    /// Memory allocation types for VirtualAllocEx
    /// </summary>
    [Flags]
    public enum AllocationType : uint
    {
        Commit = 0x00001000,
        Reserve = 0x00002000,
        Decommit = 0x00004000,
        Release = 0x00008000,
        Reset = 0x00080000,
        Physical = 0x00400000,
        TopDown = 0x00100000,
        WriteWatch = 0x00200000,
        LargePages = 0x20000000
    }

    /// <summary>
    /// Memory protection flags
    /// </summary>
    [Flags]
    public enum MemoryProtection : uint
    {
        Execute = 0x10,
        ExecuteRead = 0x20,
        ExecuteReadWrite = 0x40,
        ExecuteWriteCopy = 0x80,
        NoAccess = 0x01,
        ReadOnly = 0x02,
        ReadWrite = 0x04,
        WriteCopy = 0x08,
        GuardModifierflag = 0x100,
        NoCacheModifierflag = 0x200,
        WriteCombineModifierflag = 0x400
    }

    /// <summary>
    /// Memory free types for VirtualFreeEx
    /// </summary>
    [Flags]
    public enum FreeType : uint
    {
        Decommit = 0x00004000,
        Release = 0x00008000
    }
}
