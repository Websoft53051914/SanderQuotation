namespace Const
{
    /// <summary>
    /// 常用 json 回傳類別
    /// </summary>
    public class CommonJsonResM<T>
        where T : class
    {
        public bool Success { get; set; }

        public string? Message { get; set; }

        public T? Data { get; set; }
    }
}
