using System;
using System.IO;
using System.Reflection;

namespace Plex.Helpers
{
    public static class PathExt
    {
        public static string GetDirectoryName(this Assembly assembly)
        {
            return Path.GetDirectoryName(assembly.CodeBase.Replace("file:///", ""));
        }



        /// <summary>
        /// Возвращает расширение файла подобно функции <see cref="Path.GetExtension"/> (с точкой в начале).
        /// Разбирает сложные расширения архивов типа .bz2 и .bzip2.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetFileExtension(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;
            try
            {
                var filename = Path.GetFileName(path);

                var ret = Path.GetExtension(filename);
                if (ret != ".bz2" && ret != ".bzip2")
                    return ret;
                return GetFileExtension(filename.Substring(0, filename.Length - ret.Length)) + ret;
            }
            catch (Exception)
            {
                var i = path.LastIndexOf('.');
                if (i == -1)
                    return "";
                var tmp = "a" + path.Substring(i);
                return Path.GetExtension(tmp);
            }
        }
    }
}
