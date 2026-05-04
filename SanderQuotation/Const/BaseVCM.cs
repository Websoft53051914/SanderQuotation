namespace Const
{
    public class BaseVCM
    {
        public BaseVCM()
        {
            ViewId = "z" + Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// 元件 ID
        /// </summary>
        public string ViewId { get; set; } = string.Empty;
        /// <summary>
        /// 表單對應
        /// </summary>
        public string PropertyName { get; set; } = string.Empty;
        /// <summary>
        /// 呼叫狀態
        /// </summary>
        public int InvokeStatus { get; set; } = (int)InvokeStatusEnum.Success;
        /// <summary>
        /// 訊息
        /// </summary>
        public string? Message { get; set; }
        /// <summary>
        /// 是否引用參考
        /// </summary>
        public bool DoLoadReference { get; set; } = false;
    }

    public enum InvokeStatusEnum
    {
        /// <summary>
        /// 失敗
        /// </summary>
        Failed = 0,
        /// <summary>
        /// 成功
        /// </summary>
        Success = 1,
    }
}
