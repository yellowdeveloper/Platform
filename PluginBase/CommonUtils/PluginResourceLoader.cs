using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace PluginBase.CommonUtils
{
    public static class PluginResourceLoader
    {
        private static readonly Dictionary<string, BitmapImage> _imageCache = new Dictionary<string, BitmapImage>();

        public static BitmapImage GetImage(string filename)
        {
            Assembly callingAssembly = Assembly.GetCallingAssembly();

            string cacheKey = $"{callingAssembly.GetName().Name}_{filename}";

            if (_imageCache.ContainsKey(cacheKey))
            {
                return _imageCache[cacheKey];
            }

            string[] resourceName = callingAssembly.GetManifestResourceNames();
            string targetResourceName = resourceName.FirstOrDefault(name => name.EndsWith(filename));

            if (targetResourceName == null)
            {
                DebugLogger.Log(2, $"[WARN] Resource '{filename}' not found in assembly '{callingAssembly.GetName().Name}'.");
                return null;
            }

            using (Stream stream = callingAssembly.GetManifestResourceStream(targetResourceName))
            {
                if (stream != null)
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad; // 메모리 누수 방지
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();

                    bitmap.Freeze(); // 캐싱을 위해 꼭 Freeze 해 주어야 함

                    _imageCache[cacheKey] = bitmap; // 캐시에 저장 후 반환
                    return bitmap;
                }
            }

            return null;
        }
    }
}
