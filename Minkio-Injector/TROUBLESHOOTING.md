# Minkio - Comprehensive Troubleshooting Guide

## Quick Diagnostic Flowchart

```
Problem: Something doesn't work
    ↓
Is Roblox running?
    ├─ NO  → Start Roblox first
    └─ YES ↓
        ↓
Is Minkio.dll found?
    ├─ NO  → Check file location (see "File Not Found")
    └─ YES ↓
        ↓
Can injector launch?
    ├─ NO  → Check MinkioInjector.exe path
    └─ YES ↓
        ↓
Does DLL inject?
    ├─ NO  → See "Injection Fails"
    └─ YES ↓
        ↓
Does Roblox crash?
    ├─ YES → See "Game Crashes After Injection"
    └─ NO  ↓
        ↓
Does overlay appear?
    ├─ NO  → See "Overlay Doesn't Appear"
    └─ YES ↓
        ↓
Can you execute scripts?
    ├─ NO  → See "Script Execution Fails"
    └─ YES ✅ Success!
```

---

## Problem: Build Fails

### Symptom
Running `build_dll.bat` fails with error about Visual Studio not found

### Root Causes & Solutions

#### Solution 1: VS2022 Not Installed

```
Check:
1. Go to C:\Program Files\Microsoft Visual Studio\2022\
2. Look for Community, Professional, or Enterprise folder
```

**If missing:**
```
Install Visual Studio 2022:
1. Download from: https://visualstudio.microsoft.com/vs/community/
2. Run installer
3. Select "Desktop Development with C++"
4. Ensure "C++ core features" is checked
5. Install Windows SDK 10.0 or later
6. Click Install (~5-10 GB download)
7. Restart computer
```

#### Solution 2: vswhere Not Found

`vswhere` is installed with Visual Studio. If missing:

```batch
REM Manual path in build_dll.bat
set "VS_PATH=C:\Program Files\Microsoft Visual Studio\2022\Community"
set "DEVENV=%VS_PATH%\Common7\IDE\devenv.exe"
set "MSBUILD=%VS_PATH%\MSBuild\Current\Bin\MSBuild.exe"
```

#### Solution 3: Firewall/Permission Issues

```batch
REM Run Command Prompt as Administrator
REM Right-click cmd.exe → Run as administrator
REM Then run build_dll.bat again
```

### Verification

After VS2022 install, verify:

```batch
where devenv.exe          REM Should find devenv.exe
dir "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\devenv.exe"
```

---

## Problem: DLL Doesn't Inject

### Symptom
Clicking "Attach" in the executor doesn't inject the DLL

### Causes & Solutions

#### Check 1: Is Minkio.dll In The Right Place?

```batch
REM DLL must be in executor directory:
dir "C:\Users\minks\OneDrive\Desktop\Minkio-Injector\MinkioExecutor\bin\Release\Minkio.dll"

REM If not found:
copy "C:\Users\minks\OneDrive\Desktop\Minkio-Injector\x64\Release\Minkio.dll" ^
     "C:\Users\minks\OneDrive\Desktop\Minkio-Injector\MinkioExecutor\bin\Release\"
```

#### Check 2: Is the DLL Actually 64-bit?

```batch
REM Check DLL architecture:
wmic datafile where name="C:\...\Minkio.dll" get Description

REM Should show: x64 or "64-bit"
```

If it's 32-bit:
```batch
REM Rebuild in x64 mode:
msbuild Minkio.vcxproj /p:Configuration=Release /p:Platform=x64
```

#### Check 3: Is MinkioInjector.exe Present?

```batch
REM If your injector is external:
dir "C:\Users\minks\OneDrive\Desktop\Minkio-Injector\MinkioExecutor\bin\Release\MinkioInjector.exe"

REM If missing, place it there:
copy <path_to_injector>\MinkioInjector.exe bin\Release\
```

#### Check 4: Does the Injector Have Permissions?

```batch
REM Run executor as Administrator:
1. Right-click MinkioExecutor.exe
2. Select "Run as administrator"
3. Try injecting again
```

#### Check 5: Check Windows Event Log

```batch
REM Open Event Viewer:
eventvwr.msc

REM Look in:
Windows Logs → Application
Look for entries from "Application Error" source
Note the exception codes
```

#### Check 6: Enable Debug Output

```cpp
// In Minkio.cpp, add to DllMain:
AllocConsole();
freopen("CONOUT$", "w", stdout);
printf("[Minkio] DLL loaded!\n");

// Rebuild (build_dll.bat)
// Run attached to debugger
// Should see console window with output
```

