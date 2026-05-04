namespace Business.Common
{
    public static class ExtensionBL
    {
        public static string ToChineseDayOfWeek(this DateTime date)
        {
            return date.DayOfWeek switch
            {
                DayOfWeek.Monday => "一",
                DayOfWeek.Tuesday => "二",
                DayOfWeek.Wednesday => "三",
                DayOfWeek.Thursday => "四",
                DayOfWeek.Friday => "五",
                DayOfWeek.Saturday => "六",
                DayOfWeek.Sunday => "日",
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
