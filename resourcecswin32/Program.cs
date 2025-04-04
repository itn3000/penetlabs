// See https://aka.ms/new-console-template for more information
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;

CsWin32Test(args[0]);

void CsWin32Test(string fileName)
{
    using var safeHandle = PInvoke.LoadLibrary(fileName);
    PInvoke.EnumResourceTypes(safeHandle, new Windows.Win32.System.LibraryLoader.ENUMRESTYPEPROCW(EnumResourceTypes), 0);
    PInvoke.EnumResourceNames(safeHandle, "BINARY", new Windows.Win32.System.LibraryLoader.ENUMRESNAMEPROCW(EnumResources), IntPtr.Zero);
    char[] localeReturn = new char[16];
    //var ret = PInvoke.GetLocaleInfoEx(PInvoke.LOCALE_NAME_SYSTEM_DEFAULT, PInvoke.LOCALE_ILANGUAGE, localeReturn);
    var ret = PInvoke.GetLocaleInfoEx("ja-JP", PInvoke.LOCALE_ILANGUAGE, localeReturn);
    if (ret != 0)
    {
        var locid = ushort.Parse(localeReturn, System.Globalization.NumberStyles.HexNumber);
        Console.WriteLine($"{ret},{new string(localeReturn)},{locid}");
        unsafe 
        {
            string resourceType = "BINARY";
            fixed (char* rtp = resourceType)
            {
                var hrsrc = PInvoke.FindResourceEx((HMODULE)safeHandle.DangerousGetHandle(), new PCWSTR(rtp), IntResourceHelper.MakeIntPcwstrResource(103), locid);
                if (hrsrc == IntPtr.Zero)
                {
                    var lasterr = Marshal.GetLastWin32Error();
                    throw new Exception($"failed to find resource({lasterr})");
                }
                using var rsrc = PInvoke.LoadResource(safeHandle, hrsrc);
                unsafe
                {
                    void* data = PInvoke.LockResource(rsrc);
                    if (data == null)
                    {
                        var lasterr = Marshal.GetLastWin32Error();
                        throw new Exception($"failed to lock resource({lasterr})");
                    }
                    var resourceLength = PInvoke.SizeofResource(safeHandle, hrsrc);
                    Span<byte> dp = new Span<byte>((byte *)data, (int)resourceLength);
                    Console.WriteLine($"resourcelength = {resourceLength}");
                }
            }
        }
    }
    else
    {
        var lasterr = Marshal.GetLastWin32Error();
        Console.WriteLine($"failed to getlocalinfo: {lasterr}");
    }
    BOOL EnumResources(HMODULE hMODULE, PCWSTR lpType, PWSTR lpName, nint ptr)
    {
        Console.WriteLine($"res = {IntResourceHelper.GetResourceIdOrName(lpType)}: {IntResourceHelper.GetResourceIdOrName(lpName)}");
        return true;
    }
    BOOL EnumResourceTypes(HMODULE hModule, PWSTR typeName, nint lparam)
    {
        Console.WriteLine($"typename = {IntResourceHelper.GetResourceIdOrName(typeName)}");
        return true;
    }
}

static class IntResourceHelper
{
    public static string GetResourceIdOrName(PWSTR lpName)
    {
        unsafe
        {
            var addr = new IntPtr(lpName.Value);
            if(IsIntResource(addr))
            {
                return addr.ToString();
            }
            else
            {
                return new string(lpName.AsSpan());
            }
        }
    }
    public static string GetResourceIdOrName(PCWSTR lpName)
    {
        unsafe
        {
            var addr = new IntPtr(lpName.Value);
            if (IsIntResource(addr))
            {
                return addr.ToString();
            }
            else
            {
                return new string(lpName.AsSpan());
            }
        }
    }
    public static bool IsIntResource(IntPtr ptr)
    {
        return IsIntResource((UIntPtr)ptr);
    }
    public static bool IsIntResource(UIntPtr ptr)
    {
        return (((UIntPtr.MaxValue) << 16) & ptr) == 0;
    }
    public static PWSTR MakeIntPwstrResource(IntPtr ptr)
    {
        return new PWSTR(ptr);
    }
    public static PCWSTR MakeIntPcwstrResource(IntPtr ptr)
    {
        unsafe
        {
            return new PCWSTR((char*)ptr);
        }
    }
}
