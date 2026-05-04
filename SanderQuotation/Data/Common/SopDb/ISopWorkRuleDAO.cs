using Core.Utility.Base.Data;
using Data.Common.SopDb.DTO;

namespace Data.Common.SopDb
{
    /// <summary>
    /// SOP 工規查詢 DAO 介面
    /// </summary>
    public interface ISopWorkRuleDAO : IBaseSuperDao
    {
        /// <summary>
        /// 取得 OrderFlowId=4（待產生檔案）的工單清單
        /// </summary>
        List<SopOrderDTO> GetOrderList();

        /// <summary>
        /// 依 OrderID 取得工單的 ItemPageData（規格 JSON）
        /// </summary>
        /// <param name="orderId">工單 ID</param>
        SopOrderDTO GetOrderById(string orderId);
    }
}
