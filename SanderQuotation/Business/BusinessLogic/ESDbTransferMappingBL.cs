using AutoMapper;
using Business.Common;
using Business.DomainModel;
using CommonClass.Model;
using CommonClass.Models;
using Core.Utility.Helper.DB.Entity;
using Core.Utility.Helper.DB;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;
using Microsoft.Extensions.Configuration;
using static Const.Enums;
using Core.Utility.Extensions;

namespace Business.BusinessLogic
{
    public partial class ESDbTransferMappingBL : BaseProjectBL
    {
        private IMapper mapper;
        private IUnitOfWork _unitOfWork;

        public ESDbTransferMappingBL(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.AllowNullCollections = true;

                cfg.CreateMap<ESDbTransferMappingDM, ESDbTransferMappingEntity>();
                cfg.CreateMap<ESDbTransferMappingEntity, ESDbTransferMappingDM>();
                cfg.CreateMap<ESDbTransferMappingDM, ESDbTransferMappingDTO>();
                cfg.CreateMap<ESDbTransferMappingDTO, ESDbTransferMappingDM>();

                cfg.CreateMap<ESDbTransferMappingColumnDM, ESDbTransferMappingColumnEntity>();
                cfg.CreateMap<ESDbTransferMappingColumnEntity, ESDbTransferMappingColumnDM>();
                cfg.CreateMap<ESDbTransferMappingColumnDM, ESDbTransferMappingColumnDTO>();
                cfg.CreateMap<ESDbTransferMappingColumnDTO, ESDbTransferMappingColumnDM>();
            });

