using System.ComponentModel.DataAnnotations.Schema;

namespace AppInfoController.Models
{
    public class LastHitByUser
    {
        public long Id { get; set; }

        public string? User { get; set; }

        public string? Date { get; set; }

        public string? Name { get; set; }

        public string? MobileNo { get; set; }

        public string? City { get; set; }

        public string? Address { get; set; }

        [NotMapped]
        public bool Inactive { get; set; }

        public string? AllowedUserId { get; set; }

        public string? AppVersion { get; set; }
        public string? Summary { get; set; }
    }
}
