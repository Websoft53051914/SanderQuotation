using Core.Utility.Base.Data;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.DataAccess.Entity
{
    [Table("TB_ControlLog")]
    public class TB_ControlLogEntity : SP_BaseEntity
    {
        /// <summary>
        /// 紀錄時間
        /// </summary>
        public DateTime LogTime { set; get; }
        /// <summary>
        /// IP
        /// </summary>
        public string? IP { set; get; }
        /// <summary>
        /// 帳號
        /// </summary>
        public string? Account { set; get; }
        /// <summary>
        /// 姓名
        /// </summary>
        public string? Name { set; get; }
        /// <summary>
        /// Exception內容
        /// </summary>
        public string? Exception { set; get; }
        /// <summary>
        /// ControllerName
        /// </summary>
        public string? ControllerName { set; get; }
        /// <summary>
        /// ActionName
        /// </summary>
        public string? ActionName { set; get; }
        /// <summary>
        /// 資料ID
        /// </summary>
        public Guid? DataId { set; get; }
        /// <summary>
        /// 0:查询,1:新增,2:修改,3:删除
        /// </summary>
        public int Action { set; get; }
    }
}
