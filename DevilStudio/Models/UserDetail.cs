using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevilStudio.Models
{
    public class UserDetail
    {
        [Key]
        public int Id { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; } // Added field
        public string? LastName { get; set; }  // Added field
        public string? MobileNumber { get; set; }
        public int ProviderType { get; set; }
        public int LastProviderType { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;  // Default value
        public DateTime ModifiedDate { get; set; } = DateTime.Now; // Default value
        public string? PlanType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool? IsActive { get; set; }
        public bool? InCompleteMobileNumber { get; set; } // Tracking Mobile number 
        public bool? CookiesChk { get; set; }
        public bool? Personalchk { get; set; }
        public bool? Advertisementchk { get; set; }
        public bool? Marketingchk { get; set; }
        public string? RefreshToken { get; set; }
        public string? AccessToken { get; set; }
        public string? Country { get; set; }

        public ICollection<ConnectedAccount> ConnectedAccounts { get; set; }

    }
}