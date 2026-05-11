using CommonClass.Model;
using Const;
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
    public interface ITB_AccountDAO : Core.Utility.Base.Data.GuidId.IBaseDAO<TB_AccountEntity>
    {
        void Enable(Guid id, int enable);
        AccountDTO FindByAccount(string memberAccount);
        TB_AccountEntity FindByAccountName(string accountName);
        AccountDTO FindDTOByPk(Guid id);
        PageResult<AccountDTO> FindPageList(ListPageEntity pageEntity, AccountDTO condition, bool isExpaied);
        List<AccountDTO> GetBackMembersByRole(Guid roleId);
        List<AccountDTO> GetBackMembersByRole(List<Guid> roleId);
        List<AccountDTO> GetLast3PWD(string memberAccount);
        PageResult<AccountDTO> GetUnRolePageList(PageEntity pageEntity, AccountDTO condition, bool isExpaied);

    }
}
