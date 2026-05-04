using Core.Utility.Base.Data;
using Core.Utility.Helper.DB.Entity;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Const.Enums;

namespace Data.DataAccess.Dao
{

    public interface ITB_SysFuncDAO : Core.Utility.Base.Data.GuidId.IBaseDAO<TB_SysFuncEntity>
    {
        List<SysFuncDTO> FindList(string memberAccount);
        PageResult<SysFuncDTO> FindPageList(PageEntity pageEntity, SysFuncDTO dto);
        void Enable(Guid id, int enable);
        SysFuncDTO FindByName(string name);
        List<SysFuncDTO> GetList();

        List<SysFuncDTO> GetClassNameList();
        SysFuncDTO GetInfoByFuncId(string funcId);
        SysFuncDTO GetInfo(string url);

        SysFuncDTO GetByFunIdEnum(FuncID funcID);
        List<SysFuncDTO> FindListMenu(string memberAccount);

        /// <summary>
        /// 依帳號取得所有已授權的 PermissionCode 整數清單
        /// </summary>
        List<int> FindPermissionCodesByAccount(string memberAccount);
    }
}
