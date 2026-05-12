using AutoMapper;
using Business.Common;
using Business.DomainModel;
using CommonClass.Model;
using CommonClass.Models;
using Const;
using Core.Utility.Enums;
using Core.Utility.Helper.DB;
using Core.Utility.Helper.DB.Entity;
using Data.DataAccess.Dao;
using Data.DataAccess.Entity;
using System.Transactions;

namespace Business.BusinessLogic
{
    public partial class TableExcelBL : BaseProjectBL
    {
        private readonly IMapper mapper;
        private readonly IUnitOfWork _unitOfWork;

        public TableExcelBL(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<EsFileTransferMappingDM, EsFileTransferMappingEntity>().ReverseMap();
                cfg.CreateMap<EsFileTransferMappingColumnDM, EsFileTransferMappingColumnEntity>().ReverseMap();
                cfg.CreateMap<ESDbTransferEntity, ESDbTransferDM>();
            });
            mapper = configuration.CreateMapper();
        }

        public TableExcelBL(IUnitOfWork unitOfWork, SessionVO sessionVO) : this(unitOfWork)
        {
            base.SessionVO = sessionVO;
        }

        private IEsFileTransferMappingDAO? mappingDAO;
        private IEsFileTransferMappingDAO GetMappingDAO()
        {
            mappingDAO ??= _unitOfWork.Repository<IEsFileTransferMappingDAO>();
            return mappingDAO;
        }

        private IEsFileTransferMappingColumnDAO? columnDAO;
        private IEsFileTransferMappingColumnDAO GetColumnDAO()
        {
            columnDAO ??= _unitOfWork.Repository<IEsFileTransferMappingColumnDAO>();
            return columnDAO;
        }

        private IESDbTransferDAO? dbTransferDAO;
        private IESDbTransferDAO GetDbTransferDAO()
        {
            dbTransferDAO ??= _unitOfWork.Repository<IESDbTransferDAO>();
            return dbTransferDAO;
        }

        /// <summary>
        /// 依 TransferMappingCode 取得檔案轉入設定（含欄位明細）
        /// </summary>
        public EsFileTransferMappingDM? GetByCode(string transferMappingCode)
        {
            var entity = GetMappingDAO().FindByPropertys(new Dictionary<string, object>
            {
                { nameof(EsFileTransferMappingEntity.TransferMappingCode), transferMappingCode }
            });
            if (entity == null) return null;

            var dm = mapper.Map<EsFileTransferMappingDM>(entity);
            var columns = GetColumnDAO().GetListByMappingSettingId(entity.TransferMappingCode);
            dm.Columns = columns.Select(c => mapper.Map<EsFileTransferMappingColumnDM>(c)).ToList();
            return dm;
        }

        /// <summary>
        /// 依 DBTransferMappingCode 取得目標資料庫連線設定
        /// </summary>
        public ESDbTransferDM? GetDbTransferConfig(string dbTransferMappingCode)
        {
            var entity = GetDbTransferDAO().FindByPropertys(new Dictionary<string, object>
            {
                { nameof(ESDbTransferEntity.TransferCode), dbTransferMappingCode }
            });
            return entity == null ? null : mapper.Map<ESDbTransferDM>(entity);
        }

        /// <summary>
        /// 取得分頁列表
        /// </summary>
        public PageResult<EsFileTransferMappingDM> GetPageList(CommonSearchQuery query)
        {
            var pageResult = GetMappingDAO().GetPageList(query);

            return new PageResult<EsFileTransferMappingDM>
            {
                CurrentPage = pageResult.CurrentPage,
                DataCount = pageResult.DataCount,
                PageDataSize = pageResult.PageDataSize,
                Results = pageResult.Results.Select(dto => new EsFileTransferMappingDM
                {
                    //Id                 = dto.Id,
                    TransferMappingCode = dto.TransferMappingCode,
                    ExampleFileName = dto.ExampleFileName,
                    ExampleFileType = dto.ExampleFileType,
                    SrcNasFilePath = dto.SrcNasFilePath,
                    Description = dto.Description,
                    Status = dto.Status,
                    MappingTables = dto.MappingTables,
                }).ToList()
            };
        }

        /// <summary>
        /// 刪除對應設定（同時刪除 EsFileTransferMappingColumn + EsFileTransferMapping）
        /// </summary>
        public DispatcherReturnMsg Delete(List<string> ids)
        {
            try
            {
                using var scope = new TransactionScope();

                foreach (var id in ids)
                {
                    if (!Guid.TryParse(id, out var rowGuid)) continue;

                    var entity = GetMappingDAO().FindByPropertys(new Dictionary<string, object>
                    {
                        { nameof(EsFileTransferMappingEntity.Id), rowGuid }
                    });
                    if (entity == null) continue;

                    GetColumnDAO().DeleteByPropertys(new Dictionary<string, object>
                    {
                        { nameof(EsFileTransferMappingColumnEntity.TransferMappingCode), entity.TransferMappingCode }
                    });
                    _unitOfWork.Commit();

                    //GetMappingDAO().Delete(rowGuid);
                    _unitOfWork.Commit();
                }

                scope.Complete();
                return new DispatcherReturnMsg { IsSuccess = "Y" };
            }
            catch (Exception ex)
            {
                return new DispatcherReturnMsg
                {
                    IsSuccess = "N",
                    AlertLevel = "error",
                    ReturnCode = "E500",
                    ReturnMsg = ex.Message
                };
            }
        }

        /// <summary>
        /// 取得單筆對應設定（含所有欄位明細）
        /// </summary>
        public EsFileTransferMappingDM? GetOne(Guid rowGuid)
        {
            var entity = GetMappingDAO().FindByPropertys(new Dictionary<string, object>
            {
                { nameof(EsFileTransferMappingEntity.Id), rowGuid }
            });
            if (entity == null) return null;

            var dm = mapper.Map<EsFileTransferMappingDM>(entity);
            var columns = GetColumnDAO().GetListByMappingSettingId(entity.TransferMappingCode);
            dm.Columns = columns.Select(c => mapper.Map<EsFileTransferMappingColumnDM>(c)).ToList();
            return dm;
        }

        /// <summary>
        /// 新增對應設定主檔及其欄位明細（同時寫入 EsFileTransferMapping + EsFileTransferMappingColumn）
        /// </summary>
        public (DispatcherReturnMsg result, string? error, string? transferCode) Insert(EsFileTransferMappingDM dm)
        {
            var entity = mapper.Map<EsFileTransferMappingEntity>(dm);
            //entity.RowGuid = Guid.NewGuid();
            //entity.TransferMappingCode = Guid.NewGuid().ToString("N").ToUpper();
            entity.Status = Status.Enable.ToValueString();
            entity.CreatedAt = DateTime.Now;
            entity.CreatedBy = UserInfo?.UserAccount;

            using var scope = new TransactionScope();

            GetMappingDAO().Insert(entity);
            _unitOfWork.Commit();

            for (int i = 0; i < dm.Columns.Count; i++)
            {
                var colEntity = mapper.Map<EsFileTransferMappingColumnEntity>(dm.Columns[i]);
                //colEntity.RowGuid = Guid.NewGuid();
                colEntity.TransferMappingCode = entity.TransferMappingCode;
                colEntity.EsFileTransferMappingColumnID = Guid.NewGuid().ToString("N").ToUpper();
                colEntity.Status = Status.Enable.ToValueString();
                colEntity.SortNo = (i + 1).ToString();
                colEntity.CreatedAt = DateTime.Now;
                colEntity.CreatedBy = UserInfo?.UserAccount;
                GetColumnDAO().Insert(colEntity);
            }

            _unitOfWork.Commit();
            scope.Complete();

            return (new DispatcherReturnMsg { IsSuccess = "Y" }, null, entity.TransferMappingCode);
        }

        /// <summary>
        /// 更新對應設定主檔及其欄位明細（先刪除舊欄位再重新寫入）
        /// </summary>
        public DispatcherReturnMsg Update(EsFileTransferMappingDM dm)
        {
            try
            {
                var entity = GetMappingDAO().FindByPropertys(new Dictionary<string, object>
                {
                    { nameof(EsFileTransferMappingEntity.Id), dm.Id }
                });
                if (entity == null)
                    return new DispatcherReturnMsg { IsSuccess = "N", ReturnCode = "E404", ReturnMsg = "資料不存在" };

                entity.ExampleFileName = dm.ExampleFileName;
                entity.ExampleFileType = dm.ExampleFileType;
                entity.SrcNasFilePath = dm.SrcNasFilePath;
                entity.Description = dm.Description;
                entity.UpdatedAt = DateTime.Now;
                entity.UpdatedBy = UserInfo?.UserAccount;

                using var scope = new TransactionScope();

                GetMappingDAO().Update(entity);
                _unitOfWork.Commit();

                GetColumnDAO().DeleteByPropertys(new Dictionary<string, object>
                {
                    { nameof(EsFileTransferMappingColumnEntity.TransferMappingCode), entity.TransferMappingCode }
                });
                _unitOfWork.Commit();

                for (int i = 0; i < dm.Columns.Count; i++)
                {
                    var colEntity = mapper.Map<EsFileTransferMappingColumnEntity>(dm.Columns[i]);
                    //colEntity.RowGuid                       = Guid.NewGuid();
                    colEntity.TransferMappingCode = entity.TransferMappingCode;
                    colEntity.EsFileTransferMappingColumnID = Guid.NewGuid().ToString("N").ToUpper();
                    colEntity.Status = Status.Enable.ToValueString();
                    colEntity.SortNo = (i + 1).ToString();
                    colEntity.CreatedAt = DateTime.Now;
                    colEntity.CreatedBy = UserInfo?.UserAccount;
                    GetColumnDAO().Insert(colEntity);
                }

                _unitOfWork.Commit();
                scope.Complete();

                return new DispatcherReturnMsg { IsSuccess = "Y" };
            }
            catch (Exception ex)
            {
                return new DispatcherReturnMsg
                {
                    IsSuccess = "N",
                    AlertLevel = "error",
                    ReturnCode = "E500",
                    ReturnMsg = ex.Message
                };
            }
        }
    }

    public partial class TableExcelBL
    {
        /// <summary>
        /// 取得所有匯入規則並包含 IsBomFileRule 標記
        /// （判斷依據：EsFileTransferMappingColumn 下是否存在 TargetTableName == 'bomfilecontent'）
        /// </summary>
        public List<EsFileTransferMappingDM> GetListWithBomFlag(SearchVO searchVO)
        {
            return GetMappingDAO().GetListWithBomFlag(searchVO)
                .Select(dto =>
                {
                    EsFileTransferMappingDM dm = mapper.Map<EsFileTransferMappingDM>(dto);
                    dm.IsBomFileRule = dto.IsBomFileRule;
                    return dm;
                })
                .ToList();
        }

        /// <summary>
        /// 依 Id 取得單筆匯入規則（含 IsBomFileRule 標記）
        /// </summary>
        /// <param name="id">EsFileTransferMapping.Id</param>
        /// <returns></returns>
        public EsFileTransferMappingDM? GetOneWithBomFlag(Guid id)
        {
            SearchVO searchVO = new();
            searchVO.IdEq = id;

            return GetListWithBomFlag(searchVO).FirstOrDefault();
        }
    }
}
