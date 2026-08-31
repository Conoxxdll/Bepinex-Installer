using System;
using System.IO;
using System.Linq;

namespace BepInExInstaller
{
    public enum UnityBackend
    {
        Unknown,
        Mono,
        IL2CPP
    }

    public enum Architecture
    {
        Unknown,
        x86,
        x64
    }

    public class GameInfo
    {
        public string FolderPath { get; set; } = string.Empty;
        public string? ExecutablePath { get; set; }
        public string ExecutableName { get; set; } = string.Empty;
        public string DataFolderName { get; set; } = string.Empty;
        public UnityBackend Backend { get; set; } = UnityBackend.Unknown;
        public Architecture Arch { get; set; } = Architecture.Unknown;
        public bool IsValidUnityGame { get; set; }
        public string? Error { get; set; }
    }

    /// <summary>
    /// Inspects a folder to figure out whether it's a Unity game, and if so,
    /// whether it uses the Mono or IL2CPP scripting backend, and whether the
    /// game executable is 32-bit or 64-bit. This determines which BepInEx
    /// package needs to be installed.
    /// </summary>
    public static class GameDetector
    {
        public static GameInfo Detect(string folderPath)
        {
            var info = new GameInfo { FolderPath = folderPath };

            if (!Directory.Exists(folderPath))
            {
                info.Error = "That folder does not exist.";
                return info;
            }

            // Find the game's main executable: any .exe in the root folder that
            // has a matching "<name>_Data" folder next to it (the Unity convention).
            var exeCandidates = Directory.GetFiles(folderPath, "*.exe", SearchOption.TopDirectoryOnly);

            string? matchedExe = null;
            string? matchedDataFolder = null;

            foreach (var exe in exeCandidates)
            {
                var exeName = Path.GetFileNameWithoutExtension(exe);
                var dataFolder = Path.Combine(folderPath, exeName + "_Data");
                if (Directory.Exists(dataFolder))
                {
                    matchedExe = exe;
                    matchedDataFolder = dataFolder;
                    break;
                }
            }

            if (matchedExe == null)
            {
                info.Error = "Couldn't find a Unity game executable in this folder " +
                              "(expected a <GameName>.exe next to a <GameName>_Data folder).";
                return info;
            }

            info.ExecutablePath = matchedExe;
            info.ExecutableName = Path.GetFileName(matchedExe);
            info.DataFolderName = Path.GetFileName(matchedDataFolder!);
            info.IsValidUnityGame = true;

            // IL2CPP games ship GameAssembly.dll in the root folder and do NOT have
            // a Managed folder with game-code DLLs (Mono games do).
            var gameAssemblyDll = Path.Combine(folderPath, "GameAssembly.dll");
            var managedFolder = Path.Combine(matchedDataFolder!, "Managed");

            if (File.Exists(gameAssemblyDll))
            {
                info.Backend = UnityBackend.IL2CPP;
            }
            else if (Directory.Exists(managedFolder) &&
                     Directory.GetFiles(managedFolder, "Assembly-CSharp.dll", SearchOption.TopDirectoryOnly).Any())
            {
                info.Backend = UnityBackend.Mono;
            }
            else
            {
                info.Backend = UnityBackend.Unknown;
            }

            info.Arch = DetectArchitecture(matchedExe);

            return info;
        }

        /// <summary>
        /// Reads the PE header of the executable to determine if it's x86 or x64.
        /// This is far more reliable than guessing from folder names.
        /// </summary>
        private static Architecture DetectArchitecture(string exePath)
        {
            try
            {
                using var fs = new FileStream(exePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var br = new BinaryReader(fs);

                // DOS header: e_lfanew is at offset 0x3C, points to the PE header
                fs.Seek(0x3C, SeekOrigin.Begin);
                int peHeaderOffset = br.ReadInt32();

                fs.Seek(peHeaderOffset, SeekOrigin.Begin);
                uint peSignature = br.ReadUInt32(); // "PE\0\0"
                if (peSignature != 0x00004550)
                    return Architecture.Unknown;

                ushort machine = br.ReadUInt16();

                return machine switch
                {
                    0x8664 => Architecture.x64, // IMAGE_FILE_MACHINE_AMD64
                    0x014c => Architecture.x86, // IMAGE_FILE_MACHINE_I386
                    0xAA64 => Architecture.x64, // ARM64 - treat as x64 pkg (BepInEx doesn't ship arm64 win builds; falls back)
                    _ => Architecture.Unknown
                };
            }
            catch
            {
                return Architecture.Unknown;
            }
        }
    }
}
