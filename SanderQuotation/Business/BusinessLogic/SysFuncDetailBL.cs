using AutoMapper;
using Core.Utility.Helper.DB;
using Business.Common;
using Business.DomainModel;
using Data.DataAccess.Dao;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.BusinessLogic
{

    public class SysFuncDetailBL : BaseProjectBL
    {
        IMapper mapper;
        private IUnitOfWork _unitOfWork;

        public SysFuncDetailBL(IUnitOfWork unitOfWork)
        {
            this._unitOfWork = unitOfWork;

            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.AllowNullCollections = true;
                cfg.CreateMap<SysFuncDetailDTO, SysFuncDetailDM>();
                cfg.CreateMap<SysFuncDetailDM, SysFuncDetailDTO>();

                cfg.CreateMap<TB_SysFuncDetailEntity, SysFuncDetailDM>();
                cfg.CreateMap<SysFuncDetailDM, TB_SysFuncDetailEntity>();
            });
            mapper = configuration.CreateMapper();
        }

        public void Edit(SysFuncDetailDM dm)
        {
            // 加入除錯用的檢查點
            System.Diagnostics.Debug.WriteLine($"PerCodes Count: {dm.PerCodes?.Count ?? 0}");
            System.Diagnostics.Debug.WriteLine($"Codes Count: {dm.Codes?.Count ?? 0}");

            if (dm.PerCodes != null)
            {
                foreach (var code in dm.PerCodes)
                {
                    System.Diagnostics.Debug.WriteLine($"PerCode: {code}");
                }
            }

            if (dm.Codes != null)
            {
                for (int i = 0; i < dm.Codes.Count; i++)
                {
                    System.Diagnostics.Debug.WriteLine($"Code[{i}]: {dm.Codes[i]}");
                }
            }

            var dao = this._unitOfWork.Repository<ITB_SysFuncDetailDAO>();
            var dtos = dao.FindListByFuncId(dm.Id);

            if (dm.PerCodes == null)
                dm.PerCodes = new List<string>();

            //差集 DB資料不存在就新增
            var insertList = dm.PerCodes.Except(dtos.Select(s => s.Sequence));
            //var insertList = dm.PerCodes.Where(w => exceptResultDM.Contains(w)).ToList();
            foreach (var item in insertList)
            {
                dao.Insert(new TB_SysFuncDetailEntity()
                {
                    Name = "N",
                    FuncId = dm.Id,
                    Status = 1,
                    PermissionCode = dm.Codes[int.Parse(item) - 1],
                    Sequence = item,
                });
            }

            //交集 存在就修改
            foreach (var item in dm.PerCodes)
            {
                if (dtos.Where(w => w.Sequence == item).Count() > 0)
                {
                    var dto = dtos.Where(w => w.Sequence == item).FirstOrDefault();
                    dto.PermissionCode = dm.Codes[int.Parse(item) - 1];
                    dto.Status = 1;
                    dao.Update(dto);
                }
            }

            //差集 DB多的就做廢
            var exceptResultDTO = dtos.Select(s => s.Sequence).Except(dm.PerCodes);
            var deleteList = dtos.Where(w => exceptResultDTO.Contains(w.Sequence)).ToList();
            foreach (var item in deleteList)
            {
                item.Status = 9;
                dao.Update(item);
            }
            dao.DbHelper.Commit();
        }

        public List<SysFuncDetailDM> GetList()
        {
            var dao = this._unitOfWork.Repository<ITB_SysFuncDetailDAO>();
            var dtos = dao.FindList();
            var dms = mapper.Map<List<SysFuncDetailDM>>(dtos);
            return dms;
        }

        public List<SysFuncDetailDM> GetInfoBySysFuncId(Guid id)
        {
            var dao = this._unitOfWork.Repository<ITB_SysFuncDetailDAO>();
            var dtos = dao.FindListByFuncId(id, 1);
            var dms = mapper.Map<List<SysFuncDetailDM>>(dtos);

            return dms;
        }
    }
}