            mapper = configuration.CreateMapper();
        }

        public ESDbTransferMappingBL(IUnitOfWork unitOfWork, SessionVO sessionVO) : this(unitOfWork)
        {
            base.SessionVO = sessionVO;
        }

        private IESDbTransferMappingDAO dao;
        private IESDbTransferMappingDAO GetDAO()
        {
            dao ??= _unitOfWork.Repository<IESDbTransferMappingDAO>();
            return dao;
        }
    }

    public partial class ESDbTransferMappingBL
    {
        public PageResult<ESDbTransferMappingDM> GetPageList(PageEntity pageEntity, ESDbTransferMappingDM dm)
        {
            var dto = mapper.Map<ESDbTransferMappingDTO>(dm);
            var result = GetDAO().FindPageList(pageEntity, dto);

            var dms = mapper.Map<List<ESDbTransferMappingDM>>(result.Results);

            return new PageResult<ESDbTransferMappingDM>
            {
                CurrentPage = result.CurrentPage,
                DataCount = result.DataCount,
                PageDataSize = result.PageDataSize,
                Results = dms
            };
        }

        public List<ESDbTransferMappingDM> GetAll()
        {
            var entities = GetDAO().FindByAll();
            return entities.Select(x => mapper.Map<ESDbTransferMappingDM>(x)).ToList();
        }

        private bool CheckDstDuplicate(string dstDbTransferCode, string dstTableName, Guid? excludeId = null)
        {
            var existing = GetDAO().FindListByPropertys(new Dictionary<string, object>
            {
                { nameof(ESDbTransferMappingEntity.DstDbTransferCode), dstDbTransferCode },
                { nameof(ESDbTransferMappingEntity.DstTableName), dstTableName }
            });
            if (excludeId.HasValue)
                existing = existing.Where(x => x.Id != excludeId.Value).ToList();
            return existing.Count > 0;
        }

        public Guid Create(ESDbTransferMappingDM dm)
        {
            var entity = mapper.Map<ESDbTransferMappingEntity>(dm);
            entity.Status = StatusEnum.Enabled.ToInt();
            var dtNow = DateTime.Now;
            entity.CreatedBy = UserInfo.UserAccount;
            entity.UpdatedBy = UserInfo.UserAccount;
            entity.CreatedAt = dtNow;
            entity.UpdatedAt = dtNow;

            IESDbTransferMappingDAO dao = _unitOfWork.Repository<IESDbTransferMappingDAO>();
            //var result = dao.Insert(entity);

            dao.InsertAction(entity);


            if (dm.Columns?.Count > 0)
                InsertColumns(entity.TransferMappingCode, dm.Columns, dtNow);
            _unitOfWork.Commit();
            return entity.Id;
        }

        private void InsertColumns(string TransferMappingCode, List<ESDbTransferMappingColumnDM> columns, DateTime dtNow)
        {
            var colDao = _unitOfWork.Repository<IESDbTransferMappingColumnDAO>();
            var index = 1;
            foreach (var col in columns)
            {
                var colEntity = mapper.Map<ESDbTransferMappingColumnEntity>(col);
                colEntity.TransferMappingCode = TransferMappingCode;
                colEntity.Status = StatusEnum.Enabled.ToInt();
                colEntity.CreatedBy = UserInfo.UserAccount;
                colEntity.UpdatedBy = UserInfo.UserAccount;
                colEntity.CreatedAt = dtNow;
                colEntity.UpdatedAt = dtNow;
                colEntity.SortNo = "S" + index.ToString().PadLeft(4, '0');

                colDao.InsertAction(colEntity);

                index++;
            }

        }

        public void Delete(List<Guid> ids)
        {
            IESDbTransferMappingDAO dao = _unitOfWork.Repository<IESDbTransferMappingDAO>();
            foreach (var id in ids)
            {
                dao.Delete(id);
            }

            dao.DbHelper.Commit();
        }

        public ESDbTransferMappingDM Get(Guid id)
        {
            IESDbTransferMappingDAO dao = _unitOfWork.Repository<IESDbTransferMappingDAO>();
            var entity = dao.FindByPk(id);
            var newDM = mapper.Map<ESDbTransferMappingDM>(entity);
            return newDM;
        }

        public ESDbTransferMappingDM GetByCode(string transferMappingCode)
        {
            var dao = _unitOfWork.Repository<IESDbTransferMappingDAO>();
            var entity = dao.FindByPropertys(new Dictionary<string, object>
            {
                { nameof(ESDbTransferMappingEntity.TransferMappingCode), transferMappingCode }
            });
            return mapper.Map<ESDbTransferMappingDM>(entity);
        }

        public List<ESDbTransferMappingColumnDM> GetColumns(string TransferMappingCode)
        {
            var colDao = _unitOfWork.Repository<IESDbTransferMappingColumnDAO>();
            var entities = colDao.FindListByPropertys(new Dictionary<string, object>
            {
                { nameof(ESDbTransferMappingColumnEntity.TransferMappingCode), TransferMappingCode },
                    { nameof(ESDbTransferMappingColumnEntity.Status), StatusEnum.Enabled.ToInt() }
            });
            return entities.Select(x => mapper.Map<ESDbTransferMappingColumnDM>(x)).OrderBy(s => s.SortNo).ToList();
        }

        public ESDbTransferMappingDM GetById(Guid id)
        {
            var dao = _unitOfWork.Repository<IESDbTransferMappingDAO>();
            var entity = dao.FindByPropertys(new Dictionary<string, object>
            {
                { nameof(ESDbTransferMappingEntity.Id), id }
            });
            return mapper.Map<ESDbTransferMappingDM>(entity);
        }

        public void Edit(ESDbTransferMappingDM dm)
        {
            IESDbTransferMappingDAO dao = _unitOfWork.Repository<IESDbTransferMappingDAO>();
            var dto = dao.FindByPk(dm.Id);

            var entity = mapper.Map<ESDbTransferMappingEntity>(dm);
            entity.Status = dto.Status;
            entity.Type = dto.Type;
            entity.SortNo = dto.SortNo;
            entity.Priority = dto.Priority;
            entity.CreatedAt = dto.CreatedAt;
            entity.CreatedBy = dto.CreatedBy;
            entity.UpdatedAt = DateTime.Now;

            dao.Update(entity);

            // 先刪除舊欄位對應，再寫入新的
            var colDao = _unitOfWork.Repository<IESDbTransferMappingColumnDAO>();
            var oldCols = colDao.FindListByPropertys(new Dictionary<string, object> { { nameof(ESDbTransferMappingColumnEntity.TransferMappingCode), entity.TransferMappingCode } });
            foreach (var old in oldCols)
            {
                old.UpdatedBy = UserInfo.UserAccount;
                old.UpdatedAt = base.now;
                old.Status = StatusEnum.Cancel.ToInt();
                colDao.Update(old);


            }

            if (dm.Columns?.Count > 0)
                InsertColumns(entity.TransferMappingCode, dm.Columns,base.now);

            _unitOfWork.Commit();
        }


        /// <summary>
        /// 執行指定的資料傳輸對應設定
        /// </summary>
        /// <param name="mappingRowGuid">傳輸對應設定的 RowGuid（與 transferMappingCode 擇一使用）</param>
        /// <param name="transferMappingCode">傳輸對應設定代碼（與 mappingRowGuid 擇一使用）</param>
        /// <param name="secretKey">加密金鑰（選填，若欄位需要加密則必填）</param>
        /// <param name="secretIV">加密向量（選填，若欄位需要加密則必填）</param>
        /// <returns>執行結果</returns>
        public EsScheduleCycleLogDetailDM ExecuteTransfer(
            Guid? mappingRowGuid = null,
            string transferMappingCode = null,
            string secretKey = "",
            string secretIV = "")
        {
            try
            {
                if (!mappingRowGuid.HasValue && string.IsNullOrEmpty(transferMappingCode))
                    return ErrorResult("必須提供 mappingRowGuid 或 transferMappingCode 其中之一");

                ESDbTransferMappingDM mapping = null;

                if (mappingRowGuid.HasValue)
                {
                    var mappingResponse = GetById(mappingRowGuid.Value);
                    if (mappingResponse == null)
                        return ErrorResult("找不到指定的傳輸對應設定");
                    mapping = mappingResponse;
                }
                else
                {
                    var entity = GetDAO().FindByPropertys(new Dictionary<string, object>
                    {
                        { nameof(ESDbTransferMappingEntity.TransferMappingCode), transferMappingCode }
                    });

                    if (entity == null)
                        return ErrorResult("找不到指定的傳輸對應設定");

                    mapping = mapper.Map<ESDbTransferMappingDM>(entity);
                }

                return ExecuteTransferCore(mapping, secretKey, secretIV);
            }
            catch (Exception ex)
            {
                return ErrorResult($"執行傳輸時發生錯誤: {ex.Message}");
            }
        }



        private static EsScheduleCycleLogDetailDM ErrorResult(string message) =>
            new EsScheduleCycleLogDetailDM
            {
                JobStatus = "Failed",
                ErrorMessage = message,
            };


        private EsScheduleCycleLogDetailDM ExecuteTransferCore(
            ESDbTransferMappingDM mapping,
            string secretKey,
            string secretIV)
        {
            try
            {
                // 取得欄位對應設定
                var columns = GetColumns(mapping.TransferMappingCode);
                if (columns == null || columns.Count == 0)
                    return ErrorResult("找不到欄位對應設定");

                // 取得來源資料庫設定
                var srcDbDao = _unitOfWork.Repository<IESDbTransferDAO>();
                var srcDbEntity = srcDbDao.FindByPropertys(new Dictionary<string, object>
                {
                    { nameof(ESDbTransferEntity.TransferCode), mapping.SrcDbTransferCode }
                });
                if (srcDbEntity == null)
                    return ErrorResult("找不到來源資料庫設定");
                var srcDbConfig = new ESDbTransferDM
                {
                    TransferCode = srcDbEntity.TransferCode,
                    TransferName = srcDbEntity.TransferName,
                    DbType = srcDbEntity.DbType,
                    DbHost = srcDbEntity.DbHost,
                    DbPort = srcDbEntity.DbPort,
                    DbName = srcDbEntity.DbName,
                    DbUser = srcDbEntity.DbUser,
                    DbPassword = srcDbEntity.DbPassword
                };

                // 取得目的資料庫設定
                var dstDbEntity = srcDbDao.FindByPropertys(new Dictionary<string, object>
                {
                    { nameof(ESDbTransferEntity.TransferCode), mapping.DstDbTransferCode }
                });
                if (dstDbEntity == null)
                    return ErrorResult("找不到目的資料庫設定");
                var dstDbConfig = new ESDbTransferDM
                {
                    TransferCode = dstDbEntity.TransferCode,
                    TransferName = dstDbEntity.TransferName,
                    DbType = dstDbEntity.DbType,
                    DbHost = dstDbEntity.DbHost,
                    DbPort = dstDbEntity.DbPort,
                    DbName = dstDbEntity.DbName,
                    DbUser = dstDbEntity.DbUser,
                    DbPassword = dstDbEntity.DbPassword
                };

                // 建立執行器並執行
                var executor = new ESDbTransferExecutor(
                    _unitOfWork,
                    mapping,
                    columns,
                    srcDbConfig,
                    dstDbConfig,
                    secretKey,
                    secretIV
                );

                return executor.Execute();
            }
            catch (Exception ex)
            {
                return ErrorResult($"執行傳輸時發生錯誤: {ex.Message}");
            }
        }


        public void CheckExist(ESDbTransferMappingDM dm)
        {
            if(dm.Id == Guid.Empty)
            {
                IESDbTransferMappingDAO dao = _unitOfWork.Repository<IESDbTransferMappingDAO>();
                var entity = dao.FindByPropertys(new Dictionary<string, object>
                {
                    { nameof(ESDbTransferMappingEntity.TransferMappingCode), dm.TransferMappingCode },
                    { nameof(ESDbTransferMappingEntity.Status), StatusEnum.Enabled.ToInt() }
                });
                if (entity != null)
                {
                    GetMessage().SetAlert("此資料表轉檔設定代碼已存在");
                }
            }
        }
    }
}
