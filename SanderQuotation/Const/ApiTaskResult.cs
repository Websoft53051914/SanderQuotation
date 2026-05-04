namespace Const
{
    public class ApiTaskResult<T>
        where T : class
    {
        public ApiTaskResult(ApiTaskStatusEnum status, string message = "")
        {
            Status = status;
            Message = message;
            Data = null;
        }

        public ApiTaskResult(ApiTaskStatusEnum status, T? data)
        {
            Status = status;
            Data = data;
        }

        /// <summary>
        /// 狀態
        /// </summary>
        public ApiTaskStatusEnum Status { get; set; }

        /// <summary>
        /// 訊息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 資料
        /// </summary>
        public T? Data { get; set; }

        /// <summary>
        /// 是否錯誤
        /// </summary>
        /// <returns></returns>
        public bool IsError()
        {
            return Status == ApiTaskStatusEnum.Error || Status == ApiTaskStatusEnum.Fail;
        }

        public static ApiTaskResult<T> Success() => new(ApiTaskStatusEnum.Success);
        public static ApiTaskResult<T> Success(T? data) => new(ApiTaskStatusEnum.Success, data);
        public static ApiTaskResult<T> Fail(string message) => new(ApiTaskStatusEnum.Fail, message);
        public static ApiTaskResult<T> Error(Exception exception) => new(ApiTaskStatusEnum.Error, exception.ToString());
    }

    /// <summary>
    /// 工作任務狀態
    /// </summary>
    public enum ApiTaskStatusEnum
    {
        /// <summary>
        /// 成功
        /// </summary>
        Success = 1,

        /// <summary>
        /// 失敗
        /// </summary>
        Fail = 2,

        /// <summary>
        /// 錯誤
        /// </summary>
        Error = 9,
    }
}
