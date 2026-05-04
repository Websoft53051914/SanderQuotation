namespace Const
{
    public class LocaleConst
    {
        public const string ZH_TW = "zh-tw";
        public const string EN_US = "en-us";

        public static string GitCommitDate = GetGitCommitDate();//"1.2025.9.8.1416";

        private static string GetGitCommitDate()
        {
            try
            {
                string filePath = Path.Combine(AppContext.BaseDirectory, "gitcommitdate.txt");

                if (File.Exists(filePath))
                {
                    string commitDate = File.ReadAllText(filePath).Trim();
                    var dateStr = DateTime.Parse(commitDate).ToString("1.yyyy.M.d.HHmm");
                    return dateStr;
                }
                else
                {
                    return "";
                    //Console.WriteLine("gitcommitdate.txt 不存在！");
                }
            }
            catch (Exception ex)
            {
                return "";
            }
        }
    }
}
