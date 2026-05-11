using CommonClass.Model;
using Core.Utility.Base.Data;
using Core.Utility.Helper.DB.Entity;
using Data.DataAccess.Entity;

namespace Data.DataAccess.Dao
{
    public interface ITB_ControlLogDAO : Core.Utility.Base.Data.GuidId.IBaseDAO<TB_ControlLogEntity>

    {
        PageResult<TB_ControlLogEntity> GetPageList(PageEntity pageEntity,int? status, string keyword, DateTime? dateGte, DateTime? dateLte);

        void DeleteOldLog(int days);
    }
}
