using System.Runtime.InteropServices;
using Microsoft.Maui.Storage;
using QuestPDF.Infrastructure;

namespace Reflecta.Services;

public static class QuestPdfBootstrapper
{
    private static bool _initialized;
    private static Exception? _initException;

    public static void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        if (_initException != null)
        {
            throw new InvalidOperationException("QuestPDF initialization previously failed.", _initException);
        }

        try
        {
#if MACCATALYST
            LoadMacCatalystNativeLibrary();
#endif
            QuestPDF.Settings.License = LicenseType.Community;
            _initialized = true;
        }
        catch (Exception ex)
        {
            _initException = ex;
            throw;
        }
    }

#if MACCATALYST
    private static void LoadMacCatalystNativeLibrary()
    {
        var nativeAssetName = RuntimeInformation.ProcessArchitecture == Architecture.Arm64
            ? "libQuestPdfSkia-arm64.dylib"
            : "libQuestPdfSkia-x64.dylib";

        var targetPath = Path.Combine(FileSystem.AppDataDirectory, "libQuestPdfSkia.dylib");

        if (!File.Exists(targetPath))
        {
            using var stream = FileSystem.OpenAppPackageFileAsync(nativeAssetName).GetAwaiter().GetResult();
            using var fileStream = File.Create(targetPath);
            stream.CopyTo(fileStream);
        }

        var handle = NativeLibrary.Load(targetPath);
        NativeLibrary.SetDllImportResolver(typeof(QuestPDF.Settings).Assembly, (libraryName, assembly, searchPath) =>
        {
            if (libraryName == "QuestPdfSkia" || libraryName == "libQuestPdfSkia")
            {
                return handle;
            }
            return IntPtr.Zero;
        });
    }
#endif
}
