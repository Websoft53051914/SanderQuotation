using AutoMapper;
using Business.Common;
using Business.DomainModel;
using CommonClass.Model;
using CommonClass.Models;
using Core.Utility.Helper.DB;
using Core.Utility.Helper.DB.Entity;
using Core.Utility.Utility;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;
using DocumentFormat.OpenXml.Bibliography;
using Microsoft.Graph.Models.Security;
using static Const.Enums;

namespace Business.BusinessLogic
{
    public class LogBL : BaseProjectBL
    {
        private readonly IUnitOfWork _unitOfWork;
        IMapper mapper;

        public LogBL(IUnitOfWork unitOfWork)
        {
            this._unitOfWork = unitOfWork;

            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.AllowNullCollections = true;
                cfg.CreateMap<TB_ControlLogEntity, ControlLogDM>().ReverseMap();
            });

            mapper = configuration.CreateMapper();
        }

        public LogBL(IUnitOfWork unitOfWork, SessionVO sessionVO) : this(unitOfWork)
        {
            base.SessionVO = sessionVO;
        }

        public string? GetException(Guid id)
        {
            ITB_ControlLogDAO dao = _unitOfWork.Repository<ITB_ControlLogDAO>();
            var entity = dao.FindByPk(id);
            return entity?.Exception;
        }

        public PageResult<ControlLogDM> GetPageList(PageEntity pageEntity, string keyword, DateTime? dateGte, DateTime? dateLte, int? status)
        {
            ITB_ControlLogDAO dao = _unitOfWork.Repository<ITB_ControlLogDAO>();
            var pageResult = dao.GetPageList(pageEntity, status, keyword, dateGte, dateLte);

            return new PageResult<ControlLogDM>()
            {
                CurrentPage = pageResult.CurrentPage,
                DataCount = pageResult.DataCount,
                PageDataSize = pageResult.PageDataSize,
                Results = mapper.Map<List<ControlLogDM>>(pageResult.Results)
            };
        }

        public Guid InsertLog(ControlLogDM dm)
        {
            try
            {
                var account = "tempAcc";
                var accountName = "tempAcc";

                if (SessionVO != null)
                {
                    if (!string.IsNullOrEmpty(SessionVO.Account))
                    {
                        account = SessionVO.Account;
                    }
                    if (!string.IsNullOrEmpty(SessionVO.AccountName))
                    {
                        accountName = SessionVO.AccountName;
                    }
                }

                ITB_ControlLogDAO dao = _unitOfWork.Repository<ITB_ControlLogDAO>();
                IMapper mapperEntity = CommonUtility.CreateMapper<TB_ControlLogEntity, ControlLogDM>();
                var entity = mapperEntity.Map<TB_ControlLogEntity>(dm);
                entity.LogTime = DateTime.Now;
                entity.Account = account;
                entity.Name = accountName;

                if (string.IsNullOrEmpty(entity.IP))
                {
                    entity.IP = base.SessionVO.IP ?? string.Empty;
                }

                var insertedEntity = dao.InsertAction(entity);
                dao.DbHelper.Commit();
                Guid id = insertedEntity.Id;
                return id;
            }
            catch (Exception ex)
            {
                return Guid.Empty;
            }
        }
    }
}
