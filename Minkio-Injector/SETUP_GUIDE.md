# Minkio Executor - Complete Setup & Deployment Guide

## Table of Contents

1. [Project Overview](#project-overview)
2. [Architecture](#architecture)
3. [Required Files](#required-files)
4. [Step 1: Build the DLL](#step-1-build-the-dll)
5. [Step 2: Build the C# Projects](#step-2-build-the-c-projects)
6. [Step 3: Setup Directory Structure](#step-3-setup-directory-structure)
7. [Step 4: Troubleshooting](#step-4-troubleshooting)
8. [Step 5: Finding & Updating Offsets](#step-5-finding--updating-offsets)
9. [Theory & Architecture Deep Dive](#theory--architecture-deep-dive)

---

## Project Overview

**Minkio** is a Roblox script executor consisting of:

1. **Minkio DLL** (C++) - Injected into Roblox, displays overlay, executes Lua scripts
2. **MinkioAPI** (C# Library) - Provides injection functionality
3. **MinkioExecutor** (C# WinForms) - GUI for the executor
4. **MinkioInjector** (External tool) - Handles DLL injection

### What the System Does

- ✅ Injects `Minkio.dll` into `RobloxPlayerBeta.exe`
- ✅ Displays "Minkio v1.0" overlay notification
- ✅ Creates named pipe for IPC (Inter-Process Communication)
- ✅ Executes Lua scripts sent from the executor
- ✅ All without crashing the game

---

## Architecture

```
User (You)
    ↓
MinkioExecutor.exe (C# WinForms GUI)
    ↓
MinkioAPI (C# Class Library)
    ├─→ MinkioInjector class (handles injection)
    └─→ ScriptPipeClient (sends scripts via named pipe)
    ↓
MinkioInjector.exe (External injector tool)
    ↓
LoadLibraryA (Windows API)
    ↓
RobloxPlayerBeta.exe (Game Process)
    ↓
Minkio.dll (Injected DLL)
    ├─ Overlay.hpp (displays notification)
    ├─ NamedPipeServer.hpp (receives scripts)
    ├─ LuaExecution.hpp (executes Lua)
    ├─ PatternScanning.hpp (finds Lua functions)
    └─ Minkio.cpp (DLL entry point)
```

---

## Required Files

### C++ DLL (Minkio)
- ✅ `Minkio.cpp` - Main DLL entry point
- ✅ `Overlay.hpp` - Overlay display implementation
- ✅ `PatternScanning.hpp` - Pattern scanning for offsets
- ✅ `NamedPipeServer.hpp` - IPC named pipe server
- ✅ `LuaExecution.hpp` - Lua script execution
- ✅ `Minkio.vcxproj` - Visual Studio project

### C# API (MinkioAPI)
- ✅ `MinkioInjector.cs` - Injection functionality (improved)
- ✅ `ScriptPipeClient.cs` - Named pipe client
- ✅ `MinkioAPI.csproj` - Project file

### C# Executor (MinkioExecutor)
- ✅ `ExecutorForm.cs` or `ExecutorFormExample.cs` - GUI
- ✅ `MinkioExecutor.csproj` - Project file

### Build Tools
- ✅ `build_dll.bat` - Automated build script (IMPROVED)

---

## Step 1: Build the DLL

### 1.1 Prerequisites

Install Visual Studio 2022 with:
- Desktop Development with C++
- Windows SDK 10.0 or later
- C++17 or later compiler

### 1.2 Build Process

**Open Command Prompt** in the project directory and run:

```bash
cd C:\Users\minks\OneDrive\Desktop\Minkio-Injector
build_dll.bat
```

**What happens:**
1. Script locates Visual Studio 2022 automatically (using vswhere)
2. Cleans previous builds
3. Compiles `Minkio.cpp` and headers into `Minkio.dll`
4. Outputs: `x64\Release\Minkio.dll` (~150-200 KB)

**Expected output:**
```
============================================================================
                        BUILD SUCCESSFUL!
============================================================================
Output DLL:    C:\Users\...\x64\Release\Minkio.dll
DLL Size:      xxxxxx bytes
Architecture:  x64 (64-bit)
Configuration: Release
```

### 1.3 Verify Build

Check that `x64\Release\Minkio.dll` exists:

```bash
dir x64\Release\Minkio.dll
```

---

## Step 2: Build the C# Projects

### 2.1 Using Visual Studio

1. Open `Injector-master\Injector.slnx` (or similar solution file)
2. Build → Build Solution
3. Ensure both **MinkioAPI** and **MinkioExecutor** build successfully
4. Output directory typically: `bin\Release\`

### 2.2 Using Command Line (MSBuild)

```bash
msbuild "Injector-master\Injector.slnx" /p:Configuration=Release /p:Platform=x64
```

---

## Step 3: Setup Directory Structure

After building, your structure should be:

```
Minkio-Injector/
├── Minkio.cpp                    (DLL source)
├── Overlay.hpp                   (Overlay implementation)
├── PatternScanning.hpp           (Pattern scanning)
├── NamedPipeServer.hpp           (Named pipe IPC)
├── LuaExecution.hpp              (Lua execution)
├── Minkio.vcxproj                (DLL project)
├── build_dll.bat                 (Build script)
├── x64/Release/
│   └── Minkio.dll               (⭐ COMPILED DLL)
│
├── MinkioAPI/
│   ├── MinkioInjector.cs
│   ├── ScriptPipeClient.cs
│   ├── MinkioAPI.csproj
│   └── bin/Release/
│       └── MinkioAPI.dll
│
├── MinkioExecutor/
│   ├── ExecutorFormExample.cs
│   ├── MinkioExecutor.csproj
│   └── bin/Release/
│       ├── MinkioExecutor.exe    (⭐ RUN THIS)
│       └── [dependencies]
│
└── Injector-master/
    ├── Injector/
    └── ...
```

### 3.1 Copy Minkio.dll to Executor

**CRITICAL:** Copy the compiled DLL to the executor's output directory:

```bash
copy x64\Release\Minkio.dll MinkioExecutor\bin\Release\
```

The executor will launch and look for `Minkio.dll` in the same directory.

### 3.2 Prepare MinjkioInjector.exe

If you have a separate `MinkioInjector.exe` tool:

```bash
copy <path_to_injector>\MinkioInjector.exe MinkioExecutor\bin\Release\
```

---

## Step 4: Test the Injection

### 4.1 Launch Roblox

1. Start Roblox (join a game)
2. Wait for it to fully load

### 4.2 Run the Executor

1. Navigate to `MinkioExecutor\bin\Release\`
2. Run `MinkioExecutor.exe`
3. Click the **"Attach"** button

### 4.3 Verify Injection

**You should see:**
- Status changes to "SUCCESS: Minkio.dll is now loaded"
- "Minkio v1.0" overlay appears in bottom-right corner of Roblox
- Script pipe becomes available

**If this doesn't happen:**
- See [Troubleshooting](#step-4-troubleshooting) section below

### 4.4 Execute a Script

1. Click the **"Execute"** button
2. Script should run in Roblox
3. Output visible in Roblox console (if accessed)

---

## Step 4: Troubleshooting

### Problem: Roblox Crashes Immediately After Injection

**Cause:** The overlay initialization may have an issue OR the DLL has a crash in DllMain

**Solutions:**
1. Check that DLL is 64-bit:
   ```bash
   wmic datafile where name="C:\...\Minkio.dll" get Description,Version
   ```
2. Rebuild with debug info:
   - Edit `Minkio.vcxproj` and change Configuration to Debug
   - Rebuild and test with a debugger attached

3. Disable the overlay temporarily - edit `Minkio.cpp`:
   ```cpp
   // Comment out overlay initialization
   // InitializeOverlay();
   ```

4. Check Windows Event Viewer for crash logs

### Problem: Overlay Doesn't Appear

**Cause:** Overlay thread may not have initialized

**Solutions:**
1. Check that window class registration succeeds
2. Verify screen resolution is correct (overlay position calculation)
3. Add debug prints in `OverlayThreadProc()` to see if it's running

### Problem: Script Pipe Connection Fails

**Cause:** Named pipe server in DLL not initialized

**Solutions:**
1. Verify injection succeeded (DLL loaded)
2. Wait longer after injection before executing script (DLL may still initializing)
3. Check that `InitializeNamedPipeServer()` is called in `Minkio.cpp`

### Problem: Script Seems to Execute But Nothing Happens

**Cause:** Lua offsets are wrong OR script execution fails silently

**Solutions:**
1. Use a simple test script: `print('test')`
2. Check Roblox console for output
3. See "Finding & Updating Offsets" section below

### Problem: "MinkioInjector.exe not found"

**Cause:** Missing or misplaced injector executable

**Solutions:**
1. If you have a custom injector, place it in the same directory as `MinkioExecutor.exe`
2. If not, the code will try to use Windows API directly (may require implementation)

---

## Step 5: Finding & Updating Offsets

### Understanding Patterns

Roblox updates frequently. When it does:
1. Function addresses change
2. Pattern scanning finds them dynamically
3. You may need to update patterns

### Approach 1: Use Existing Patterns (Recommended)

The code in `PatternScanning.hpp` attempts to find Lua functions using byte patterns. These should be relatively stable across updates.

### Approach 2: Manual Pattern Scanning

**When patterns fail:**

1. **Get Roblox Base Address:**
   ```
   tasklist /m RobloxPlayerBeta.exe
   (note the PID and module address)
   ```

2. **Open with Debugger:**
   - Open x64dbg or IDA Pro
   - Attach to RobloxPlayerBeta.exe process
   - Press Ctrl+G to go to address
   - Type: `RobloxPlayerBeta`

3. **Search for Lua Strings:**
   - Ctrl+B (Binary Search)
   - Search for string: "lua_getglobal" or similar
   - Right-click → Disassemble

4. **Find Function Address:**
   - Look for function prologue: `55 48 89 E5` or similar
   - Calculate offset from module base
   - Update `PatternScanning.hpp` with new offsets

### Approach 3: Automated Offset Finding

**In production, implement:**

```cpp
// Create tool to scan and report offsets
// Run at startup, log results to file
// Update hardcoded offsets based on output
```

### Example: Finding lua_pcall

```
In IDA Pro:
1. Strings View (Shift+F12)
2. Search for "lua_getglobal"
3. Go to address
4.Right-click → Jump to disassemble
5. Look for nearby calls to lua_pcall
6. Extract offset = address - RobloxPlayerBeta.exe base
```

---

## Theory & Architecture Deep Dive

### Why Three Layers?

**Layer 1: C# Executor (MinkioExecutor.exe)**
- User-friendly GUI
- No admin rights needed
- Easy to modify and test

**Layer 2: C# API (MinkioAPI.dll)**
- Encapsulates injection logic
- Reusable for other projects
- Handles communication with DLL

**Layer 3: C++ DLL (Minkio.dll)**
- Runs inside Roblox process memory
- Direct access to Lua state
- Can execute Roblox Lua directly

### Thread Safety

**Why separate threads?**

```
If init happens on main thread:
  Game Main Thread → DllMain → Overlay Init
  ↓ (blocked)
  Can take > 100ms
  ↓ (game stutters)
  Game hangs/crashes

With separate thread:
  Game Main Thread → DllMain → CreateThread(Init)
  ↓ (returns immediately)
  Game continues @ 60 FPS
  ↓ (smooth experience)
  Init thread does work in background
```

**Thread-safe mechanisms:**

1. **Atomic Bool** - `std::atomic<bool>` for flags
   ```cpp
   std::atomic<bool> g_PipeServerRunning(false);
   // No mutex, no deadlock risk
   ```

2. **Named Pipes** - Built-in Windows IPC
   ```cpp
   CreateNamedPipeW(PIPE_NAME, ...);
   // Handles serialization automatically
   ```

3. **__try/__except**
   ```cpp
   __try { 
       // Code that might crash
   } 
   __except (EXCEPTION_EXECUTE_HANDLER) {
       // Handle gracefully
   }
   ```

### Pattern Scanning Theory

**Why use patterns instead of hardcoded offsets?**

Roblox updates → Binary changes → Offsets change

**How patterns work:**

```
Search for: 48 8B 0D ?? ?? ?? ??
                ↑↑↑ Code that never changes
                ??? ↑↑↑ Offset that changes (we skip with ?? wildcards)

Pattern found at: 0x123456
Offset (RIP-relative): *(int32_t*)(0x123456 + 3)
Actual address = 0x123456 + 7 + offset
```

**Pattern stability:**

- ✅ Opcodes (mov, call, etc.) usually stay the same
- ❌ Offsets in immediates (addresses) change every update
- ✅ Patterns with few dependencies survive updates longer
- ❌ Patterns depending on nearby code are fragile

### Named Pipe IPC Theory

**Why named pipes?**

```
Option 1: Direct memory write
  ❌ Requires finding shared memory location
  ❌ No synchronization
  ❌ Race conditions possible

Option 2: Network socket
  ✅ Standard protocol
  ❌ Slower (localhost)
  ❌ Requires opening ports

Option 3: Named pipes (Chosen)
  ✅ Ultra-fast (same machine)
  ✅ Built-in synchronization
  ✅ No security issues
  ✅ Easy to implement
```

**How it works:**

```
C# Side:                          C++ Side (DLL):
    ↓
OpenNamedPipe                     CreateNamedPipe (listening)
    ↓
Write script                      ConnectNamedPipe (blocks)
Flush                                 ↓
    ↓                             ReadFile (receives script)
                                      ↓
                                  ExecuteLua()
                                      ↓
                                  WriteFile (ACK)
Read ACK ← ←← ← ← ← ← ← ← ← ← ← ← ↑
    ↓
Done
```

---

## Maintenance & Updates

### When Roblox Updates

1. Test if injection still works
2. If not, update patterns in `PatternScanning.hpp`
3. Rebuild DLL with `build_dll.bat`
4. Deploy new DLL

### Adding New Features

**To add a new feature:**

1. Create a new `.hpp` file in the DLL project
2. Implement the feature with proper error handling
3. Call it from `Minkio.cpp` DllMain
4. Rebuild and test

### Debugging

**Enable debug output:**

```cpp
// In Minkio.cpp:
OutputDebugStringW(L"[Minkio] Debug message\n");

// View with:
// - Visual Studio Output window
// - DebugView (SysInternals)
```

---

## Common Mistakes to Avoid

❌ **Don't call Roblox functions from main thread**
```cpp
// WRONG:
DllMain {
    CallRobloxFunction();  // Blocks game
}

// RIGHT:
DllMain {
    CreateThread([]{
        CallRobloxFunction();  // Separate thread
    });
}
```

❌ **Don't use hardcoded memory addresses**
```cpp
// WRONG:
uintptr_t lua_pcall = (uintptr_t)0x140000000;
// Breaks next Roblox update!

// RIGHT:
uintptr_t lua_pcall = PatternScanner::FindPattern(...);
// Works across updates
```

❌ **Don't crash the game**
```cpp
// WRONG:
int* ptr = nullptr;
*ptr = 0;  // Crash!

// RIGHT:
__try {
    int* ptr = ...;
    *ptr = 0;
}
__except (...) {
    // Handle gracefully
}
```

❌ **Don't block the game's render thread**
```cpp
// WRONG:
while(true) {
    ExecuteNextScript();  // Busy wait
}

// RIGHT:
WaitForSingleObject(hPipeHandle, INFINITE);  // Event-driven
ExecuteNextScript();
```

---

## Summary Checklist

- [ ] Visual Studio 2022 installed with C++ tools
- [ ] `build_dll.bat` runs successfully (produces `Minkio.dll`)
- [ ] `Minkio.dll` copied to executor output directory
- [ ] MinkioAPI and MinkioExecutor compile without errors
- [ ] Roblox launched and fully loaded
- [ ] Executor.exe runs and finds Roblox
- [ ] Click "Attach" → DLL injected without crash
- [ ] "Minkio v1.0" overlay appears
- [ ] Script execution works
- [ ] All error messages are meaningful/helpful

---

## Next Steps

1. ✅ Build and inject the DLL
2. ✅ Verify overlay appears
3. ✅ Test basic script execution
4. 🔄 Update offsets for current Roblox version
5. 🔄 Add more features (FPS counter, watermark, etc.)
6. 🔄 Implement anti-detection (if desired)

---

**Good luck with Minkio!**

For issues, check the troubleshooting section or investigate using your debugger.
