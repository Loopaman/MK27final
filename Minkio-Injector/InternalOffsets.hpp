#pragma once

#include <Windows.h>
#include <cstdint>
#include <vector>
#include <cstring>
#include <string>

// ============================================================================
// PATTERN SCANNING FOR LUA FUNCTIONS - DYNAMIC OFFSET DISCOVERY
// This replaces hardcoded offsets that break on every Roblox update
// ============================================================================

/// <summary>
/// Pattern scanner that finds byte sequences in memory.
/// Patterns use 0x00 mask bytes as wildcards - safe for version changes.
/// </summary>
class PatternScanner
{
public:
    /// <summary>
    /// Find a byte pattern in memory range.
    /// mask[i] == 0xFF means pattern[i] must match exactly
    /// mask[i] == 0x00 means pattern[i] is a wildcard (any byte)
    /// </summary>
    static uintptr_t ScanMemory(
        const uint8_t* pattern,
        const uint8_t* mask,
        uintptr_t startAddr,
        size_t size)
    {
        if (!pattern || !mask || !startAddr || size == 0)
            return 0;

        __try
        {
            // Find pattern length (scan until we hit a fully-zeroed mask)
            int patternLen = 0;
            for (int i = 0; i < 256 && (mask[i] != 0x00 || pattern[i] != 0x00); i++)
                patternLen++;

            if (patternLen == 0 || patternLen > 256)
                return 0;

            // Search for pattern match
            for (uintptr_t addr = startAddr; addr < startAddr + size - patternLen; addr++)
            {
                uint8_t* data = (uint8_t*)addr;
                bool match = true;

                for (int i = 0; i < patternLen; i++)
                {
                    if (mask[i] == 0xFF && data[i] != pattern[i])
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                    return addr;
            }
        }
        __except (EXCEPTION_EXECUTE_HANDLER)
        {
            OutputDebugStringW(L"[PatternScanning] Exception during scan\n");
        }

        return 0;
    }

    /// <summary>
    /// Simple pattern scan (no mask) - all bytes must match exactly
    /// </summary>
    static uintptr_t ScanMemorySimple(
        const uint8_t* pattern,
        int patternLen,
        uintptr_t startAddr,
        size_t size)
    {
        if (!pattern || patternLen == 0 || !startAddr)
            return 0;

        __try
        {
            for (uintptr_t addr = startAddr; addr < startAddr + size - patternLen; addr++)
            {
                if (memcmp((void*)addr, pattern, patternLen) == 0)
                    return addr;
            }
        }
        __except (EXCEPTION_EXECUTE_HANDLER)
        {
        }

        return 0;
    }
};

// ============================================================================
// ROBLOX-SPECIFIC SCANNING - Find Lua execution functions
// ============================================================================

class RobloxLuaScanner
{
public:
    /// <summary>
    /// Get the base address and size of a loaded module
    /// </summary>
    static bool GetModuleInfo(const wchar_t* moduleName, uintptr_t& outBase, size_t& outSize)
    {
        __try
        {
            HMODULE hMod = GetModuleHandleW(moduleName);
            if (!hMod)
                return false;

            outBase = (uintptr_t)hMod;

            // Get size from PE header
            uint32_t* pDosHeader = (uint32_t*)outBase;
            uint32_t peOffset = pDosHeader[0x3C / 4];
            if (peOffset == 0 || peOffset > 0x10000)
                return false;

            uint32_t* pSizeOfImage = (uint32_t*)(outBase + peOffset + 0x38);
            outSize = *pSizeOfImage;

            return outSize > 0 && outSize < 0x200000000;  // Sanity check
        }
        __except (EXCEPTION_EXECUTE_HANDLER)
        {
            return false;
        }
    }

