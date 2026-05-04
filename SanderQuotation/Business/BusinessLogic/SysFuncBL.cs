using AutoMapper;
using Business.Common;
using Business.DomainModel;
using Core.Utility.Extensions;
using Core.Utility.Helper.DB;
using Core.Utility.Helper.DB.DBEntityAttribute;
using Core.Utility.Helper.DB.Entity;
using Core.Utility.Utility;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;
using Microsoft.Graph.Models;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Const.Enums;

namespace Business.BusinessLogic
{
    public class SysFuncBL : BaseProjectBL
    {
        IMapper mapper;
        private IUnitOfWork _unitOfWork;

        public SysFuncBL(IUnitOfWork unitOfWork)
        {
            this._unitOfWork = unitOfWork;

            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.AllowNullCollections = true;
                cfg.CreateMap<SysFuncDTO, SysFuncDM>();
                cfg.CreateMap<SysFuncDM, SysFuncDTO>();

                cfg.CreateMap<TB_SysFuncEntity, SysFuncDM>();
                cfg.CreateMap<SysFuncDM, TB_SysFuncEntity>();

                cfg.CreateMap<TB_SysFuncClassEntity, SysFuncClassDM>();
                cfg.CreateMap<SysFuncClassDM, TB_SysFuncClassEntity>();


                cfg.CreateMap<TB_SysFuncDetailEntity, SysFuncDetailDM>();
                cfg.CreateMap<SysFuncDetailDM, TB_SysFuncDetailEntity>();

            });

