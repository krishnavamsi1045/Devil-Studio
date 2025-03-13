using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DevilStudio.Models
{
    public class ConnectedAccount
    {
        [Key]
        public int Id { get; set; }
        [ForeignKey("UserDetail")]
        public int UserDetailId { get; set; }
        public UserDetail? UserDetail { get; set; }
        public int ProviderType { get; set; }
        public string? AccessToken { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string? Email { get; set; }
        public string? UserName { get; set; }
    }
    public class ConnAccount
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Provider { get; set; }
        public string? Logo { get; set; }
        public DateTime Date { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
    }
}
