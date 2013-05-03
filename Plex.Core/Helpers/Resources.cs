using System;
using System.Reflection;
using System.Text;

namespace Plex.Helpers
{
    public static class Resources
    {
        #region Работа с ресурсами

        public static byte[] ExtractResourceFile(string fileName)
        {
            return ExtractResourceFile(Assembly.GetCallingAssembly(), fileName);
        }

        public static byte[] ExtractResourceFile(Assembly assembly, string fileName)
        {
            var stream = assembly.GetManifestResourceStream(fileName);
            if (stream == null)
                throw new Exception("Не удалось извлечь ресурс " + fileName + " из сборки " + assembly.FullName);

            byte[] buffer;
            try
            {
                buffer = new byte[stream.Length];
                stream.Read(buffer, 0, (int)stream.Length);
            }
            finally
            {
                stream.Close();
            }

            return buffer;
        }

        public static string ExtractResourceTextFile(string fileName, Encoding encoding)
        {
            return ExtractResourceTextFile(Assembly.GetCallingAssembly(), fileName, encoding);
        }

        public static string ExtractResourceTextFile(string fileName)
        {
            try
            {
                return ExtractResourceTextFile(Assembly.GetCallingAssembly(), fileName, Encoding.UTF8);
            }
            catch (Exception e)
            {
                throw new ApplicationException("Не удалось извлечь ресурс " + fileName + " из сборки " + Assembly.GetCallingAssembly().FullName, e);
            }
        }

        public static string ExtractResourceTextFile(Assembly assembly, string fileName, Encoding encoding)
        {
            var data = ExtractResourceFile(assembly, fileName);
            return encoding.GetString(data);
        }

        #endregion

    }
}
