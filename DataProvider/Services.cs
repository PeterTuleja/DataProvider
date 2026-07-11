using System.Text.RegularExpressions;

namespace DataProvider
{
    internal static class Services
    {
        internal static bool IsFolderWritable(string folderPath)
        {
            try
            {
                using (FileStream fs = File.Create(Path.Combine(folderPath, $"Omega_IsFolderWritable{Path.GetRandomFileName()}"), 1,
                    FileOptions.DeleteOnClose))
                {
                }
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }


        internal static string INIPolozka(this string s, int primaryKey, string defaultHodnota = "", char endChar = '~', string primaryKeyStr = "")
        {
            if (s == null)
            {
                return defaultHodnota;
            }

            string pattern;
            if (string.IsNullOrEmpty(primaryKeyStr))
            {
                pattern = string.Format(@"\[{0:00}\]([^{1}]*)({1}|$)", primaryKey, Regex.Escape(endChar.ToString()));
            }
            else
            {
                pattern = string.Format(@"\[{0}\]([^{1}]*)({1}|$)", Regex.Escape(primaryKeyStr), Regex.Escape(endChar.ToString()));
            }

            Match m = Regex.Match(s, pattern);
            if (m.Success)
            {
                return m.Groups[1].Value;
            }

            return defaultHodnota;
        }
    }
}
