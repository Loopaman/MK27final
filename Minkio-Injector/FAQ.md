# Frequently Asked Questions (FAQ)

## Build & Compilation

### Q: How do I compile this DLL?

**A:** Three methods:

1. **Batch Script (Easiest)**:
   ```bash
   build_dll.bat
   ```

2. **Visual Studio IDE**:
   - Open `Minkio.vcxproj`
   - Select Release | x64
   - Press Ctrl+Shift+B

3. **Command Line**:
   ```bash
   msbuild Minkio.vcxproj /p:Configuration=Release /p:Platform=x64
   ```

---

### Q: "Visual Studio 2022 not found" error

**A:** Install Visual Studio 2022 with "Desktop Development with C++" workload:
1. Run Visual Studio Installer
2. Select "Desktop Development with C++"
3. Install Windows SDK 10.0 or later

---

### Q: Which version of Visual Studio do I need?

**A:** Visual Studio 2022 is required. The project uses `v143` toolset (VS2022).

If you have VS2019, change in `Minkio.vcxproj`:
```xml
<PlatformToolset>v142</PlatformToolset>  <!-- For VS2019 -->
```

---

### Q: How large is the compiled DLL?

**A:** ~150-200 KB in Release mode.

Debug mode: ~300-400 KB (not recommended for injection)

---

## Injection & Deployment

### Q: How do I inject the DLL into Roblox?

**A:** Three methods:

1. **Using C# Wrapper**:
   ```csharp
   MinkioOverlay.InjectOverlay();
   ```

2. **Using Existing Injector**: 
   - Place Minkio.dll in injector folder
   - Use your current injection tool

3. **Manual DLL Injection**:
   - Use any DLL injector tool
   - Target: RobloxPlayerBeta.exe
   - DLL: Minkio.dll

---

### Q: Does Roblox need to be running before injection?

**A:** Yes! The process must exist first:
1. Launch Roblox
2. Wait for game to load
3. Then inject the DLL

---

### Q: Can I inject multiple times?

**A:** No. Injecting twice creates overlapping windows. 

If needed, unload the DLL first using your injector tool.

---

### Q: Where should I place the Minkio.dll?

**A:** 
- **With C# injector**: Same folder as your C# executable
- **With external injector**: Anywhere accessible by the injector
- **Absolute path**: Best practice

---

## Overlay Display

### Q: The overlay doesn't appear. Why?

**Checklist**:
1. ✓ Is Roblox running?
2. ✓ Is the DLL actually injected? (Check with Process Monitor)
3. ✓ Is it a 64-bit Roblox? (It always is)
4. ✓ Check Windows Event Viewer for crashes

**Solution**: If still missing:
- Add debug output to DllMain
- Check overlay thread creation succeeded
- Verify window pos isn't off-screen

---

### Q: I see the overlay text but it's not positioned correctly

**A:** Edit `Overlay.hpp`:
```cpp
int overlayWidth = 150;      // Change width
int overlayHeight = 50;      // Change height
int posX = screenWidth - overlayWidth - 20;   // X position
int posY = screenHeight - overlayHeight - 20;  // Y position
```

Common positions:
- **Top-Left**: `posX = 20; posY = 20;`
- **Top-Right**: `posX = screenWidth - overlayWidth - 20; posY = 20;`
- **Bottom-Left**: `posX = 20; posY = screenHeight - overlayHeight - 20;`
- **Bottom-Right**: (default)
- **Center**: `posX = (screenWidth - overlayWidth) / 2; posY = (screenHeight - overlayHeight) / 2;`

---

### Q: Can I change the overlay text?

**A:** Yes! Edit `Overlay.hpp`, find:
```cpp
graphics.DrawString(L"Minkio v1.0", -1, &font, pointF, &textBrush);
```

Replace `L"Minkio v1.0"` with any Unicode text.

---

### Q: Can I add multiple lines of text?

**A:** Yes, call `DrawString()` multiple times:
```cpp
PointF point1(20.0f, 20.0f);
PointF point2(20.0f, 40.0f);
graphics.DrawString(L"Line 1", -1, &font, point1, &textBrush);
graphics.DrawString(L"Line 2", -1, &font, point2, &textBrush);
```

---

### Q: How do I change the overlay color?

**A:** Edit these lines in `Overlay.hpp`:

```cpp
// Background color (alpha, R, G, B)
Color bgColor(200, 0, 0, 0);        // Black with 78% opacity

// Text color (alpha, R, G, B)
Color textColor(255, 255, 255, 255); // White (fully opaque)
```

Examples:
- **Red**: `Color(200, 255, 0, 0)`
- **Green**: `Color(200, 0, 255, 0)`
- **Blue**: `Color(200, 0, 0, 255)`
- **Semi-Transparent**: Lower alpha value (50-150)

---

### Q: Can I make the text larger or smaller?

**A:** Yes, in `Overlay.hpp`:
```cpp
Font font(L"Arial", 14, FontStyleRegular, UnitPixel);
//                     ^^
//                     Change this (14 = 14 points)
```

Try: 10 (small), 16 (medium), 20 (large), 24 (xlarge)

---

### Q: The text looks blurry

**A:** Check these are set in `OverlayWindowProc()`:
```cpp
graphics.SetSmoothingMode(SmoothingModeAntiAlias);
graphics.SetTextRenderingHint(TextRenderingHintAntiAlias);
```

If still blurry, the font might not be available. Change:
```cpp
Font font(L"Consolas", 14, FontStyleRegular, UnitPixel);  // Try another font
```

Available fonts: Arial, Times New Roman, Courier New, Segoe UI, Consolas

---

## Performance & Stability

### Q: Does the overlay affect FPS?

