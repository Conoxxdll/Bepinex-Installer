using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace BepInExInstaller
{
    public class ReleaseAsset
    {
        public string Name { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public long Size { get; set; }
    }

    /// <summary>
    /// Talks to the GitHub API to find the right BepInEx zip for the detected
    /// backend/architecture. Asset naming has changed over BepInEx versions, so
    /// instead of hardcoding a filename we score every asset in the last few
    /// releases and pick the best match. This keeps working even if BepInEx
    /// renames things or ships a new major version.
    /// </summary>
    public class BepInExReleaseFetcher
    {
        private const string Repo = "BepInEx/BepInEx";
        private readonly HttpClient _http;

        public BepInExReleaseFetcher()
        {
            _http = new HttpClient();
            _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("BepInExInstaller", "1.0"));
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        }

        /// <summary>
        /// Finds the best-matching BepInEx zip asset for the given backend + arch,
        /// searching the most recent releases (including pre-releases, since
        /// IL2CPP support has historically lived in "be" pre-release builds).
        /// </summary>
        public async Task<ReleaseAsset?> FindBestAssetAsync(UnityBackend backend, Architecture arch, Action<string>? log = null)
        {
            var url = $"https://api.github.com/repos/{Repo}/releases?per_page=15";
            log?.Invoke("Checking BepInEx releases on GitHub...");

            using var response = await _http.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);

            ReleaseAsset? best = null;
            int bestScore = int.MinValue;
            string? bestReleaseTag = null;

            foreach (var release in doc.RootElement.EnumerateArray())
            {
                var tag = release.TryGetProperty("tag_name", out var tagEl) ? tagEl.GetString() ?? "" : "";

                if (!release.TryGetProperty("assets", out var assets))
                    continue;

                foreach (var asset in assets.EnumerateArray())
                {
                    var name = asset.GetProperty("name").GetString() ?? "";
                    if (!name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                        continue;

                    int score = ScoreAssetName(name, backend, arch);
                    if (score == int.MinValue) continue;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestReleaseTag = tag;
                        best = new ReleaseAsset
                        {
                            Name = name,
                            DownloadUrl = asset.GetProperty("browser_download_url").GetString() ?? "",
                            Size = asset.TryGetProperty("size", out var sizeEl) ? sizeEl.GetInt64() : 0
                        };
                    }
                }
            }

            if (best != null)
                log?.Invoke($"Selected: {best.Name} (release {bestReleaseTag})");
            else
                log?.Invoke("No matching BepInEx build found in recent releases.");

            return best;
        }

        /// <summary>
        /// Higher score = better match. Returns int.MinValue if the asset is
        /// clearly not applicable (wrong OS, wrong backend, etc).
        /// </summary>
        private static int ScoreAssetName(string name, UnityBackend backend, Architecture arch)
        {
            var n = name.ToLowerInvariant();

            // Skip non-Windows builds outright.
            if (n.Contains("linux") || n.Contains("macos") || n.Contains("osx") || n.Contains("unix"))
                return int.MinValue;

            // Skip source/checksum/debug-symbol style assets.
            if (n.Contains(".sha") || n.Contains("source") || n.EndsWith(".pdb.zip"))
                return int.MinValue;

            int score = 0;

            bool mentionsIl2cpp = n.Contains("il2cpp");
            bool mentionsMono = n.Contains("mono");

            if (backend == UnityBackend.IL2CPP)
            {
                if (mentionsIl2cpp) score += 100;
                else if (mentionsMono) return int.MinValue; // explicitly the wrong backend
                else score += 10; // ambiguous/unified build, still plausible
            }
            else // Mono or Unknown -> prefer Mono build, it's the safe/common default
            {
                if (mentionsIl2cpp) return int.MinValue;
                if (mentionsMono) score += 100;
                else score += 20; // classic BepInEx 5.x assets don't say "mono" at all
            }

            bool wantsX64 = arch != Architecture.x86; // default to x64 if unknown
            if (wantsX64 && (n.Contains("x64") || n.Contains("win64") || n.Contains("amd64")))
                score += 50;
            else if (!wantsX64 && (n.Contains("x86") || n.Contains("win32")))
                score += 50;
            else if (n.Contains("x64") && !wantsX64)
                score -= 30;
            else if (n.Contains("x86") && wantsX64)
                score -= 30;

            // Slight preference for stable-looking version numbers over "be"/canary builds
            // when scores would otherwise tie, since those are less likely to break games.
            if (!n.Contains("be.") && !n.Contains("canary")) score += 1;

            return score;
        }

        public async Task DownloadAsync(string url, string destinationPath, IProgress<double>? progress = null)
        {
            using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            await using var contentStream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = new System.IO.FileStream(destinationPath, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None, 8192, true);

            var buffer = new byte[8192];
            long totalRead = 0;
            int read;
            while ((read = await contentStream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, read));
                totalRead += read;
                if (totalBytes > 0)
                    progress?.Report((double)totalRead / totalBytes * 100.0);
            }
        }
    }
}
