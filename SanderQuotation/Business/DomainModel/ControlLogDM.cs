namespace Business.DomainModel
{
    public class ControlLogDM : BaseDM
    {
        public DateTime LogTime { set; get; }
        public string? IP { set; get; }
        public string? Account { set; get; }
        public string? Name { set; get; }
        public string? Exception { set; get; }
        public string? ControllerName { set; get; }
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