---

## Problem: Game Crashes After Injection

### Symptom
Roblox works fine, but crashes immediately after injection

### Most Common Cause: Overlay Initialization

The overlay uses Windows API calls that might fail.

#### Solution: Disable Overlay Temporarily

```cpp
// In Minkio.cpp DllMain, comment out:
// InitializeOverlay();           // ← Comment this
  InitializeNamedPipeServer();

// Rebuild and test
```

If this fixes the crash, the problem is in `Overlay.hpp`

#### Solution: Fix Overlay Code

Common issues in overlay:

```cpp
// Bad: RegisterClass might fail silently
RegisterClassW(&wc);

// Good: Check return value
if (!RegisterClassW(&wc)) {
    OutputDebugStringW(L"RegisterClass failed\n");
    return;  // Exit thread gracefully
}
```

**Check these in Overlay.hpp:**
1. `RegisterClassW` return value
2. `CreateWindowExW` return value
3. Memory/resource leaks
4. Exception handling around all Win32 calls

#### Solution: Reduce Overlay Complexity

Simplify overlay:

```cpp
// Instead of full window drawing:
HWND hwnd = CreateWindowExW(...);

// Just create window, no painting:
SetWindowPos(hwnd, HWND_TOPMOST, ...);
ShowWindow(hwnd, SW_SHOW);
// Done - let Windows paint default

// Skip complicated GDI+ rendering
```

###Other Potential Causes

#### DLL Contains Uninitialized Code

```cpp
// Bad: Global variable initialization
static std::thread* ptr = new std::thread(...);  // WRONG - runs at DLL load!

// Good: Initialize in DllMain
std::thread* ptr = nullptr;  // In DllMain:
ptr = new std::thread(...);
```

#### Conflicting with Roblox Code

Roblox might patch or hook similar APIs:
- Window creation
- Memory allocation
- Thread creation

**Solution:**
```cpp
// Use __try/__except around everything
__try {
    // Window creation code
}
__except (EXCEPTION_EXECUTE_HANDLER) {
    // Didn't work, but don't crash
    return;
}
```

#### Solution: Attach Debugger

```
1. Open Visual Studio
2. Debug → Attach to Process
3. Find RobloxPlayerBeta.exe in the list
4. Click Attach
5. Run executor and inject
6. If crash, debugger will break
7. Look at Call Stack to see what failed
```

---

## Problem: Overlay Doesn't Appear

### Symptom
DLL injects, no crash, but "Minkio v1.0" text doesn't show

### Causes & Solutions

#### Check 1: Is Window Actually Created?

```cpp
// In OverlayThreadProc(), add debug output:
if (!g_OverlayWindow) {
    OutputDebugStringW(L"[Overlay] Window creation failed\n");
    return;
}
OutputDebugStringW(L"[Overlay] Window created successfully\n");
```

#### Check 2: Is Window Off-Screen?

```cpp
// In OverlayThreadProc():
int screenWidth = GetSystemMetrics(SM_CXSCREEN);
int screenHeight = GetSystemMetrics(SM_CYSCREEN);
printf("Screen: %d x %d\n", screenWidth, screenHeight);

int posX = screenWidth - OVERLAY_WIDTH - OVERLAY_MARGIN;
int posY = screenHeight - OVERLAY_HEIGHT - OVERLAY_MARGIN;
printf("Overlay pos: %d,%d\n", posX, posY);

// If posX or posY is negative or huge, it's off-screen
```

**Solution:**
```cpp
// Clamp position:
if (posX < 0) posX = 0;
if (posY < 0) posY = 0;
if (posX + OVERLAY_WIDTH > screenWidth) 
    posX = screenWidth - OVERLAY_WIDTH;
if (posY + OVERLAY_HEIGHT > screenHeight) 
    posY = screenHeight - OVERLAY_HEIGHT;
```

#### Check 3: Is Window Visible?

```cpp
// Make sure ShowWindow is called
ShowWindow(g_OverlayWindow, SW_SHOW);
UpdateWindow(g_OverlayWindow);

// And WS_VISIBLE flag is set
WS_POPUP | WS_VISIBLE
```

#### Check 4: Is Text Color Visible?

If background and text are both black:

```cpp
// In OverlayWindowProc WM_PAINT:
SetTextColor(hdc, RGB(255, 255, 255));  // WHITE text
// If this is black, text won't show on black background
```

#### Check 5: Try Simpler Window

