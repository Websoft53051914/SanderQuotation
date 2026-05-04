namespace ViewModel
{
    public class ControlLogVM : BaseVM
    {
        /// <summary>
        /// 項次
        /// </summary>
        public string No { get; set; } = "";
        /// <summary>
        /// 紀錄時間
        /// </summary>
        public string LogTime { get; set; } = "";
        /// <summary>
        /// 操作者姓名
        /// </summary>
        public string Name { get; set; } = "";
        /// <summary>
        /// 操作帳號
        /// </summary>
        public string Account { get; set; } = "";
        /// <summary>
        /// 狀態
        /// </summary>
        //public string Status { get; set; } = "";
        /// <summary>
        /// 狀態(描述)
        /// </summary>
        public string StatusDescription { get; set; } = "";
        public string ControllerName { get; set; } = "";
        public string ActionName { get; set; } = "";


        /// <summary>
        /// 資料ID
        /// </summary>
        public Guid? DataId { set; get; }
        /// <summary>
        /// 0:查询,1:新增,2:修改,3:删除
        /// </summary>
        public int Action { set; get; }


        public string StatusName { set; get; }
        public string ActionStr { set; get; }


    }
}
