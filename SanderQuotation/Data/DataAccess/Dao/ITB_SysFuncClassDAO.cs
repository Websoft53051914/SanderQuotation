
using Core.Utility.Base.Data;
using Core.Utility.Helper.DB.Entity;
using Data.DataAccess.DTO;
using Data.DataAccess.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.DataAccess.Dao
{

    public interface ITB_SysFuncClassDAO : Core.Utility.Base.Data.GuidId.IBaseDAO<TB_SysFuncClassEntity>
    {
        List<TB_SysFuncClassEntity> FindList();
        SysFuncClassDTO FindByName(string name);
        PageResult<SysFuncClassDTO> FindPageList(PageEntity pageEntity, SysFuncClassDTO dto);
        TB_SysFuncClassEntity FindFuncClass(string url);
    }
}