            mapper = configuration.CreateMapper();
        }

        public List<SysFuncDM> GetList()
        {
            ITB_SysFuncDAO dao = _unitOfWork.Repository<ITB_SysFuncDAO>();

            var dtos = dao.GetList();

            var dms = mapper.Map<List<SysFuncDM>>(dtos);

            return dms;

        }

        public List<SysFuncDM> GetClassNameList()
        {
            ITB_SysFuncDAO dao = _unitOfWork.Repository<ITB_SysFuncDAO>();

            var dtos = dao.GetClassNameList();

            var dms = mapper.Map<List<SysFuncDM>>(dtos);

            return dms;

        }

        public PageResult<SysFuncDM> GetPageList(PageEntity pageEntity, SysFuncDM dm)
        {
            ITB_SysFuncDAO dao = _unitOfWork.Repository<ITB_SysFuncDAO>();
            var dto = mapper.Map<SysFuncDTO>(dm);
            dto.FilterFuncClassId = dm.FuncClassId;
            dto.FilterStatus = dm.Status;

            PageResult<SysFuncDTO> dtos = dao.FindPageList(pageEntity, dto);

            //ISysFuncClassDAO funcClassDao = _unitOfWork.Repository<ISysFuncClassDAO>();
            //var SysFuncClassDtos = funcClassDao.FindList(null);

            //var dic = SysFuncClassDtos.ToDictionary(d => d.Id);
            //foreach (var item in dtos.Results)
            //{
            //    if (dic.ContainsKey(item.FUNCCLASSID))
            //        item.ClassName = dic[item.FUNCCLASSID].ClassName;
            //}

            PageResult<SysFuncDM> dms = new PageResult<SysFuncDM>()
            {
                CurrentPage = dtos.CurrentPage,
                DataCount = dtos.DataCount,
                PageDataSize = dtos.PageDataSize,
                Results = mapper.Map<List<SysFuncDM>>(dtos.Results)
            };

            return dms;
        }

        public SysFuncDM GetInfo(Guid id)
        {
            ITB_SysFuncDAO dao = _unitOfWork.Repository<ITB_SysFuncDAO>();
            var entity = dao.FindByPk(id);
            var dm = mapper.Map<SysFuncDM>(entity);
            return dm;
        }

        public void Enable(Guid id, int enable)
        {
            ITB_SysFuncDAO dao = _unitOfWork.Repository<ITB_SysFuncDAO>();
            dao.Enable(id, enable);
        }

        public List<SysFuncClassDM> FindClassList()
        {
            ITB_SysFuncClassDAO dao = _unitOfWork.Repository<ITB_SysFuncClassDAO>();
            var entities = dao.FindListByProperty(nameof(TB_SysFuncClassEntity.Status), StatusEnum.Enabled.ToInt());
            var dms = mapper.Map<List<SysFuncClassDM>>(entities);
            return dms;
        }

        public Guid Create(SysFuncDM dm)
        {
            ITB_SysFuncDAO dao = _unitOfWork.Repository<ITB_SysFuncDAO>();
            var entity = new TB_SysFuncEntity()
            {
                Name = dm.Name,
                Memo = dm.Memo,
                Sequence = dm.Sequence,
                Status = dm.Status,
                Url = dm.URL,
                FuncClassId = dm.FuncClassId ?? Guid.Empty,
            };

            dao.Insert(entity);
            dao.DbHelper.Commit();

            dm.Id = entity.Id;
            return entity.Id;
        }

        public void Edit(SysFuncDM dm)
        {
            ITB_SysFuncDAO dao = _unitOfWork.Repository<ITB_SysFuncDAO>();
            var entity = dao.FindByPk(dm.Id);
            entity.Name = dm.Name;
            entity.Memo = dm.Memo;
            entity.Sequence = dm.Sequence;
            entity.Status = dm.Status;
            entity.Url = dm.URL;
            entity.FuncClassId = dm.FuncClassId ?? Guid.Empty;
            entity.UpdatedAt = DateTime.Now;
            dao.Update(entity);
            dao.DbHelper.Commit();
        }

        public SysFuncDM CheckExist(string name)
        {
            ITB_SysFuncDAO dao = _unitOfWork.Repository<ITB_SysFuncDAO>();
            var entity = dao.FindByName(name);
            var dm = mapper.Map<SysFuncDM>(entity);
            return dm;
        }

        public void Delete(Guid id)
        {
            ITB_SysFuncDAO dao = _unitOfWork.Repository<ITB_SysFuncDAO>();
            var entity = dao.FindByPk(id);
            entity.Status = 9;
            dao.Update(entity);
            dao.DbHelper.Commit();
        }

        public SysFuncDM GetInfo(string url)
        {
            ITB_SysFuncDAO dao = _unitOfWork.Repository<ITB_SysFuncDAO>();
            TB_SysFuncEntity entity = dao.GetInfo(url);
            SysFuncDM dm = mapper.Map<SysFuncDM>(entity);
            return dm;
        }

        /// <summary>
        /// 根據功能Enum PermissionCode 取得 sysfunc.id & sysfuncClass.ClassName
        /// </summary>
        /// <param name="funcID"></param>
        /// <returns></returns>
        public SysFuncDM GetByFunIdEnum(FuncID funcID)
        {
            ITB_SysFuncDAO dao = _unitOfWork.Repository<ITB_SysFuncDAO>();
            var entity = dao.GetByFunIdEnum(funcID);
            var dm = mapper.Map<SysFuncDM>(entity);
            return dm;
        }

        public List<SysFuncDM> GetAll()
        {
            ITB_SysFuncDAO dao = _unitOfWork.Repository<ITB_SysFuncDAO>();
            var entities = dao.FindListByPropertys(new Dictionary<string, object>() { { "Status", 1 } });

            var dms = mapper.Map<List<SysFuncDM>>(entities);

            if (dms.Count > 0)
            {
                ITB_SysFuncClassDAO daoClass = _unitOfWork.Repository<ITB_SysFuncClassDAO>();
                var classes = daoClass.FindByAll();
                var dic = classes.ToDictionary(k => k.Id, v => v.ClassName);

                foreach (var item in dms)
                {
                    if (dic.ContainsKey(item.FuncClassId ?? Guid.Empty))
                        item.ClassName = dic[item.FuncClassId ?? Guid.Empty];
                }

                dms = dms.OrderBy(s => s.ClassName).ThenBy(s => s.Name).ToList();
            }

            return dms;
        }


        //public T FindByPropertys(Dictionary<string, object> propertyValue, IEnumerable<string> exceptSelectPropertys = null)
        //{
        //    ITB_SysFuncDAO dao = _unitOfWork.Repository<ITB_SysFuncDAO>();
        //    IEnumerable<string> exceptSelectPropertys2 = exceptSelectPropertys;
        //    Type typeFromHandle = typeof(T);
        //    DBEntityAttributeHelper entityAttrInfo = dao.DbHelper.GetEntityAttrInfo(typeFromHandle);
        //    StringBuilder stringBuilder = new StringBuilder();
        //    Dictionary<string, object> dictionary = new Dictionary<string, object>();
        //    if (propertyValue.Count == 0)
        //    {
        //        throw new InvalidOperationException();
        //    }

        //    foreach (KeyValuePair<string, object> item in propertyValue)
        //    {
        //        if (typeof(T).GetProperty(item.Key) == null)
        //        {
        //            throw new InvalidOperationException();
        //        }

        //        if (item.Value == null)
        //        {
        //            StringBuilder stringBuilder2 = stringBuilder;
        //            StringBuilder stringBuilder3 = stringBuilder2;
        //            StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder2);
        //            handler.AppendLiteral("AND tw.");
        //            handler.AppendFormatted(item.Key);
        //            handler.AppendLiteral(" IS NULL ");
        //            stringBuilder3.Append(ref handler);
        //        }
        //        else
        //        {
        //            StringBuilder stringBuilder2 = stringBuilder;
        //            StringBuilder stringBuilder4 = stringBuilder2;
        //            StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(12, 2, stringBuilder2);
        //            handler.AppendLiteral("AND tw.");
        //            handler.AppendFormatted(item.Key);
        //            handler.AppendLiteral(" = @");
        //            handler.AppendFormatted(item.Key);
        //            handler.AppendLiteral(" ");
        //            stringBuilder4.Append(ref handler);
        //            dictionary.Add(item.Key, item.Value);
        //        }
        //    }

        //    string value = "*";
        //    if (exceptSelectPropertys2 != null)
        //    {
        //        value = string.Join(',', from x in entityAttrInfo.Props
        //                                 where !exceptSelectPropertys2.Contains(x.Name)
        //                                 select x.Name);
        //    }

        //    string sQLScript = $"select  {value} from {entityAttrInfo.Table()} tw where 1 = 1 {stringBuilder}";
        //    return dao.DbHelper.Find<T>(sQLScript, dictionary);
        //}
    }
}
