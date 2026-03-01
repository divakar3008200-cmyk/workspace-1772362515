using Microsoft.AspNetCore.Mvc;
using dotnetapp.Data;
using dotnetapp.Models;
using dotnetapp.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace dotnetapp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AircraftController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public AircraftController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _db.Aircrafts.Include(a => a.MaintenanceRecords).ToListAsync();
            return Ok(list);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var aircraft = await _db.Aircrafts.Include(a => a.MaintenanceRecords).FirstOrDefaultAsync(a => a.AircraftId == id);
            if (aircraft == null) throw new AircraftNotFoundException(id);
            return Ok(aircraft);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Aircraft aircraft)
        {
            _db.Aircrafts.Add(aircraft);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = aircraft.AircraftId }, aircraft);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Aircraft updated)
        {
            var aircraft = await _db.Aircrafts.FindAsync(id);
            if (aircraft == null) throw new AircraftNotFoundException(id);
            aircraft.RegistrationNumber = updated.RegistrationNumber;
            aircraft.Model = updated.Model;
            aircraft.Manufacturer = updated.Manufacturer;
            aircraft.Capacity = updated.Capacity;
            await _db.SaveChangesAsync();
            return Ok(aircraft);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var aircraft = await _db.Aircrafts.FindAsync(id);
            if (aircraft == null) throw new AircraftNotFoundException(id);
            _db.Aircrafts.Remove(aircraft);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
