using AutoMapper;
using Business.Common;
using Business.DomainModel;
using Core.Utility.Helper.DB;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Const;
using static Const.Enums;
using Core.Utility.Extensions;

namespace Business.BusinessLogic
{
    public class EmbeddedHistoryFileBL:BaseProjectBL
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="unitOfWork">工作單元</param>
        public EmbeddedHistoryFileBL(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

            MapperConfiguration cfg = new(c =>
            {
                c.AllowNullCollections = true;
                c.CreateMap<EmbeddedHistoryFileEntity, EmbeddedHistoryFileDM>().ReverseMap();
            });
            _mapper = cfg.CreateMapper();
        }


        private IEmbeddedHistoryFileDAO? _dao = null;

        /// <summary>
        /// 取得 DAO 實例
        /// </summary>
        public IEmbeddedHistoryFileDAO GetDAO()
        {
            _dao ??= _unitOfWork.Repository<IEmbeddedHistoryFileDAO>();

            return _dao;
        }

    }
}
