using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{
    internal static class AssetCache
    {
        private static readonly object syncRoot = new object();
        private static readonly Dictionary<string, Bitmap> images = new Dictionary<string, Bitmap>(StringComparer.OrdinalIgnoreCase);

        private static Task preloadTask;
        private static string currentAssetsFolder;

        public static Task PreloadAllAsync(string assetsFolder, Action<AssetPreloadProgress> progressCallback = null)
        {
            if (string.IsNullOrWhiteSpace(assetsFolder))
                return Task.CompletedTask;

            lock (syncRoot)
            {
                if (preloadTask == null || !string.Equals(currentAssetsFolder, assetsFolder, StringComparison.OrdinalIgnoreCase))
                {
                    currentAssetsFolder = assetsFolder;
                    preloadTask = Task.Run(() => PreloadInternal(assetsFolder, progressCallback));
                }
                else if (progressCallback != null && preloadTask.IsCompleted)
                {
                    progressCallback(new AssetPreloadProgress(images.Count, images.Count, string.Empty));
                }

                return preloadTask;
            }
        }

        public static Image GetOrLoad(string assetsFolder, string fileName)
        {
            if (string.IsNullOrWhiteSpace(assetsFolder) || string.IsNullOrWhiteSpace(fileName))
                return null;

            string path = Path.Combine(assetsFolder, fileName);
            return GetOrLoad(path);
        }

        public static Image GetOrLoad(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return null;

            lock (syncRoot)
            {
                if (images.TryGetValue(path, out Bitmap cachedImage))
                    return cachedImage;

                Bitmap loadedImage = LoadBitmap(path);
                images[path] = loadedImage;
                return loadedImage;
            }
        }

        public static void DisposeAll()
        {
            lock (syncRoot)
            {
                foreach (Bitmap image in images.Values)
                {
                    image.Dispose();
                }

                images.Clear();
                preloadTask = null;
                currentAssetsFolder = null;
            }
        }

        private static void PreloadInternal(string assetsFolder, Action<AssetPreloadProgress> progressCallback)
        {
            if (!Directory.Exists(assetsFolder))
                return;

            string[] files = Directory.GetFiles(assetsFolder)
                .Where(path =>
                {
                    string extension = Path.GetExtension(path);
                    return string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase);
                })
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            int total = files.Length;
            int loaded = 0;

            foreach (string file in files)
            {
                GetOrLoad(file);
                loaded++;
                progressCallback?.Invoke(new AssetPreloadProgress(loaded, total, Path.GetFileName(file)));
            }

            progressCallback?.Invoke(new AssetPreloadProgress(total, total, string.Empty));
        }

        private static Bitmap LoadBitmap(string path)
        {
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (Image source = Image.FromStream(stream))
            {
                return new Bitmap(source);
            }
        }
    }

    internal sealed class AssetPreloadProgress
    {
        public AssetPreloadProgress(int loaded, int total, string currentFile)
        {
            Loaded = loaded;
            Total = total;
            CurrentFile = currentFile ?? string.Empty;
        }

        public int Loaded { get; }
        public int Total { get; }
        public string CurrentFile { get; }
        public int Percentage => Total <= 0 ? 100 : (int)Math.Round((double)Loaded * 100d / Total);
    }
}
