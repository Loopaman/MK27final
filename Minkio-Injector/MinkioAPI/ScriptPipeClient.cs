using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;

namespace MinkioAPI
{
    /// <summary>
    /// ScriptPipeClient - Sends Lua scripts to the injected DLL via named pipe
    /// </summary>
    public class ScriptPipeClient
    {
        private const string PIPE_NAME = "Minkio_Script_Server";
        private const int CONNECTION_TIMEOUT = 2000;  // 2 seconds
        private const int WRITE_TIMEOUT = 5000;       // 5 seconds

        /// <summary>
        /// Send a Lua script to the injected Minkio.dll
        /// </summary>
        /// <param name="script">The Lua script to execute</param>
        /// <returns>True if script was sent successfully</returns>
        public static bool SendScript(string script)
        {
            return SendScript(script, WRITE_TIMEOUT);
        }

        /// <summary>
        /// Send a Lua script with custom timeout
        /// </summary>
        public static bool SendScript(string script, int timeoutMs)
        {
            if (string.IsNullOrEmpty(script))
            {
                Console.WriteLine("[ScriptPipe] Error: Script is empty");
                return false;
            }

            if (script.Length > 4096)
            {
                Console.WriteLine("[ScriptPipe] Error: Script too large (max 4096 bytes)");
                return false;
            }

            NamedPipeClientStream pipeClient = null;

            try
            {
                Console.WriteLine("[ScriptPipe] Connecting to pipe: " + PIPE_NAME);

                // Create pipe client
                pipeClient = new NamedPipeClientStream(
                    ".",                    // Local machine
                    PIPE_NAME,
                    PipeDirection.InOut,    // Read and write
                    PipeOptions.None
                );

                // Connect with timeout
                pipeClient.Connect(CONNECTION_TIMEOUT);

                Console.WriteLine("[ScriptPipe] Connected to pipe server");

                // Write the script to the pipe
                byte[] scriptBytes = Encoding.UTF8.GetBytes(script);
                pipeClient.Write(scriptBytes, 0, scriptBytes.Length);
                pipeClient.Flush();

                Console.WriteLine("[ScriptPipe] Script sent (" + scriptBytes.Length + " bytes)");

                // Read response (acknowledgment)
                byte[] buffer = new byte[256];
                int bytesRead = pipeClient.Read(buffer, 0, buffer.Length);

                if (bytesRead > 0)
                {
                    string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine("[ScriptPipe] Response: " + response);
                    return true;
                }

                return false;
            }
            catch (TimeoutException)
            {
                Console.WriteLine("[ScriptPipe] Error: Connection timeout");
                return false;
            }
            catch (IOException ex)
            {
                Console.WriteLine("[ScriptPipe] Error: IO exception - " + ex.Message);
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine("[ScriptPipe] Error: Access denied - " + ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ScriptPipe] Error: " + ex.Message);
                return false;
            }
            finally
            {
                // Always close the pipe
                if (pipeClient != null)
                {
                    try
                    {
                        pipeClient.Close();
                        pipeClient.Dispose();
                    }
                    catch
                    {
                    }
                }
            }
        }

        /// <summary>
        /// Test if the pipe server is available
        /// </summary>
        public static bool IsPipeAvailable()
        {
            try
            {
                using (NamedPipeClientStream testPipe = new NamedPipeClientStream(".", PIPE_NAME))
                {
                    testPipe.Connect(500);  // Quick timeout for test
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Example usage - can be called from executor
        /// </summary>
        public static bool ExecuteScriptExample()
        {
            try
            {
                // Simple test script
                string script = "print('Hello from Minkio!')";

                Console.WriteLine("[ScriptPipe] Sending test script...");
                bool success = SendScript(script);

                if (success)
                {
                    Console.WriteLine("[ScriptPipe] Script execution successful");
                    return true;
                }
                else
                {
                    Console.WriteLine("[ScriptPipe] Script execution failed");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ScriptPipe] Exception: " + ex.Message);
                return false;
            }
        }
    }
}
