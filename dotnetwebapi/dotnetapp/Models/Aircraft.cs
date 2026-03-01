using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace dotnetapp.Models
{
    public class Aircraft
    {
        [Key]
        public int AircraftId { get; set; }
        [Required]
        public string RegistrationNumber { get; set; } = string.Empty;
        [Required]
        public string Model { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public int Capacity { get; set; }

        public List<MaintenanceRecord> MaintenanceRecords { get; set; } = new();
    }
}