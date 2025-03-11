using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelManager.Model
{
    public class GoogleDriveFile
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? MimeType { get; set; }
        public string? Description { get; set; }
        public string? CreatedTime { get; set; }
        public string? ModifiedTime { get; set; }
        public bool IsFolder { get; set; }
    }
    public class Account
    {
        public string? Provider { get; set; }
        public string? Email { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
    }
}