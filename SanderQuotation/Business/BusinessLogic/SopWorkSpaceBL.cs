using Business.Common;
using Business.DomainModel;
using Data.Common.SopDb;
using Data.Common.SopDb.DTO;
using Data.UnitOfWork;
using System.Text.Json;

namespace Business.BusinessLogic
{
    /// <summary>
    /// SOP 工規工作站 BL：提供工單下拉、站點下拉、工規內容查詢
    /// </summary>
    public class SopWorkSpaceBL : BaseProjectBL
    {
        private readonly IUnitOfWorkSOP _unitOfWorkSOP;

        /// <summary>
        /// 建構子，由 Unity 自動注入 IUnitOfWorkSOP
        /// </summary>
        public SopWorkSpaceBL(IUnitOfWorkSOP unitOfWorkSOP)
        {
            _unitOfWorkSOP = unitOfWorkSOP;
        }

        private ISopWorkRuleDAO GetDAO()
        {
            return _unitOfWorkSOP.Repository<ISopWorkRuleDAO>();
        }

        /// <summary>
        /// 取得工單下拉選單清單（OrderFlowId=4）
        /// </summary>
        public List<SopOrderDM> GetOrderList()
        {
            List<SopOrderDTO> dtoList = GetDAO().GetOrderList();
            List<SopOrderDM> result = new();
            foreach (SopOrderDTO dto in dtoList)
            {
                SopOrderDM dm = new();
                dm.OrderID = dto.OrderID;
                dm.DisplayText = $"{dto.DocNo} {dto.DocVersion}";
                result.Add(dm);
            }
            return result;
        }

        /// <summary>
        /// 依工單 ID 取得該工單底下的站點清單
        /// </summary>
        /// <param name="orderId">工單 ID</param>
        public List<SopStationDM> GetStationList(string orderId)
        {
            SopOrderDTO dto = GetDAO().GetOrderById(orderId);
            if (dto == null || string.IsNullOrWhiteSpace(dto.ItemPageData))
                return new List<SopStationDM>();

            List<SopItemPageDTO> pages = ParseItemPageData(dto.ItemPageData);
            List<SopStationDM> result = new();
            foreach (SopItemPageDTO page in pages)
            {
                SopStationDM dm = new();
                dm.OpNo = page.OpNo;
                dm.OpName = page.OpName;
                result.Add(dm);
            }
            return result;
        }

        /// <summary>
        /// 依工單 ID 與工站編號取得工規內容
        /// </summary>
        /// <param name="orderId">工單 ID</param>
        /// <param name="opNo">工站編號</param>
        public SopStationContentDM? GetStationContent(string orderId, string opNo)
        {
            SopOrderDTO? dto = GetDAO().GetOrderById(orderId);
            if (dto == null || string.IsNullOrWhiteSpace(dto.ItemPageData))
                return null;

            List<SopItemPageDTO> pages = ParseItemPageData(dto.ItemPageData);
            SopItemPageDTO? page = pages.Find(p => p.OpNo == opNo);
            if (page == null)
                return null;

            SopStationContentDM dm = new();
            dm.OpNo = page.OpNo;
            dm.OpName = page.OpName;
            dm.Special = page.Special;
            dm.RuleType = page.RuleType;

            dm.RuleList = new();
            if (page.RuleList != null)
            {
                foreach (SopRuleRowDTO row in page.RuleList)
                {
                    SopRuleRowDM rowDm = new();
                    rowDm.RuleType = row.RuleType;
                    rowDm.Machine = row.Machine;
                    rowDm.Item = row.Item;
                    rowDm.Spec = row.Spec;
                    rowDm.Reference = row.Reference;
                    dm.RuleList.Add(rowDm);
                }
            }

            dm.Remark = new();
            if (page.Remark != null)
            {
                foreach (SopRemarkDTO remark in page.Remark)
                {
                    SopRemarkDM remarkDm = new();
                    remarkDm.ContentText = remark.ContentText;
                    dm.Remark.Add(remarkDm);
                }
            }

            dm.ImageFilePaths = page.ImageFilePathList ?? new List<string>();

            return dm;
        }

        private List<SopItemPageDTO> ParseItemPageData(string json)
        {
            JsonSerializerOptions options = new();
            options.PropertyNameCaseInsensitive = true;
            return JsonSerializer.Deserialize<List<SopItemPageDTO>>(json, options)
                   ?? new List<SopItemPageDTO>();
        }
    }
}
