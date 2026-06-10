using Microsoft.AspNetCore.Identity;

namespace UserRoleApp.Models
{
    public class Users : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string? Department { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        public bool IsDeletionPending { get; set; } = false;
        public string? DeletionRequestReason { get; set; }
    }
}