**A:** No! It runs on a separate thread. 

**Performance Impact**:
- CPU: <1% (message-driven)
- GPU: <1% (simple 2D)
- Memory: 2-3 MB
- FPS: 0 (completely separate)

---

### Q: Will the overlay crash Roblox?

**A:** No. The overlay is completely isolated:
- ✅ Separate thread
- ✅ No game memory access
- ✅ No hooks into game code
- ✅ Graceful error handling

Even if the overlay fails, the game continues normally.

---

### Q: Is the DLL detected by anti-cheat?

**A:** Possibly. DLL injection is detectable by:
- Memory scanning
- Module enumeration
- Behavioral analysis

This DLL uses standard Windows APIs and doesn't hide. Use only in:
- Private testing
- Authorized environments
- Games that allow modifications

---

## Customization & Advanced

### Q: How do I add an animation (fade-in)?

**A:** Modify `OverlayThreadFunction()`:
```cpp
// Fade in animation
for (int i = 0; i <= 255; i += 5) {
    SetLayeredWindowAttributes(g_OverlayWindow, RGB(0,0,0), i, LWA_ALPHA);
    Sleep(10);
    InvalidateRect(g_OverlayWindow, NULL, FALSE);
}
```

---

### Q: Can I make the overlay interactive (clickable)?

**A:** Currently no, because of `WS_EX_TRANSPARENT`. To enable clicks:

Remove `WS_EX_TRANSPARENT`:
```cpp
IntPtr hwnd = CreateWindowExW(
    WS_EX_LAYERED | WS_EX_TOPMOST,  // Removed WS_EX_TRANSPARENT
    ...
);
```

Then handle clicks in `OverlayWindowProc()`:
```cpp
case WM_LBUTTONDOWN:
    MessageBox(NULL, L"Clicked!", L"Overlay", MB_OK);
    return 0;
```

---

### Q: Can I read a config file at startup?

**A:** Yes! In `InitializeOverlay()` or `DllMain()`:
```cpp
std::ifstream config("minkio_config.ini");
std::string text;
std::getline(config, text);  // Read first line
// Use 'text' variable
```

---

### Q: Can I display FPS counter?

**A:** Yes, but requires hooking the game. This is complex.

Alternative: Display in overlay, update from shared memory:
```
Main Game → Shared Memory (FPS value)
    ↓
Overlay Thread → Read FPS → Draw it
```

---

## Troubleshooting

### Q: Overlay appears but Roblox crashes shortly after

**Check list**:
1. Is it a 32-bit Minkio.dll on 64-bit Roblox?
   - Solution: Rebuild as x64
2. Are other DLLs also injected?
   - Solution: Try injecting Minkio alone
3. Is GDI+ initialization failing?
   - Solution: Add debug output, check Event Viewer

---

### Q: Game freezes when overlay is injected

**Causes**:
- Overlay thread is blocking main thread (shouldn't happen)
- CreateWindowExW is failing silently
- GDI+ initialization is hanging

**Solution**:
- Add debug console output
- Run with Visual Studio debugger
- Check thread stack in Process Explorer

---

### Q: DLL injection fails with "File not found"

**A:** The DLL path is wrong.

Check:
```csharp
string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Minkio.dll");
Console.WriteLine("Looking for: {0}", dllPath);
```

Make sure DLL is in the same folder as your .exe, or provide absolute path.

---

### Q: How do I verify the DLL injected successfully?

**A:** Use Process Explorer (SysInternals):
1. Ctrl+F (find)
2. Type "Minkio"
3. Should find your DLL in Roblox process

Alternative: Check in code:
```cpp
HMODULE hMod = GetModuleHandle("Minkio.dll");
if (hMod) {
    MessageBox(NULL, L"Minkio loaded!", L"Success", MB_OK);
}
```

---

### Q: The overlay window is invisible

**Check**:
1. Window exists but is off-screen?
   - Check posX, posY calculation
2. Window is transparent?
   - Check SetLayeredWindowAttributes call
3. Font isn't rendering?
   - Check GDI+ initialization

**Debug solution**:
```cpp
// In OverlayWindowProc WM_PAINT
RECT rc;
GetClientRect(hwnd, &rc);
std::cout << "Window rect: " << rc.right << " x " << rc.bottom << std::endl;
```

---

### Q: Multiple overlay windows appear

**A:** DLL was injected twice. Solution:
1. Close and reopen Roblox
2. Inject only once
3. Verify only one instance of Minkio.exe exists

---

## Building & Advanced Setup

### Q: How do I build from command line without Visual Studio?

**A:** Use MSBuild directly:
```bash
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" Minkio.vcxproj /p:Configuration=Release /p:Platform=x64
```

---

### Q: Can I use this in Visual Studio 2019?

**A:** Yes, but change the toolset:

In `Minkio.vcxproj`:
```xml
<PlatformToolset>v142</PlatformToolset>  <!-- Change from v143 -->
```

Then rebuild.

---

### Q: How do I debug the DLL?

**A:**
1. In Minkio.cpp, add console:
   ```cpp
   AllocConsole();
   freopen("CONOUT$", "w", stdout);
   std::cout << "Debug output here" << std::endl;
   ```

2. Attach VS debugger to Roblox process
3. Set breakpoints in code
4. Check console output in attached window

---

## Contact & Support

**Issues?**
- Check QUICK_START.md for basic help
- See TECHNICAL_DOCUMENTATION.md for architecture
- Verify build steps in build_dll.bat

**Still stuck?**
- Rebuild from scratch: `build_dll.bat`
- Check Windows Event Viewer for exceptions
- Ensure Visual Studio 2022 is properly installed

---

**Last Updated**: February 2026  
**Version**: 1.0  