    /// <summary>
    /// Scan for Lua state pointer - PLACEHOLDER PATTERNS (see update instructions)
    /// </summary>
    static uintptr_t FindLuaState()
    {
        uintptr_t robloxBase = 0;
        size_t robloxSize = 0;

        if (!GetModuleInfo(L"RobloxPlayerBeta.exe", robloxBase, robloxSize))
        {
            OutputDebugStringW(L"[LuaScanner] RobloxPlayerBeta not found\n");
            return 0;
        }

        // Pattern: mov rcx, [rip + displacement]; test rcx, rcx
        // Bytes: 48 8B 0D ?? ?? ?? ?? 48 85 C9
        // Mask:  FF FF FF 00 00 00 00 FF FF FF
        // PLACEHOLDER - Update these values for your Roblox version!
        
        uint8_t patternLuaState[] = {
            0x48, 0x8B, 0x0D,           // mov rcx, [rip + offset]
            0x00, 0x00, 0x00, 0x00,     // (4-byte displacement, varies per version)
            0x48, 0x85, 0xC9            // test rcx, rcx
        };

        uint8_t maskLuaState[] = {
            0xFF, 0xFF, 0xFF,           // These bytes must match exactly
            0x00, 0x00, 0x00, 0x00,     // These bytes are wildcards
            0xFF, 0xFF, 0xFF            // These bytes must match exactly
        };

        uintptr_t found = PatternScanner::ScanMemory(
            patternLuaState, maskLuaState, robloxBase, robloxSize);

        if (!found)
        {
            OutputDebugStringW(L"[LuaScanner] Lua state pattern not found\n");
            return 0;
        }

        // Extract RIP-relative offset and calculate actual address
        int32_t displacement = *(int32_t*)(found + 3);
        uintptr_t luaStateAddr = found + 7 + displacement;

        return luaStateAddr;
    }

    /// <summary>
    /// Find lua_pcall (Lua script execution function)
    /// PLACEHOLDER - Update for your Roblox version
    /// </summary>
    static uintptr_t FindLuaPcall()
    {
        uintptr_t robloxBase = 0;
        size_t robloxSize = 0;

        if (!GetModuleInfo(L"RobloxPlayerBeta.exe", robloxBase, robloxSize))
            return 0;

        uint8_t patternLuaPcall[] = {
            0x48, 0x8B, 0x01,           // mov rax, [rcx]
            0x48, 0x89, 0x5C, 0x24      // mov [rsp], rbx
        };

        return PatternScanner::ScanMemorySimple(
            patternLuaPcall, sizeof(patternLuaPcall), robloxBase, robloxSize);
    }

    /// <summary>
    /// Find luaL_loadstring (Lua script loading function)
    /// PLACEHOLDER - Update for your Roblox version
    /// </summary>
    static uintptr_t FindLuaLLoadstring()
    {
        uintptr_t robloxBase = 0;
        size_t robloxSize = 0;

        if (!GetModuleInfo(L"RobloxPlayerBeta.exe", robloxBase, robloxSize))
            return 0;

        uint8_t patternLoadString[] = {
            0x80, 0x3C, 0x08, 0x3D      // cmp byte [rax+rcx], '='
        };

        return PatternScanner::ScanMemorySimple(
            patternLoadString, sizeof(patternLoadString), robloxBase, robloxSize);
    }
};

// ============================================================================
// HOW TO UPDATE PATTERNS WHEN ROBLOX UPDATES
// ============================================================================
//
// When Roblox updates and scripts stop working:
//
// 1. Download x64dbg from: https://x64dbg.com/
// 2. Attach to RobloxPlayerBeta.exe (must run as admin)
// 3. Ctrl+G → type "RobloxPlayerBeta" to go to module base
// 4. Ctrl+B → search for strings: "mov rcx, [rip" or other patterns
// 5. Find function addresses in disassembly
// 6. Calculate byte patterns from the instructions
// 7. Update the pattern arrays above with new byte sequences
// 8. Recompile with Visual Studio: build_dll.bat
//
// Example: If you find "mov rcx, [rip+0x12345]" at address 0x400000:
//   - Bytes: 48 8B 0D 34 12 00 00
//   - Pattern: {0x48, 0x8B, 0x0D, 0x00, 0x00, 0x00, 0x00, ...}
//   - Mask:    {0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, ...}
//