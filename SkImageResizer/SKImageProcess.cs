using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SkiaSharp;

namespace SkImageResizer
{
    public class SKImageProcess
    {
        /// <summary>
        /// 進行圖片的縮放作業
        /// </summary>
        /// <param name="sourcePath">圖片來源目錄路徑</param>
        /// <param name="destPath">產生圖片目的目錄路徑</param>
        /// <param name="scale">縮放比例</param>
        public void ResizeImages(string sourcePath, string destPath, double scale)
        {
            if (!Directory.Exists(destPath))
            {
                Directory.CreateDirectory(destPath);
            }

            var allFiles = FindImages(sourcePath);
            foreach (var filePath in allFiles)
            {
                var bitmap = SKBitmap.Decode(filePath);
                var imgPhoto = SKImage.FromBitmap(bitmap);
                var imgName = Path.GetFileNameWithoutExtension(filePath);

                var sourceWidth = imgPhoto.Width;
                var sourceHeight = imgPhoto.Height;

                var destinationWidth = (int)(sourceWidth * scale);
                var destinationHeight = (int)(sourceHeight * scale);

                using var scaledBitmap = bitmap.Resize(
                    new SKImageInfo(destinationWidth, destinationHeight),
                    SKFilterQuality.High);
                using var scaledImage = SKImage.FromBitmap(scaledBitmap);
                using var data = scaledImage.Encode(SKEncodedImageFormat.Jpeg, 100);
                using var s = File.OpenWrite(Path.Combine(destPath, imgName + ".jpg"));
                data.SaveTo(s);
            }
        }

        public async Task ResizeImagesAsync(string sourcePath, string destPath, double scale)
        {
            if (!Directory.Exists(destPath))
            {
                Directory.CreateDirectory(destPath);
            }

            var allFiles = FindImages(sourcePath);

            await Task.Run(() => Parallel.ForEach(allFiles, async filePath => await SetResizeImageAsync(filePath, destPath, scale)));
        }

        private async Task SetResizeImageAsync(string filePath, string destPath, double scale)
        {
            var imgName = Path.GetFileNameWithoutExtension(filePath);
            var bitmap = SKBitmap.Decode(filePath);

            using var scaledBitmap = await GetscaledBitmapAsync(bitmap, scale);
            using var scaledImage = await GetScaledImageAsync(scaledBitmap);
            using var data = await GetDataAsync(scaledImage);

            using var s = File.OpenWrite(Path.Combine(destPath, $"{imgName}.jpg"));
            await SaveDataAsync(data, s);
        }

        private async Task<SKBitmap> GetscaledBitmapAsync(SKBitmap bitmap, double scale)
        {
            var imgPhoto = SKImage.FromBitmap(bitmap);

            var sourceWidth = imgPhoto.Width;
            var sourceHeight = imgPhoto.Height;

            var destinationWidth = (int)(sourceWidth * scale);
            var destinationHeight = (int)(sourceHeight * scale);

            return await Task.Run(() => bitmap.Resize(
                new SKImageInfo(destinationWidth, destinationHeight),
                SKFilterQuality.High));
        }
        private async Task<SKImage> GetScaledImageAsync(SKBitmap scaledBitmap) => await Task.Run(() => SKImage.FromBitmap(scaledBitmap));
        private async Task<SKData> GetDataAsync(SKImage scaledImage) => await Task.Run(() => scaledImage.Encode(SKEncodedImageFormat.Jpeg, 100));
        private async Task SaveDataAsync(SKData data, FileStream fs) => await Task.Run(() => data.SaveTo(fs));


        /// <summary>
        /// 清空目的目錄下的所有檔案與目錄
        /// </summary>
        /// <param name="destPath">目錄路徑</param>
        public void Clean(string destPath)
        {
            if (!Directory.Exists(destPath))
            {
                Directory.CreateDirectory(destPath);
            }
            else
            {
                var allImageFiles = Directory.GetFiles(destPath, "*", SearchOption.AllDirectories);

                foreach (var item in allImageFiles)
                {
                    File.Delete(item);
                }
            }
        }

        /// <summary>
        /// 找出指定目錄下的圖片
        /// </summary>
        /// <param name="srcPath">圖片來源目錄路徑</param>
        /// <returns></returns>
        public List<string> FindImages(string srcPath)
        {
            List<string> files = new List<string>();
            files.AddRange(Directory.GetFiles(srcPath, "*.png", SearchOption.AllDirectories));
            files.AddRange(Directory.GetFiles(srcPath, "*.jpg", SearchOption.AllDirectories));
            files.AddRange(Directory.GetFiles(srcPath, "*.jpeg", SearchOption.AllDirectories));
            return files;
        }
    }
}