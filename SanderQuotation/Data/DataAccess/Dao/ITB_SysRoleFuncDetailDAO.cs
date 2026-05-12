using Core.Utility.Base.Data;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.DataAccess.Dao
{

    public interface ITB_SysRoleFuncDetailDAO : Core.Utility.Base.Data.GuidId.IBaseDAO<TB_SysRoleFuncDetailEntity>
    {
        void DeleteByRoleId(Guid id);
        List<TB_SysRoleFuncDetailEntity> FindListByRoleId(Guid id);
        /// <summary>
        /// 根據帳號查詢權限資料
        /// </summary>
        /// <param name="memberAccount">帳號</param>
        /// <returns></returns>
        List<TB_SysFuncDetailEntity> FindListByMemberAccount(string memberAccount);
    }
}
