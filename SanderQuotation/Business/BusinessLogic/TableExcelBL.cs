using AutoMapper;
using Business.Common;
using Business.DomainModel;
using CommonClass.Model;
using CommonClass.Models;
using Const;
using Core.Utility.Enums;
using Core.Utility.Extensions;
using Core.Utility.Helper.DB;
using Core.Utility.Helper.DB.Entity;
using Data.DataAccess.Dao;
using Data.DataAccess.Entity;
using DocumentFormat.OpenXml.Bibliography;
using System.Transactions;
using static Const.Enums;

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

        private IEsScheduleCycleExcelDAO? scheduleCycleExcelDAO;
        private IEsScheduleCycleExcelDAO GetScheduleCycleExcelDAO()
        {
            scheduleCycleExcelDAO ??= _unitOfWork.Repository<IEsScheduleCycleExcelDAO>();
            return scheduleCycleExcelDAO;
        }

        /// <summary>
        /// 依 TransferMappingCode 取得檔案轉入設定（含欄位明細）
        /// </summary>
        public EsFileTransferMappingDM? GetByCode(string transferMappingCode)
        {
            var entity = GetMappingDAO().FindByPropertys(new Dictionary<string, object>
            {
                { nameof(EsFileTransferMappingEntity.TransferMappingCode), transferMappingCode },
                { nameof(EsFileTransferMappingEntity.Status), (int)StatusEnum.Enabled }
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
        public PageResult<EsFileTransferMappingDM> GetPageList(PageEntity pageEntity, SearchVO searchVO)
        {
            var pageResult = GetMappingDAO().GetPageList(pageEntity, searchVO);

            var list = GetScheduleCycleExcelDAO().GetListByFilter(new SearchVO
            {
                TransferCodeIn = pageResult.Results.Select(r => r.TransferMappingCode).ToList()
            });
            var dms = pageResult.Results.Select(dto => new EsFileTransferMappingDM
            {
                Id = dto.Id,
                TransferMappingCode = dto.TransferMappingCode,
                ExampleFileName = dto.ExampleFileName,
                ExampleFileType = dto.ExampleFileType,
                SrcNasFilePath = dto.SrcNasFilePath,
                Description = dto.Description,
                Status = dto.Status ?? 0,
                MappingTables = dto.MappingTables,
            }).ToList();
            var scheduleCycleDict = list.GroupBy(x => x.TransferCode).ToDictionary(g => g.Key, g => g.Select(s =>new EsScheduleCycleDM()
            {
                ScheduleCycleCode = s.ScheduleCycleCode
            }).ToList());
            for(int i = 0; i < dms.Count; i++)
            {
                if(scheduleCycleDict.ContainsKey(dms[i].TransferMappingCode))
                {
                    dms[i].EsScheduleCycleDMs = scheduleCycleDict[dms[i].TransferMappingCode];
                }
            }
               

            return new PageResult<EsFileTransferMappingDM>
            {
                CurrentPage = pageResult.CurrentPage,
                DataCount = pageResult.DataCount,
                PageDataSize = pageResult.PageDataSize,
                Results = dms
            };
        }

        /// <summary>
        /// 刪除對應設定（同時刪除 EsFileTransferMappingColumn + EsFileTransferMapping）
        /// </summary>
        public void Delete(List<string> ids)
        {
            var pkList = GetMappingDAO().FindByPkList(ids.Select(x => Guid.Parse(x)).ToList());
            pkList.ForEach(x =>
            {
                x.UpdatedBy = UserInfo.UserAccount;
                x.UpdatedAt = base.now;
                x.Status = StatusEnum.Cancel.ToInt();
                GetMappingDAO().Update(x);
            });
            GetColumnDAO().FindListByFilter(new SearchVO
            {
                TransferMappingCodeIn = pkList.Select(x => x.TransferMappingCode).ToList(),
            }).ForEach(x =>
            {
                x.UpdatedBy = UserInfo.UserAccount;
                x.UpdatedAt = base.now;
                x.Status = StatusEnum.Cancel.ToInt();
                GetColumnDAO().Update(x);
            });
            _unitOfWork.Commit();
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
        public string? Insert(EsFileTransferMappingDM dm)
        {
            var entity = mapper.Map<EsFileTransferMappingEntity>(dm);
            //entity.RowGuid = Guid.NewGuid();
            //entity.TransferMappingCode = Guid.NewGuid().ToString("N").ToUpper();
            entity.Status = Status.Enable.ToInt();
            entity.CreatedAt = DateTime.Now;
            entity.CreatedBy = UserInfo?.UserAccount;

            using var scope = new TransactionScope();

            GetMappingDAO().Insert(entity);

            for (int i = 0; i < dm.Columns.Count; i++)
            {
                var colEntity = mapper.Map<EsFileTransferMappingColumnEntity>(dm.Columns[i]);
                //colEntity.RowGuid = Guid.NewGuid();
                colEntity.TransferMappingCode = entity.TransferMappingCode;
                colEntity.EsFileTransferMappingColumnID = Guid.NewGuid().ToString("N").ToUpper();
                colEntity.Status = StatusEnum.Enabled.ToInt();
                colEntity.SortNo = (i + 1).ToString();
                colEntity.CreatedAt = DateTime.Now;
                colEntity.CreatedBy = UserInfo?.UserAccount;
                GetColumnDAO().InsertAction(colEntity);
            }

            _unitOfWork.Commit();
            scope.Complete();

            return entity.TransferMappingCode;
        }


        public void CheckExist(EsFileTransferMappingDM dm)
        {
            if(dm.Id == Guid.Empty)
            {
                var existEntity = GetMappingDAO().FindByPropertys(new Dictionary<string, object>
                {
                    { nameof(EsFileTransferMappingEntity.TransferMappingCode), dm.TransferMappingCode },
                     { nameof(EsFileTransferMappingEntity.Status), StatusEnum.Enabled.ToInt() },
                });
                if (existEntity != null)
                {
                    GetMessage().SetAlert("此檔案轉檔代碼已存在");
                }
            }
        }

        /// <summary>
        /// 更新對應設定主檔及其欄位明細（先刪除舊欄位再重新寫入）
        /// </summary>
        public void Update(EsFileTransferMappingDM dm)
        {
            var entity = GetMappingDAO().FindByPk(dm.Id);

            entity.ExampleFileName = dm.ExampleFileName;
            entity.ExampleFileType = dm.ExampleFileType;
            entity.SrcNasFilePath = dm.SrcNasFilePath;
            entity.Description = dm.Description;
            entity.UpdatedAt = base.now;
            entity.UpdatedBy = UserInfo?.UserAccount;


            GetMappingDAO().Update(entity);

            var columnEntitys = GetColumnDAO().FindListByPropertys(new Dictionary<string, object>
                {
                    { nameof(EsFileTransferMappingColumnEntity.TransferMappingCode), entity.TransferMappingCode },
                    { nameof(EsFileTransferMappingColumnEntity.Status), StatusEnum.Enabled.ToInt() },
                });
            foreach (var col in columnEntitys)
            {
                col.Status = StatusEnum.Cancel.ToInt();
                col.UpdatedAt = base.now;
                col.UpdatedBy = UserInfo?.UserAccount;
                GetColumnDAO().Update(col);
            }

            for (int i = 0; i < dm.Columns.Count; i++)
            {
                var colEntity = mapper.Map<EsFileTransferMappingColumnEntity>(dm.Columns[i]);
                //colEntity.RowGuid                       = Guid.NewGuid();
                colEntity.TransferMappingCode = entity.TransferMappingCode;
                colEntity.EsFileTransferMappingColumnID = Guid.NewGuid().ToString("N").ToUpper();
                colEntity.Status = Status.Enable.ToInt();
                colEntity.SortNo = (i + 1).ToString();
                colEntity.CreatedAt = DateTime.Now;
                colEntity.CreatedBy = UserInfo?.UserAccount;
                GetColumnDAO().InsertAction(colEntity);
            }

            _unitOfWork.Commit();
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
