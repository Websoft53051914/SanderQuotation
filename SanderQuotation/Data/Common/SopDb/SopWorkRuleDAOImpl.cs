using Core.Utility.Base.Data;
using Data.Common.SopDb.DTO;

namespace Data.Common.SopDb
{
    /// <summary>
    /// SOP 工規查詢 DAO 實作
    /// 使用 DaoFactory / UnitOfWorkSOPSqlServer 注入的 DbHelper 連線至 SOP 資料庫
    /// </summary>
    public class SopWorkRuleDAOImpl : BaseSuperImpl, ISopWorkRuleDAO
    {
        /// <summary>
        /// 取得 SOP 工單清單（OrderFlowId = 4，待產生檔案狀態）
        /// </summary>
        public List<SopOrderDTO> GetOrderList()
        {
            string sql = @"
SELECT
    o.OrderID,
    o.DocNo,
    o.DocVersion,
    md.ItemPageData
FROM SOP_Order o
INNER JOIN SOP_OrderTrace ot ON o.OrderID = ot.OrderID
INNER JOIN SOP_Metadata md  ON md.MetadataID = o.MetadataID
WHERE 1=1
  AND o.EndTime   = '2999-12-31 00:00:00'
  AND ot.EndTime  = '2999-12-31 00:00:00'
  AND md.EndTime  = '2999-12-31 00:00:00'
  -- AND ot.OrderFlowId = 4
ORDER BY o.DocNo, o.DocVersion
";
            return DbHelper.FindList<SopOrderDTO>(sql, new Dictionary<string, object>());
        }

        /// <summary>
        /// 依工單 ID 取得單筆工單（含 ItemPageData）
        /// </summary>
        /// <param name="orderId">工單 ID</param>
        public SopOrderDTO GetOrderById(string orderId)
        {
            string sql = @"
SELECT
    o.OrderID,
    o.DocNo,
    o.DocVersion,
    md.ItemPageData
FROM SOP_Order o
INNER JOIN SOP_OrderTrace ot ON o.OrderID = ot.OrderID
INNER JOIN SOP_Metadata md  ON md.MetadataID = o.MetadataID
WHERE 1=1
  AND o.EndTime   = '2999-12-31 00:00:00'
  AND ot.EndTime  = '2999-12-31 00:00:00'
  AND md.EndTime  = '2999-12-31 00:00:00'
  -- AND ot.OrderFlowId = 4
  AND o.OrderID = @OrderID
";
            Dictionary<string, object> paras = new();
            paras.Add("OrderID", orderId);
            return DbHelper.Find<SopOrderDTO>(sql, paras);
        }
    }
}
