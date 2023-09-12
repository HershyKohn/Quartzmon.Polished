using System.IO;

namespace Quartzmon.Models
{
    public class FormFile
    {
#if ( NETSTANDARD || NETCOREAPP )
		readonly Microsoft.AspNetCore.Http.IFormFile _file;
        public FormFile(Microsoft.AspNetCore.Http.IFormFile file) => _file = file;

        public Stream GetStream() => _file.OpenReadStream();
#endif

        public byte[] GetBytes()
        {
            using (var stream = new MemoryStream())
            {
                GetStream().CopyTo(stream);
                return stream.ToArray();
            }
        }
    }
}
