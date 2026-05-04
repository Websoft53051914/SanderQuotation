namespace Data.DataAccess.DTO
{
    public class ApplyStatusCountDTO
    {
        public int SigningExpiredCount { get; set; }
        public int SigningNotExpiredCount { get; set; }
        public int ClosedCount { get; set; }
    }
}
