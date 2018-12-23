using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TinyBlog2.Areas.Identity.Data;

namespace TinyBlog2.Models
{
    public class UploadFile
    {
        public int Id { get; set; }
        public TinyBlog2User User { get; set; }
        public byte[] md5 { get; set; }
        public string Uri { get; set; }
    }
}
