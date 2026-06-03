namespace backend.Services.QuotationResult;

/// <summary>
/// 定時查價結果寫入類操作結果（成功或業務錯誤訊息），供 AppService 回傳、Controller 轉 JSON。
/// </summary>
public class QuotationResultServiceResult
{
    /// <summary>輸出參數：是否成功。</summary>
    public bool Success { get; init; }

    /// <summary>輸出參數：業務失敗訊息；成功時為 null。</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>功能說明：建立成功結果。</summary>
    /// <returns>輸出參數：Success=true 的 QuotationResultServiceResult。</returns>
    /// <remarks>
    /// 參考功能名稱與用途：ReInternalQuotationAsync 等寫入流程結束時回傳。
    /// 訊息內容及生成條件：無 ErrorMessage；Controller 對應 JsonOK。
    /// </remarks>
    public static QuotationResultServiceResult Ok() => new() { Success = true };

    /// <summary>功能說明：建立業務失敗結果。</summary>
    /// <param name="message">輸入參數：錯誤訊息（如「資料不存在」）。</param>
    /// <returns>輸出參數：Success=false、ErrorMessage=message。</returns>
    /// <remarks>
    /// 參考功能名稱與用途：AppService 驗證失敗；Controller 轉 JsonValidFail(ErrorMessage)。
    /// 訊息內容及生成條件：由呼叫端傳入固定或 GetMsg 字串。
    /// </remarks>
    public static QuotationResultServiceResult Fail(string message) =>
        new() { Success = false, ErrorMessage = message };
}
