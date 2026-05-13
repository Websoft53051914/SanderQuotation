using AutoMapper;
using Business.Common;
using Business.DomainModel;
using Const;
using Core.Utility.Helper.DB;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;

namespace Business.BusinessLogic
{
    /// <summary>
    /// BOM 表內容
    /// </summary>
    public partial class BomFileContentBL : BaseProjectBL
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="unitOfWork">工作單元</param>
        public BomFileContentBL(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

            MapperConfiguration cfg = new(c =>
            {
                c.AllowNullCollections = true;
                c.CreateMap<BomFileContentEntity, BomFileContentDM>().ReverseMap();
                c.CreateMap<BomFileContentDTO, BomFileContentDM>().ReverseMap();
            });
            _mapper = cfg.CreateMapper();
        }

        /// <summary>
        /// 建構子 (含 Session)
        /// </summary>
        /// <param name="unitOfWork">工作單元</param>
        /// <param name="sessionVO">Session 資訊</param>
        public BomFileContentBL(IUnitOfWork unitOfWork, SessionVO sessionVO) : this(unitOfWork)
        {
            base.SessionVO = sessionVO;
        }
    }

    public partial class BomFileContentBL
    {
        #region -- BomFileContent --

        private IBomFileContentDAO? _dao = null;

        /// <summary>
        /// 取得 DAO 實例
        /// </summary>
        public IBomFileContentDAO GetDAO()
        {
            _dao ??= _unitOfWork.Repository<IBomFileContentDAO>();

            return _dao;
        }

        /// <summary>
        /// 依條件查詢清單
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        /// <returns>DM 清單</returns>
        public List<BomFileContentDM> GetListByFilter(SearchVO searchVO)
        {
            List<BomFileContentDTO> dtoList = GetDAO().GetListByFilter(searchVO);

            List<BomFileContentDM> result = [];
            foreach (BomFileContentDTO item in dtoList)
            {
                BomFileContentDM dm = _mapper.Map<BomFileContentDM>(item);
                result.Add(dm);
            }

            return result;
        }

        /// <summary>
        /// 取得單筆資料
        /// </summary>
        /// <param name="id">資料代號</param>
        /// <returns>DM 物件，找不到則回傳 null</returns>
        public BomFileContentDM? GetOneInfo(Guid id)
        {
            SearchVO searchVO = new();
            searchVO.IdEq = id;
            searchVO.IsLimit1 = true;

            return GetListByFilter(searchVO).FirstOrDefault();
        }

        /// <summary>
        /// 依上傳 ID 取得資料清單
        /// </summary>
        /// <param name="uploadId">檔案儲存代號</param>
        /// <returns>DM 清單</returns>
        public List<BomFileContentDM> GetListByUploadId(Guid uploadId)
        {
            SearchVO searchVO = new();
            searchVO.UploadIdEq = uploadId;

            return GetListByFilter(searchVO);
        }

        /// <summary>
        /// 新增資料
        /// </summary>
        /// <param name="dm">DM 物件</param>
        public void DoCreate(BomFileContentDM dm)
        {
            BomFileContentEntity entity = _mapper.Map<BomFileContentEntity>(dm);

            GetDAO().Insert(entity);
            dm.Id = entity.Id;
        }

        /// <summary>
        /// 更新資料
        /// </summary>
        /// <param name="dm">DM 物件</param>
        public void DoUpdate(BomFileContentDM dm)
        {
            BomFileContentEntity? entity = GetDAO().FindByPk(dm.Id);
            if (entity != null)
            {
                entity.ComponentPart = dm.ComponentPart;
                entity.Description = dm.Description;
                entity.Qty = dm.Qty;
                entity.Manufacturer = dm.Manufacturer;
                entity.ManufacturerPartNumber = dm.ManufacturerPartNumber;
                entity.DisplayPart = dm.DisplayPart;
                entity.UploadId = dm.UploadId;

                GetDAO().Update(entity);
            }
        }

        /// <summary>
        /// 依條件刪除資料
        /// </summary>
        /// <param name="searchVO">查詢條件</param>
        public void DeleteByFilter(SearchVO searchVO)
        {
            string account = SessionVO?.Account ?? string.Empty;
            GetDAO().DeleteByFilter(searchVO, account);
        }

        /// <summary>
        /// 依 UploadId 查詢 BOM 料項及其查價結果（LEFT JOIN tbbomfilequotation）
        /// </summary>
        /// <param name="uploadId">EsFileTransferUpload.UploadId</param>
        /// <returns>聯合查詢清單</returns>
        public List<Data.DataAccess.DTO.BomFileContentQuotationDTO> GetListWithQuotationByUploadId(Guid uploadId)
        {
            return GetDAO().GetListWithQuotationByUploadId(uploadId);
        }

        #endregion -- BomFileContent --
    }
}
