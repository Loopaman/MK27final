using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace MinkioAPI
{
    /// <summary>
    /// MinkioOverlay - Overlay injection for Roblox
    /// Injects the Minkio.dll which displays "Minkio v1.0" overlay
    /// </summary>
    public class MinkioOverlay
    {
        // Import necessary Windows APIs
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(
            ProcessAccessFlags dwDesiredAccess,
            bool bInheritHandle,
            int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetProcAddress(
            IntPtr hModule,
            string lpProcName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr VirtualAllocEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            uint dwSize,
            uint flAllocationType,
            uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualFreeEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            uint dwSize,
            uint dwFreeType);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            byte[] lpBuffer,
            uint nSize,
            out int lpNumberOfBytesWritten);

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
        private static extern uint WaitForSingleObject(
            IntPtr hHandle,
            uint dwMilliseconds);

        // Constants
        private const uint MEM_COMMIT = 0x1000;
        private const uint MEM_RELEASE = 0x8000;
        private const uint PAGE_READWRITE = 0x04;
        private const uint PROCESS_CREATE_THREAD = 0x0002;
        private const uint PROCESS_QUERY_INFORMATION = 0x0400;
        private const uint PROCESS_VM_OPERATION = 0x0008;
        private const uint PROCESS_VM_READ = 0x0010;
        private const uint PROCESS_VM_WRITE = 0x0020;
        private const uint INFINITE = 0xFFFFFFFF;

        [Flags]
        private enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VMOperation = 0x00000008,
            VMRead = 0x00000010,
            VMWrite = 0x00000020,
            DupHandle = 0x00000040,
            CreateProcess = 0x00000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            SuspendResume = 0x00000800,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000,
        }

        public const string OVERLAY_DLL_NAME = "Minkio.dll";
        public const string TARGET_PROCESS = "RobloxPlayerBeta";

        /// <summary>
        /// Injects the Minkio.dll overlay into the Roblox process
        /// </summary>
        /// <returns>True if injection was successful</returns>
        public static bool InjectOverlay()
        {
            try
            {
                // Get Roblox process
                Process[] processes = Process.GetProcessesByName(TARGET_PROCESS);
                if (processes.Length == 0)
                {
                    Console.WriteLine("Error: {0}.exe is not running", TARGET_PROCESS);
                    return false;
                }

                Process robloxProcess = processes[0];
                int processId = robloxProcess.Id;
                Console.WriteLine("Found Roblox process (PID: {0})", processId);

                // Get the full path to the DLL
                string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, OVERLAY_DLL_NAME);

                if (!File.Exists(dllPath))
                {
                    Console.WriteLine("Error: {0} not found at {1}", OVERLAY_DLL_NAME, dllPath);
                    return false;
                }

                Console.WriteLine("DLL path: {0}", dllPath);

                // Get full absolute path
                dllPath = Path.GetFullPath(dllPath);
                byte[] dllPathBytes = System.Text.Encoding.ASCII.GetBytes(dllPath);

                // Open the target process with necessary permissions
                IntPtr hProcess = OpenProcess(
                    (ProcessAccessFlags)(
                        PROCESS_CREATE_THREAD |
                        PROCESS_QUERY_INFORMATION |
                        PROCESS_VM_OPERATION |
                        PROCESS_VM_READ |
                        PROCESS_VM_WRITE
                    ),
                    false,
                    processId);

                if (hProcess == IntPtr.Zero)
                {
                    Console.WriteLine("Error: Failed to open Roblox process");
                    return false;
                }

                Console.WriteLine("Successfully opened Roblox process");

                // Allocate memory in the target process for the DLL path
                IntPtr allocatedMemory = VirtualAllocEx(
                    hProcess,
                    IntPtr.Zero,
                    (uint)dllPathBytes.Length,
                    MEM_COMMIT,
                    PAGE_READWRITE);

                if (allocatedMemory == IntPtr.Zero)
                {
                    Console.WriteLine("Error: Failed to allocate memory in Roblox process");
                    CloseHandle(hProcess);
                    return false;
                }

                Console.WriteLine("Allocated memory at: 0x{0:X}", allocatedMemory);

                // Write the DLL path to the allocated memory
                int bytesWritten;
                if (!WriteProcessMemory(
                    hProcess,
                    allocatedMemory,
                    dllPathBytes,
                    (uint)dllPathBytes.Length,
                    out bytesWritten))
                {
                    Console.WriteLine("Error: Failed to write DLL path to process memory");
                    VirtualFreeEx(hProcess, allocatedMemory, 0, MEM_RELEASE);
                    CloseHandle(hProcess);
                    return false;
                }

                Console.WriteLine("Wrote {0} bytes to process memory", bytesWritten);

                // Get the address of LoadLibraryA function
                IntPtr hKernel32 = GetModuleHandle("kernel32.dll");
                IntPtr loadLibraryA = GetProcAddress(hKernel32, "LoadLibraryA");

                if (loadLibraryA == IntPtr.Zero)
                {
                    Console.WriteLine("Error: Failed to get LoadLibraryA address");
                    VirtualFreeEx(hProcess, allocatedMemory, 0, MEM_RELEASE);
                    CloseHandle(hProcess);
                    return false;
                }

                Console.WriteLine("LoadLibraryA address: 0x{0:X}", loadLibraryA);

                // Create a remote thread that calls LoadLibraryA with the DLL path
                uint threadId;
                IntPtr hRemoteThread = CreateRemoteThread(
                    hProcess,
                    IntPtr.Zero,
                    0,
                    loadLibraryA,
                    allocatedMemory,
                    0,
                    out threadId);

                if (hRemoteThread == IntPtr.Zero)
                {
                    Console.WriteLine("Error: Failed to create remote thread");
                    VirtualFreeEx(hProcess, allocatedMemory, 0, MEM_RELEASE);
                    CloseHandle(hProcess);
                    return false;
                }

                Console.WriteLine("Created remote thread (TID: {0})", threadId);

                // Wait for the thread to complete
                uint waitResult = WaitForSingleObject(hRemoteThread, INFINITE);

                if (waitResult == 0)
                {
                    Console.WriteLine("Remote thread completed successfully");
                    Console.WriteLine("Overlay injection successful!");
                }
                else
                {
                    Console.WriteLine("Warning: Remote thread wait returned: {0}", waitResult);
                }

                // Cleanup
                CloseHandle(hRemoteThread);
                VirtualFreeEx(hProcess, allocatedMemory, 0, MEM_RELEASE);
                CloseHandle(hProcess);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error during injection: {0}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Checks if the overlay DLL exists
        /// </summary>
        public static bool DllExists()
        {
            string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, OVERLAY_DLL_NAME);
            return File.Exists(dllPath);
        }

        /// <summary>
        /// Gets information about the overlay DLL
        /// </summary>
        public static string GetDllInfo()
        {
            string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, OVERLAY_DLL_NAME);

            if (!File.Exists(dllPath))
                return "DLL not found";

            FileInfo info = new FileInfo(dllPath);
            return string.Format(
                "DLL: {0}\r\nSize: {1} bytes\r\nCreated: {2}\r\nModified: {3}",
                info.Name,
                info.Length,
                info.CreationTime,
                info.LastWriteTime);
        }
    }
}