```cpp
// Replace complex window with simple one:
g_OverlayWindow = CreateWindowExW(
    WS_EX_TOPMOST,                   // Just topmost
    OVERLAY_CLASS_NAME,
    L"Minkio v1.0",                  // Title shows in bottom-right
    WS_POPUP | WS_VISIBLE,
    posX, posY, 200, 50,
    NULL, NULL, GetModuleHandleW(NULL), NULL
);
// This might work even if full painting fails
```

---

## Problem: Named Pipe Connection Fails

### Symptom
"Script pipe not available" message after injection

### Causes & Solutions

#### Check 1: Is Pipe Server Running?

In `Minkio.cpp`, verify `InitializeNamedPipeServer()` is called:

```cpp
InitializeNamedPipeServer();  // Should be called in DllMain
```

#### Check 2: Is Named Pipe Thread Running?

```cpp
// Add debug output in PipeServerThreadProc():
OutputDebugStringW(L"[Pipe] Server thread started\n");
```

If you don't see this, pipe thread isn't starting.

#### Check 3: Can You Connect with Test?

```csharp
// In C#:
try {
    using (var pipeClient = new NamedPipeClientStream(".", "Minkio_Script_Server")) {
        pipeClient.Connect(1000);
        Console.WriteLine("Connected!");
    }
} catch (Exception ex) {
    Console.WriteLine("Failed: " + ex.Message);
}
```

If this fails, pipe server isn't listening.

#### Check 4: Pipe Name Mismatch

Make sure pipe name matches in both DLL and C#:

**In NamedPipeServer.hpp:**
```cpp
const wchar_t* PIPE_NAME = L"\\\\.\\pipe\\Minkio_Script_Server";
```

**In ScriptPipeClient.cs:**
```csharp
private const string PIPE_NAME = "Minkio_Script_Server";
```

Both must match!

#### Solution: Wait Longer After Injection

Pipe server might still initializing:

```csharp
// In executor:
Thread.Sleep(2000);  // Wait 2 seconds after injection
if (ScriptPipeClient.IsPipeAvailable()) {
    // Now try to execute scripts
}
```

---

## Problem: Script Execution Fails

### Symptom
Script appears to execute but nothing happens in Roblox

### Most Likely Cause: Lua Offsets Are Wrong

When Roblox updates, function addresses change.

#### Check 1: Verify Offsets Are Found

```cpp
// In LuaExecution.hpp, add debug:
if (!InitializeLuaFunctions()) {
    OutputDebugStringW(L"[Lua] Offset finding failed\n");
    return false;
}
```

#### Check 2: Use Test Scripts

Start with simplest possible script:

```lua
print("Hello")
```

If this doesn't appear in console, offsets are wrong.

#### Check 3: How to Find New Offsets

When Roblox updates:

1. **Use IDA Pro or x64dbg to analyze RobloxPlayerBeta.exe**
2. **Find Lua functions by searching for error strings**
3. **Extract new offsets**
4. **Update PatternScanning.hpp**
5. **Rebuild DLL**

**Detailed walkthrough:**

```
In x64dbg:
1. Ctrl+G → Go to address
2. Type: RobloxPlayerBeta (jumps to module base)
3. Ctrl+B → Binary Search
4. Search for string: "lua"
5. Look for functions with names like:
   - lua_getglobal
   - lua_pcall
   - luaL_loadstring
6. Right-click → Disassemble
7. Note the address from the left column
8. Calculate offset: address - RobloxPlayerBeta base
9. Update code with new offset
```

#### Check 4: Verify Lua State Pointer

```cpp
// Add this to debug:
if (!g_LuaState || (uintptr_t)g_LuaState < 0x10000) {
    OutputDebugStringW(L"[Lua] Invalid Lua state pointer\n");
    return false;
}
```

####Check 5: Simple Test Function

Create minimal Lua executor:

```cpp
bool TestLuaExecution() {
    if (!g_LuaState || !g_lua_pcall) {
        return false;
    }
    
    // Try to call getglobal("print")
    __try {
        if (g_lua_getglobal) {
            g_lua_getglobal(g_LuaState, "print");
        }
        return true;
    }
    __except (...) {
        return false;
    }
}
```

---

## Problem: "File Not Found" Errors

### Solution Overview

```
Error: "Minkio.dll not found"

Possible locations to check:
1. MinkioExecutor\bin\Release\Minkio.dll          ← Most likely here
2. Minkio-Injector\x64\Release\Minkio.dll         ← Built here
3. C:\Windows\System32\ or SysWOW64\             ← Should NOT be here
```

