using AutoMapper;
using Business.Common;
using Business.DomainModel;
using CommonClass.Model;
using CommonClass.Models;
using Const;
using Core.Utility.Extensions;
using Core.Utility.Helper.DB;
using Core.Utility.Helper.DB.Entity;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;
using Sander.Platform.DbTransfer;
using static Const.Enums;

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

        private IEsScheduleCycleDbTransferDAO? esScheduleCycleDbTransferDAO = null;
        private IEsScheduleCycleDbTransferDAO GetScheduleCycleDbTransferDAO()
        {
            esScheduleCycleDbTransferDAO ??= _unitOfWork.Repository<IEsScheduleCycleDbTransferDAO>();
            return esScheduleCycleDbTransferDAO;
        }
    }

    public partial class ESDbTransferMappingBL
    {
        public PageResult<ESDbTransferMappingDM> GetPageList(PageEntity pageEntity, SearchVO searchVO)
        {
            var result = GetDAO().FindPageList(pageEntity, searchVO);
            var list = GetScheduleCycleDbTransferDAO().GetListByFilter(new SearchVO
            {
                TransferCodeIn = result.Results.Select(r => r.TransferMappingCode).ToList()
            });

            var dms = result.Results.Select(dto => mapper.Map<ESDbTransferMappingDM>(dto)).ToList();
            var scheduleCycleDict = list.GroupBy(x => x.TransferCode).ToDictionary(g => g.Key, g => g.Select(s => new EsScheduleCycleDM()
            {
                ScheduleCycleCode = s.ScheduleCycleCode
            }).ToList());
            for (int i = 0; i < dms.Count; i++)
            {
                if (scheduleCycleDict.ContainsKey(dms[i].TransferMappingCode))
                {
                    dms[i].EsScheduleCycleDMs = scheduleCycleDict[dms[i].TransferMappingCode];
                }
            }

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
            var pkList = dao.FindByPkList(ids);
            pkList.ForEach(x =>
            {
                x.UpdatedBy = UserInfo.UserAccount;
                x.UpdatedAt = base.now;
                x.Status = StatusEnum.Cancel.ToInt();
                dao.Update(x);
            });

            IESDbTransferMappingColumnDAO eSDbTransferMappingColumnDAO = _unitOfWork.Repository<IESDbTransferMappingColumnDAO>();
            eSDbTransferMappingColumnDAO.FindListByFilter(new SearchVO
            {
               TransferMappingCodeIn = pkList.Select(x => x.TransferMappingCode).ToList(),
            }).ForEach(x =>
            {
                x.UpdatedBy = UserInfo.UserAccount;
                x.UpdatedAt = base.now;
                x.Status = StatusEnum.Cancel.ToInt();
                eSDbTransferMappingColumnDAO.Update(x);
            });

            dao.DbHelper.Commit();
        }

        public ESDbTransferMappingDM Get(Guid id)
        {
            IESDbTransferMappingDAO dao = _unitOfWork.Repository<IESDbTransferMappingDAO>();
            var entity = dao.FindByPk(id);
            var newDM = mapper.Map<ESDbTransferMappingDM>(entity);
            return newDM;
        }

        public List<DbTransferOptionItem> GetDbTransferOptions()
        {
            return GetDAO().FindListByProperty(nameof(ESDbTransferMappingEntity.Status), StatusEnum.Enabled.ToInt())
                .Select(x => new DbTransferOptionItem
                {
                    Value = x.TransferMappingCode,
                    Text = x.TransferMappingCode
                }).ToList();
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
            entity.UpdatedAt = DateTime.Now;
            entity.UpdatedBy = UserInfo.UserAccount;

            dao.Update(entity);

            // ¥ý§R°£ÂÂÄæ¦ì¹ïÀ³¡A¦A¼g¤J·sªº
            var colDao = _unitOfWork.Repository<IESDbTransferMappingColumnDAO>();
            var oldCols = colDao.FindListByPropertys(new Dictionary<string, object> { { nameof(ESDbTransferMappingColumnEntity.TransferMappingCode), entity.TransferMappingCode },{ nameof(ESDbTransferMappingColumnEntity.Status), StatusEnum.Enabled.ToInt() } });
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
        /// °õ¦æ«ü©wªº¸ê®Æ¶Ç¿é¹ïÀ³³]©w
        /// </summary>
        /// <param name="mappingRowGuid">¶Ç¿é¹ïÀ³³]©wªº RowGuid¡]»P transferMappingCode ¾Ü¤@¨Ï¥Î¡^</param>
        /// <param name="transferMappingCode">¶Ç¿é¹ïÀ³³]©w¥N½X¡]»P mappingRowGuid ¾Ü¤@¨Ï¥Î¡^</param>
        /// <param name="secretKey">¥[±Kª÷Æ_¡]¿ï¶ñ¡A­YÄæ¦ì»Ý­n¥[±K«h¥²¶ñ¡^</param>
        /// <param name="secretIV">¥[±K¦V¶q¡]¿ï¶ñ¡A­YÄæ¦ì»Ý­n¥[±K«h¥²¶ñ¡^</param>
        /// <returns>°õ¦æµ²ªG</returns>
        public EsScheduleCycleLogDetailDM ExecuteTransfer(
            Guid? mappingRowGuid = null,
            string transferMappingCode = null,
            string secretKey = "",
            string secretIV = "")
        {
            try
            {
                if (!mappingRowGuid.HasValue && string.IsNullOrEmpty(transferMappingCode))
                    return ErrorResult("¥²¶·´£¨Ñ mappingRowGuid ©Î transferMappingCode ¨ä¤¤¤§¤@");

                ESDbTransferMappingDM mapping = null;

                if (mappingRowGuid.HasValue)
                {
                    var mappingResponse = GetById(mappingRowGuid.Value);
                    if (mappingResponse == null)
                        return ErrorResult("§ä¤£¨ì«ü©wªº¶Ç¿é¹ïÀ³³]©w");
                    mapping = mappingResponse;
                }
                else
                {
                    var entity = GetDAO().FindByPropertys(new Dictionary<string, object>
                    {
                        { nameof(ESDbTransferMappingEntity.TransferMappingCode), transferMappingCode }
                    });

                    if (entity == null)
                        return ErrorResult("§ä¤£¨ì«ü©wªº¶Ç¿é¹ïÀ³³]©w");

                    mapping = mapper.Map<ESDbTransferMappingDM>(entity);
                }

                return ExecuteTransferCore(mapping, secretKey, secretIV);
            }
            catch (Exception ex)
            {
                return ErrorResult($"°õ¦æ¶Ç¿é®Éµo¥Í¿ù»~: {ex.Message}");
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
                // ¨ú±oÄæ¦ì¹ïÀ³³]©w
                var columns = GetColumns(mapping.TransferMappingCode);
                if (columns == null || columns.Count == 0)
                    return ErrorResult("§ä¤£¨ìÄæ¦ì¹ïÀ³³]©w");

                // ¨ú±o¨Ó·½¸ê®Æ®w³]©w
                var srcDbDao = _unitOfWork.Repository<IESDbTransferDAO>();
                var srcDbEntity = srcDbDao.FindByPropertys(new Dictionary<string, object>
                {
                    { nameof(ESDbTransferEntity.TransferCode), mapping.SrcDbTransferCode }
                });
                if (srcDbEntity == null)
                    return ErrorResult("§ä¤£¨ì¨Ó·½¸ê®Æ®w³]©w");
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

                // ¨ú±o¥Øªº¸ê®Æ®w³]©w
                var dstDbEntity = srcDbDao.FindByPropertys(new Dictionary<string, object>
                {
                    { nameof(ESDbTransferEntity.TransferCode), mapping.DstDbTransferCode }
                });
                if (dstDbEntity == null)
                    return ErrorResult("§ä¤£¨ì¥Øªº¸ê®Æ®w³]©w");
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

                // «Ø¥ß°õ¦æ¾¹¨Ã°õ¦æ
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
                return ErrorResult($"°õ¦æ¶Ç¿é®Éµo¥Í¿ù»~: {ex.Message}");
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
