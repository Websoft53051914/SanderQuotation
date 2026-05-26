using AutoMapper;
using Business.Common;
using Business.DomainModel;
using Const;
using Core.Utility.Helper.DB;
using Core.Utility.Helper.DB.Entity;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using static Const.Enums;

namespace Business.BusinessLogic
{
    public class AILogBL : BaseProjectBL
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="unitOfWork">工作單元</param>
        public AILogBL(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

            MapperConfiguration cfg = new(c =>
            {
                c.CreateMap<AILogDM, AILogEntity>().ReverseMap();
                c.AllowNullCollections = true;
            });
            _mapper = cfg.CreateMapper();
        }

        public AILogBL(IUnitOfWork unitOfWork, SessionVO sessionVO) : this(unitOfWork)
        {
            base.SessionVO = sessionVO;
        }

        private IAILogDAO aILogDAO;
        private IAILogDAO GetDAO()
        {
            aILogDAO ??= _unitOfWork.Repository<IAILogDAO>();
            return aILogDAO;
        }


        public void Insert(AILogDM dm)
        {
            AILogEntity entity = _mapper.Map<AILogEntity>(dm);
            entity.CreatedBy = UserInfo?.UserAccount??"";
            entity.UpdatedBy = UserInfo?.UserAccount??"";
            entity.Account = UserInfo?.UserAccount ?? "";
            entity.UpdatedAt = base.now;
            entity.CreatedAt = base.now;
            entity.Status = (int)StatusEnum.Enabled;
            entity.LogTime = base.now;
            GetDAO().Insert(entity);
        }



       
    }
}
