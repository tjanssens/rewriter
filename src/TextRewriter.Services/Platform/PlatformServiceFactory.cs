using System.Runtime.InteropServices;
using TextRewriter.Core.Interfaces;

namespace TextRewriter.Services.Platform;

public static class PlatformServiceFactory
{
    public static IPlatformService Create()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return new MacPlatformService();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new WindowsPlatformService();
        return new LinuxPlatformService();
    }
}
