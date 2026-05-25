using Const;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;
using System.Text;
using static Const.Enums;

namespace Data.DataAccess.Impl
{
    /// <summary>
    /// BOM 決策歷程 DAO 實作
    /// </summary>
    public class TBBomFileDecisionLogDaoImpl : Core.Utility.Base.Data.GuidId.BaseImpl<TBBomFileDecisionLogEntity>, ITBBomFileDecisionLogDAO
    {
        /// <summary>
        /// 依條件查詢清單
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <returns>清單資料</returns>
        public List<TBBomFileDecisionLogDTO> GetListByFilter(SearchVO searchVO)
        {
            StringBuilder condition = new();
            string sqlLimit = string.Empty;
            Dictionary<string, object> paras = [];

            if (searchVO.IsLimit1)
            {
                sqlLimit = " LIMIT 1 ";
            }
            if (searchVO.IdEq.HasValue)
            {
                condition.Append($"AND l.{nameof(TBBomFileDecisionLogDTO.Id)} = @{nameof(searchVO.IdEq)} ");
                paras.Add(nameof(searchVO.IdEq), searchVO.IdEq);
            }
            if (searchVO.StatusEq.HasValue)
            {
                condition.Append($"AND l.{nameof(TBBomFileDecisionLogDTO.Status)} = @{nameof(searchVO.StatusEq)} ");
                paras.Add(nameof(searchVO.StatusEq), searchVO.StatusEq);
            }
            if (searchVO.BomFileContentIdEq.HasValue)
            {
                condition.Append($"AND l.{nameof(TBBomFileDecisionLogDTO.BomFileContentId)} = @{nameof(searchVO.BomFileContentIdEq)} ");
                paras.Add(nameof(searchVO.BomFileContentIdEq), searchVO.BomFileContentIdEq);
            }

            string sql = $@"
SELECT l.*
FROM tb_bomfiledecisionlog l
WHERE 1=1
{condition}
ORDER BY l.Stage, l.Step
{sqlLimit}";

            return DbHelper.FindList<TBBomFileDecisionLogDTO>(sql, paras);
        }

        /// <summary>
        /// 依條件刪除資料 (邏輯刪除)
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <param name="account">執行帳號</param>
        public void DeleteByFilter(SearchVO searchVO, string account)
        {
            StringBuilder condition = new();
            Dictionary<string, object> paras = [];

            if (searchVO.IdEq.HasValue)
            {
                condition.Append($"AND l.{nameof(TBBomFileDecisionLogDTO.Id)} = @{nameof(searchVO.IdEq)} ");
                paras.Add(nameof(searchVO.IdEq), searchVO.IdEq);
            }
            else if (searchVO.IdIn?.Count > 0)
            {
                condition.Append($"AND l.{nameof(TBBomFileDecisionLogDTO.Id)} = ANY(@{nameof(searchVO.IdIn)}) ");
                paras.Add(nameof(searchVO.IdIn), searchVO.IdIn);
            }
            if (searchVO.BomFileContentIdEq.HasValue)
            {
                condition.Append($"AND l.{nameof(TBBomFileDecisionLogDTO.BomFileContentId)} = @{nameof(searchVO.BomFileContentIdEq)} ");
                paras.Add(nameof(searchVO.BomFileContentIdEq), searchVO.BomFileContentIdEq);
            }
            if (searchVO.StageEq.HasValue)
            {
                condition.Append($"AND l.{nameof(TBBomFileDecisionLogDTO.Stage)} = @{nameof(searchVO.StageEq)} ");
                paras.Add(nameof(searchVO.StageEq), searchVO.StageEq);
            }

            ArgumentNullException.ThrowIfNull(condition);

            paras.Add("DeleteStatus", (int)StatusEnum.Cancel);
            paras.Add("UpdatedBy", account);
            paras.Add("UpdatedAt", DateTime.Now);

            string sql = $@"
UPDATE tb_bomfiledecisionlog l
SET
    status = @DeleteStatus
    , updatedby = @UpdatedBy
    , updatedat = @UpdatedAt
WHERE
    1 = 1
{condition}";

            DbHelper.Execute(sql, paras);
        }

        /// <summary>
        /// 物理刪除指定 Status 的資料
        /// </summary>
        /// <param name="status">狀態值</param>
        public void PhysicalDeleteByStatus(int status)
        {
            Dictionary<string, object> paras = [];
            paras.Add("status", status);

            string sql = @"
DELETE FROM tb_bomfiledecisionlog
WHERE status = @status";

            DbHelper.Execute(sql, paras);
            DbHelper.Commit();
        }
    }
}
