using MimeTypes;

namespace Nacencom.Infrastructure.DataTypes
{
    public class FileDownloadResult
    {
        public string FileName { get; set; } = default!;
        public byte[] FileContents { get; set; } = default!;
        public string ContentType => MimeTypeMap.GetMimeType(FileName);
    }
}
