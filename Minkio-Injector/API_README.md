# Minkio Injector API Documentation

## Overview
The MinkioAPI is a C# class library that provides a simple interface for injecting the Minkio DLL into Roblox. This allows you to create Windows Forms executors or other .NET applications that can easily inject into Roblox without rewriting injection code.

## Installation

1. **Add Reference in Visual Studio**
   - Right-click on your project → Add Reference
   - Browse to `MinkioAPI.dll` (from the release build)
   - Click Add

2. **Required Files**
   - Place `MinkioInjector.exe` and `Minkio.dll` in your application's output directory
   - OR provide full paths to the `InjectDLL()` method

## Quick Start

### Simple Injection (Auto-detect paths)
```csharp
using MinkioAPI;

// Inject DLL into Roblox (looks for files in app directory)
if (MinkioInjector.InjectDLL())
{
    MessageBox.Show("Injection successful!");
}
else
{
    MessageBox.Show("Injection failed!");
}
```

### Custom Paths
```csharp
string injectorPath = @"C:\path\to\MinkioInjector.exe";
string dllPath = @"C:\path\to\Minkio.dll";

if (MinkioInjector.InjectDLL(injectorPath, dllPath))
{
    MessageBox.Show("Injection successful!");
}
```

## API Reference

### Methods

#### `bool IsRobloxRunning()`
Checks if Roblox (RobloxPlayerBeta.exe) is currently running.

**Returns:** `true` if Roblox is running, `false` otherwise

**Example:**
```csharp
if (MinkioInjector.IsRobloxRunning())
{
    lblStatus.Text = "Roblox is running";
}
else
{
    lblStatus.Text = "Start Roblox first!";
}
```

---

#### `int GetRobloxPID()`
Gets the process ID of the running Roblox process.

**Returns:** Process ID as integer, or 0 if not found

**Example:**
```csharp
int pid = MinkioInjector.GetRobloxPID();
if (pid > 0)
{
    lblStatus.Text = $"Roblox PID: {pid}";
}
```

---

#### `bool InjectDLL(string injectorPath, string dllPath)`
Injects the Minkio DLL into Roblox using the specified injector executable.

**Parameters:**
- `injectorPath` - Full path to MinkioInjector.exe
- `dllPath` - Full path to Minkio.dll

**Returns:** `true` if injection succeeded, `false` otherwise

**Exceptions:**
- `FileNotFoundException` - If injector or DLL files don't exist
- `Exception` - If Roblox is not running

**Example:**
```csharp
try
{
    string injectorPath = @"C:\Roblox\MinkioInjector.exe";
    string dllPath = @"C:\Roblox\Minkio.dll";
    
    if (MinkioInjector.InjectDLL(injectorPath, dllPath))
    {
        MessageBox.Show("Success!");
    }
    else
    {
        MessageBox.Show("Failed!");
    }
}
catch (Exception ex)
{
    MessageBox.Show($"Error: {ex.Message}");
}
```

---

#### `bool InjectDLL()`
Injects the DLL using auto-detected paths (assumes files are in application directory).

**Returns:** `true` if injection succeeded, `false` otherwise

**Example:**
```csharp
if (MinkioInjector.InjectDLL())
{
    MessageBox.Show("Injection successful!");
}
```

---

#### `string GetStatus()`
Returns a formatted status string showing Roblox and file information.

**Returns:** Multi-line status string

**Example:**
```csharp
txtStatus.Text = MinkioInjector.GetStatus();
// Output:
// Roblox Running: Yes
// Roblox PID: 12345
// Injector Found: Yes
// DLL Found: Yes
```

---

### Constants

- `INJECTOR_NAME` - "MinkioInjector.exe"
- `DLL_NAME` - "Minkio.dll"
- `TARGET_PROCESS` - "RobloxPlayerBeta.exe"

## Windows Forms Example

```csharp
using System;
using System.Windows.Forms;
using MinkioAPI;

public partial class Form1 : Form
{
    public Form1()
    {
        InitializeComponent();
    }

    private void btnInject_Click(object sender, EventArgs e)
    {
        // Check if Roblox is running
        if (!MinkioInjector.IsRobloxRunning())
        {
            MessageBox.Show("Please start Roblox first!", "Error");
            return;
        }

        // Inject DLL
        btnInject.Enabled = false;
        btnInject.Text = "Injecting...";

        if (MinkioInjector.InjectDLL())
        {
            MessageBox.Show("Injection successful!", "Success");
        }
        else
        {
            MessageBox.Show("Injection failed!", "Error");
        }

        btnInject.Enabled = true;
        btnInject.Text = "Inject";

        // Update status
        txtStatus.Text = MinkioInjector.GetStatus();
    }

    private void btnRefresh_Click(object sender, EventArgs e)
    {
        txtStatus.Text = MinkioInjector.GetStatus();
    }
}
```

## Building the API

1. Open Visual Studio
2. Build `MinkioAPI` in Release mode
3. Copy the generated `MinkioAPI.dll` to your executor project
4. Add Reference to `MinkioAPI.dll` in your project
5. Use `using MinkioAPI;` in your code

## File Structure

```
MinkioAPI/
├── MinkioInjector.cs       (Main API class)
├── Properties/
│   └── AssemblyInfo.cs
└── MinkioAPI.csproj

MinkioExecutor/
├── ExecutorForm.cs         (Windows Forms example)
└── MinkioExecutor.csproj
```

## Notes

- The API automatically copies the DLL to the injector directory if needed
- Roblox must be running before injection
- The injection runs synchronously with a 10-second timeout
- All errors are safely handled and return false instead of throwing exceptions

## License

Use freely in your projects.
