namespace Business.DomainModel
{
    public class ESDbTransferDM : BaseDM
    {
        public string TransferCode { set; get; }

        public string TransferName { set; get; }

        public string DbType { set; get; }

        public string DbHost { set; get; }

        public string DbPort { set; get; }

        public string DbName { set; get; }

        public string DbUser { set; get; }

        public string DbPassword { set; get; }

        public string Description { set; get; }

        //ÃB¥~Äæ¦ì
        public List<ESDbTransferMappingDM> DbTransferMappingDMs { set; get; } = new();

        public List<EsFileTransferMappingColumnDM> FileTransferMappingColumnDMs { set; get; } = new();
    }
}
