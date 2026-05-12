namespace ViewModel
{
    public class ESDbTransferVM
    {
        public Guid Id { set; get; }

        public string Status { set; get; }

        public string Type { set; get; }

        public string SortNo { set; get; }

        public string Priority { set; get; }

        public DateTime? CreatedAt { set; get; }

        public DateTime? UpdatedAt { set; get; }

        public string CreatedBy { set; get; }

        public string UpdatedBy { set; get; }

        public string TransferCode { set; get; }

        public string TransferName { set; get; }

        public string DbType { set; get; }

        public string DbHost { set; get; }

        public string DbPort { set; get; }

        public string DbName { set; get; }

        public string DbUser { set; get; }

        public string DbPassword { set; get; }

        public string Description { set; get; }

        public int No { get; set; }

        public string DbTypeDescription { set; get; }
        /// <summary>
        /// 可否刪除(沒有任何資料表參照才可以刪除)
        /// </summary>
        public bool CanDelete { set; get; }
    }
}
