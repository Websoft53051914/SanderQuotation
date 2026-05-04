using static Const.Enums;

namespace Business.DomainModel
{
    public class BaseDM
    {
        public Guid Id { set; get; }
        public int? Status { set; get; }
        public string Type { set; get; }
        public string SortNo { set; get; }
        public string Priority { set; get; }
        /// <summary>
        /// 項次
        /// </summary>
        public long RowNum { get; set; }

        public string? CreatedBy { set; get; }

        public DateTime? CreatedAt { set; get; }

        public string? UpdatedBy { set; get; }

        public DateTime? UpdatedAt { set; get; }




        /// <summary>
        /// 儲存動作
        /// </summary>
        public SaveActionEnum SaveActionEnum { get; set; } = SaveActionEnum.None;
    }
}
