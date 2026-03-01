using Microsoft.AspNetCore.Mvc;
using dotnetapp.Data;
using dotnetapp.Models;
using dotnetapp.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace dotnetapp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MaintenanceController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public MaintenanceController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _db.MaintenanceRecords.Include(m => m.Aircraft).ToListAsync();
            return Ok(list);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var rec = await _db.MaintenanceRecords.Include(m => m.Aircraft).FirstOrDefaultAsync(m => m.MaintenanceRecordId == id);
            if (rec == null) return NotFound();
            return Ok(rec);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] MaintenanceRecord rec)
        {
            var aircraft = await _db.Aircrafts.FindAsync(rec.AircraftId);
            if (aircraft == null) throw new AircraftNotFoundException(rec.AircraftId);
            _db.MaintenanceRecords.Add(rec);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = rec.MaintenanceRecordId }, rec);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] MaintenanceRecord updated)
        {
            var rec = await _db.MaintenanceRecords.FindAsync(id);
            if (rec == null) return NotFound();
            rec.Date = updated.Date;
            rec.Description = updated.Description;
            await _db.SaveChangesAsync();
            return Ok(rec);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var rec = await _db.MaintenanceRecords.FindAsync(id);
            if (rec == null) return NotFound();
            _db.MaintenanceRecords.Remove(rec);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
