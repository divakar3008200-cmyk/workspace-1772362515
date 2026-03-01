using System;
using System.ComponentModel.DataAnnotations;

namespace dotnetapp.Models
{
    public class MaintenanceRecord
    {
        [Key]
        public int MaintenanceRecordId { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; } = string.Empty;
        public int AircraftId { get; set; }
        public Aircraft? Aircraft { get; set; }
    }
}