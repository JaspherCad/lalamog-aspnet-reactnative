using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyApi.DTOs
{
    public class ImageDto
    {
    // {
    //                 message = "Profile image uploaded successfully",
    //                 imageUrl = fileUrl,
    //                 fileName = uniqueFileName,
    //                 fileSize = file.Length
    // }
        public string Message { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
}
}