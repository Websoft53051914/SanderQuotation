namespace backend.AI.VO
{
    public class FileUploadInfo
    {
        public Stream fileStream { get; set; }
        public long fileSize { get; set; }
        public string fileMimeType { get; set; }
        public string fileName { get; set; }
    }
}
