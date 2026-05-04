namespace Business.DomainModel
{
    public class ESDbTransferMappingColumnDM : BaseDM
    {
        // FK 指向主表
        public string TransferMappingCode { set; get; }

        // 來源欄位名稱
        public string SrcColumnName { set; get; }

        // 目標欄位名稱
        public string DstColumnName { set; get; }

        // 是否加密
        public bool IsEncrypt { set; get; }

        // 是否為主鍵
        public bool IsPrimaryKey { set; get; }
    }
}
