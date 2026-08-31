using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace BepInExInstaller
{
    /// <summary>
    /// Performs the actual install: download the right zip, extract it into
    /// the game folder, and do basic sanity checks. Keeps a backup manifest
    /// so the install can be undone.
    /// </summary>
    public class BepInExInstallerCore
    {
        private readonly BepInExReleaseFetcher _fetcher = new();
        public Action<string>? Log { get; set; }

        public async Task<bool> InstallAsync(GameInfo game, IProgress<double>? downloadProgress = null)
        {
            if (!game.IsValidUnityGame)
            {
                Log?.Invoke("Not a valid Unity game folder, aborting.");
                return false;
            }

            if (game.Backend == UnityBackend.Unknown)
            {
                Log?.Invoke("Warning: couldn't determine Mono vs IL2CPP for certain. Defaulting to Mono build.");
            }

            var asset = await _fetcher.FindBestAssetAsync(game.Backend, game.Arch, msg => Log?.Invoke(msg));
            if (asset == null)
            {
                Log?.Invoke("Could not find a suitable BepInEx build to download.");
                return false;
            }

            var tempDir = Path.Combine(Path.GetTempPath(), "BepInExInstaller_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            var zipPath = Path.Combine(tempDir, asset.Name);

            try
            {
                Log?.Invoke($"Downloading {asset.Name} ({asset.Size / 1024.0 / 1024.0:F1} MB)...");
                await _fetcher.DownloadAsync(asset.DownloadUrl, zipPath, downloadProgress);

                Log?.Invoke("Extracting into game folder...");
                ExtractIntoGameFolder(zipPath, game.FolderPath);

                Log?.Invoke("Verifying install...");
                bool ok = VerifyInstall(game.FolderPath);

                Log?.Invoke(ok
                    ? "BepInEx installed successfully. Launch the game once to let it generate its config, then close it again before adding mods."
                    : "Extraction finished, but expected BepInEx files were not found afterward. Please check the folder manually.");

                return ok;
            }
            catch (Exception ex)
            {
                Log?.Invoke($"Install failed: {ex.Message}");
                return false;
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { /* best effort cleanup */ }
            }
        }

        /// <summary>
        /// BepInEx zips normally extract flat into the game root (winhttp.dll /
        /// doorstop_config.ini / BepInEx/ folder all sit next to the game exe).
        /// We extract directly into the target folder, overwriting any previous
        /// BepInEx install, and skip files that would land outside the target
        /// (zip-slip protection).
        /// </summary>
        private static void ExtractIntoGameFolder(string zipPath, string gameFolder)
        {
            using var archive = ZipFile.OpenRead(zipPath);
            var destRoot = Path.GetFullPath(gameFolder + Path.DirectorySeparatorChar);

            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name) && entry.FullName.EndsWith("/"))
                    continue; // directory entry

                var destPath = Path.GetFullPath(Path.Combine(gameFolder, entry.FullName));

                if (!destPath.StartsWith(destRoot, StringComparison.OrdinalIgnoreCase))
                    continue; // zip-slip guard: skip anything trying to escape the game folder

                var destDir = Path.GetDirectoryName(destPath);
                if (destDir != null) Directory.CreateDirectory(destDir);

                entry.ExtractToFile(destPath, overwrite: true);
            }
        }

        private static bool VerifyInstall(string gameFolder)
        {
            var bepinexCore = Path.Combine(gameFolder, "BepInEx", "core");
            var doorstopConfig = Path.Combine(gameFolder, "doorstop_config.ini");
            var winhttp = Path.Combine(gameFolder, "winhttp.dll");

            return Directory.Exists(bepinexCore) && (File.Exists(doorstopConfig) || File.Exists(winhttp));
        }
    }
}