### Fixingthe Path

```cpp
// In MinkioInjector.cs:
string dllPath = Path.Combine(
    Path.GetDirectoryName(Application.ExecutablePath),  // Executor directory
    "Minkio.dll"                                        // DLL filename
);

// Verify existence:
if (!File.Exists(dllPath)) {
    Console.WriteLine("Not found at: " + dllPath);
    return false;
}
```

### Manual Copy

```batch
REM After building DLL:
copy x64\Release\Minkio.dll MinkioExecutor\bin\Release\

REM Verify:
dir MinkioExecutor\bin\Release\Minkio.dll
```

---

## Problem: "Access Denied"

### Causes  & Solutions

#### Solution 1: Run as Administrator

```
Right-click MinkioExecutor.exe
→ Properties
→ Compatibility tab
→ Check "Run this program as an administrator"
→ Click Apply
→ Try again
```

#### Solution 2: Antivirus Blocking

Roblox injection might be flagged as suspicious:

```
1. Open Windows Defender
2. Go to Virus & Threat Management
3. Manage Settings
4. Add Exceptions
5. Add MinkioExecutor.exe
```

Or temporarily disable antivirus during testing.

#### Solution 3: Process Already Running

```batch
REM Check if Roblox process exists:
tasklist | findstr RobloxPlayerBeta

REM If multiple instances, close all and start fresh
taskkill /IM RobloxPlayerBeta.exe /F
```

---

## Advanced: Using a Debugger

### Visual Studio Debugger

```
1. Open Visual Studio
2. Debug → Attach to Process
3. Find: RobloxPlayerBeta.exe
4. Click Attach
5. Run MinkioExecutor.exe and inject
6. If crash, debugger breaks and shows:
   - Exact address where crash occurred
   - Call stack (how we got there)
   - Local variables and memory
```

### x64dbg (Free Alternative)

```
1. Download from: https://x64dbg.com/
2. Run x64dbg
3. File → Open
4. Select: RobloxPlayerBeta.exe
5. Press F9 (run)
6. Roblox starts in debugger
7. Inject DLL
8. If crash, debugger catches it
9. Use Disassembler tab to see code
```

---

## Logging & Debugging

### Add Console Output

```cpp
// In Minkio.cpp DllMain:
AllocConsole();
FILE* pFile;
freopen_s(&pFile, "CONOUT$", "w", stdout);
freopen_s(&pFile, "CONOUT$", "r", stdin);

printf("[Minkio] DLL loaded successfully!\n");
fprintf(stderr, "[Minkio] Error message\n");
```

### Use OutputDebugString

```cpp
// Works anywhere:
OutputDebugStringW(L"[Minkio] Important message\n");

// View with:
// - Visual Studio Output window
// - DebugView: https://docs.microsoft.com/en-us/sysinternals/downloads/debugview
```

### Write to File

```cpp
// Log to file:
FILE* log = fopen("C:\\Minkio_Debug.log", "a");
if (log) {
    fprintf(log, "[%s] Message\n", __FUNCTION__);
    fclose(log);
}
```

---

## Checklist for Common Issues

When troubleshooting, verify:

- [ ] Visual Studio 2022 installed with C++ tools
- [ ] `build_dll.bat` completes successfully
- [ ] `Minkio.dll` is 64-bit (`wmic` check)
- [ ] DLL is in `MinkioExecutor\bin\Release\` directory
- [ ] Roblox is running and fully loaded
- [ ] Executor runs with administrator privileges
- [ ] Firewall is not blocking connections
- [ ] No antivirus quarantining the DLL
- [ ] Named pipe server thread started (check debug output)
- [ ] Lua offsets match current Roblox version
- [ ] Script syntax is valid Lua

---

## When All Else Fails

1. **Rebuild everything from scratch:**
   ```batch
   rmdir /s x64
   build_dll.bat
   copy x64\Release\Minkio.dll MinkioExecutor\bin\Release\
   ```

2. **Check Event Viewer for crash details:**
   ```
   eventvwr.msc → Application → Look for crashes
   ```

3. **Enable maximum debug output:**
   ```cpp
   // Add OutputDebugStringW to every function
   // Use DebugView to capture all output
   ```

4. **Test with simplest possible script:**
   ```lua
   print("test")
   ```

5. **Attach debugger and trace execution**

---

**Remember:** Most crashes are in initialization. If overlay is disabled and game still crashes, it's elsewhere in DllMain.

If pipe fails, offsets are likely wrong - verify Lua function addresses.

Good luck debugging!
