#pragma once

#include <Windows.h>
#include <cstdint>
#include <vector>
#include <cstring>

// ============================================================================
// PATTERN SCANNING MODULE
// Finds functions and data within the Roblox process using byte patterns
// ============================================================================

/// <summary>
/// Represents a memory pattern to search for.
/// Uses wildcards (0xFF) to skip bytes.
/// </summary>
class PatternScanner
{
public:
    /// <summary>
    /// Find a pattern within a memory range.
    /// </summary>
    /// <param name="pattern">Byte pattern (0xFF = wildcard)</param>
    /// <param name="mask">Mask indicating which bytes matter (0x00 = wildcard, 0xFF = must match)</param>
    /// <param name="rangeStart">Start address to search</param>
    /// <param name="rangeSize">Size of range to search</param>
    /// <returns>Address of match, or NULL if not found</returns>
    static uintptr_t FindPattern(
        const uint8_t* pattern,
        const uint8_t* mask,
        uintptr_t rangeStart,
        size_t rangeSize)
    {
        if (!pattern || !mask || !rangeStart || rangeSize == 0)
            return 0;

        __try
        {
            // Get pattern length
            int patternLen = 0;
            while (mask[patternLen] != 0x00 && patternLen < 1024)
                patternLen++;

            if (patternLen == 0)
                return 0;

            // Search within range
            for (uintptr_t i = 0; i < rangeSize - patternLen; i++)
            {
                uintptr_t addr = rangeStart + i;
                uint8_t* bytes = (uint8_t*)addr;

                bool match = true;
                for (int j = 0; j < patternLen; j++)
                {
                    if (mask[j] == 0xFF && bytes[j] != pattern[j])
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
            // Invalid memory access, return 0
            return 0;
        }

        return 0;
    }

    /// <summary>
    /// Find a pattern using a string notation.
    /// Example: "48 8B C4 ?? 55 56" (? = wildcard)
    /// </summary>
    static uintptr_t FindPatternString(
        const char* patternStr,
        uintptr_t rangeStart,
        size_t rangeSize)
    {
        std::vector<uint8_t> pattern;
        std::vector<uint8_t> mask;

        // Parse pattern string
        const char* p = patternStr;
        while (*p)
        {
            // Skip whitespace
            while (*p && isspace(*p))
                p++;

            if (!*p)
                break;

            if (*p == '?')
            {
                // Wildcard
                pattern.push_back(0x00);
                mask.push_back(0x00);
                p++;
            }
            else
            {
                // Parse hex byte
                char* end = nullptr;
                int byte = strtol(p, &end, 16);
                if (end == p)
                    break;  // Parse error

                pattern.push_back((uint8_t)byte);
                mask.push_back(0xFF);
                p = end;
            }

            // Skip whitespace
            while (*p && isspace(*p))
                p++;
        }

        if (pattern.empty())
            return 0;

        return FindPattern(pattern.data(), mask.data(), rangeStart, rangeSize);
    }
};

// ============================================================================
// ROBLOX-SPECIFIC SCANNING
// Patterns and functions to find Roblox's Lua implementation
// ============================================================================

/// <summary>
/// Scan for Roblox's Lua state.
/// This is a simplified finder - actual implementation depends on Roblox version.
/// </summary>
class RobloxScanner
{
public:
    /// <summary>
    /// Get the base address of a module loaded in the process.
    /// </summary>
    static uintptr_t GetModuleBaseAddress(const wchar_t* moduleName)
    {
        __try
        {
            HMODULE hModule = GetModuleHandleW(moduleName);
            if (hModule)
                return (uintptr_t)hModule;
        }
        __except (EXCEPTION_EXECUTE_HANDLER)
        {
        }

        return 0;
    }

    /// <summary>
    /// Get the size of a module in memory.
    /// </summary>
    static size_t GetModuleSize(uintptr_t baseAddress)
    {
        __try
        {
            HMODULE hModule = (HMODULE)baseAddress;

            // Get PE header
            uint32_t* pPE = (uint32_t*)(baseAddress + 0x3C);
            uint32_t peOffset = *pPE;

            // Get size of image from PE headers
            uint32_t* pSizeOfImage = (uint32_t*)(baseAddress + peOffset + 0x38);
            return *pSizeOfImage;
        }
        __except (EXCEPTION_EXECUTE_HANDLER)
        {
            return 0;
        }
    }

    /// <summary>
    /// Scan RobloxPlayerBeta.exe for Lua functions.
    /// NOTE: These patterns may need updating when Roblox updates!
    /// </summary>
    static bool FindLuaFunctions(
        uintptr_t& outLuaState,
        uintptr_t& outLuaLoadString,
        uintptr_t& outLuaPcall)
    {
        __try
        {
            // Get RobloxPlayerBeta.exe base
            uintptr_t robloxBase = GetModuleBaseAddress(L"RobloxPlayerBeta.exe");
            if (!robloxBase)
                return false;

            size_t robloxSize = GetModuleSize(robloxBase);
            if (!robloxSize)
                return false;

            // Pattern 1: Find Lua state global pointer
            // This is a simplified pattern - actual patterns are more complex
            uint8_t patternLuaState[] = {
                0x48, 0x8B, 0x0D, 0x00, 0x00, 0x00, 0x00,  // mov rcx, [rip+...]
                0x48, 0x85, 0xC9,                           // test rcx, rcx
                0x74, 0x00                                  // jz ...
            };
            uint8_t maskLuaState[] = {
                0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00,
                0xFF, 0xFF, 0xFF,
                0xFF, 0x00
            };

            outLuaState = PatternScanner::FindPattern(
                patternLuaState, maskLuaState, robloxBase, robloxSize);

            // If pattern didn't work, return false
            // In a real implementation, you'd have multiple patterns
            if (!outLuaState)
                return false;

            // Extract the RIP-relative address
            // mov rcx, [rip + displacement]
            // RIP points to next instruction (3 bytes from pattern start)
            int32_t displacement = *(int32_t*)(outLuaState + 3);
            outLuaState = outLuaState + 7 + displacement;  // 7 = size of mov instruction

            // TODO: Find luaL_loadstring and lua_pcall
            // These would use similar pattern matching

            return true;
        }
        __except (EXCEPTION_EXECUTE_HANDLER)
        {
            return false;
        }
    }
};

// ============================================================================
// HELPER FUNCTIONS FOR FINDING COMMON ROBLOX PATTERNS
// ============================================================================

/// <summary>
/// Find the Lua state pointer.
/// This is called once at DLL initialization.
/// </summary>
inline uintptr_t FindLuaState()
{
    uintptr_t luaState = 0;
    uintptr_t luaLoadString = 0;
    uintptr_t luaPcall = 0;

    if (RobloxScanner::FindLuaFunctions(luaState, luaLoadString, luaPcall))
    {
        return luaState;
    }

    return 0;
}

/// <summary>
/// Following is a note about updating patterns when Roblox updates:
/// 
/// WHEN ROBLOX UPDATES:
/// 1. Run the executor and note if pattern finding fails
/// 2. Use a disassembler (IDA Pro, Ghidra, or x64dbg) to:
///    - Search for "luaL_loadstring" or similar function names
///    - Look for characteristic byte sequences near Lua functions
/// 3. Update the patterns in RobloxScanner::FindLuaFunctions()
/// 4. Rebuild the DLL
/// 
/// PATTERN FINDING TIPS:
/// - Look for function prologue: 48 89 5C 24 or 48 83 EC
/// - Search for string comparisons with Lua error messages
/// - Look for consecutive mov/call instructions typical of API calls
/// 
/// ALTERNATIVE APPROACH:
/// - Query Lua directly using lua_getglobal, lua_pcall, etc.
/// - Use the Lua API to call back to script
/// - This requires finding just the initial Lua state pointer
/// </summary>

#endif // PATTERN_SCANNING_HPP
