using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;
using dotnetapp.Data;
using Microsoft.Extensions.DependencyInjection;

namespace TestProject
{
    [TestFixture]
    public class AircraftControllerTests
    {
        private HttpClient _client;
        private WebApplicationFactory<Program> _factory;

        [SetUp]
        public void Setup()
        {
            _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("TestDb");
                    });
                });
            });

            _client = _factory.CreateClient();
        }

        [TearDown]
        public void TearDown()
        {
            _client?.Dispose();
            _factory?.Dispose();
        }

        // helper: search upward for a relative path
        private string? FindUpwards(string relativePath, int maxLevels = 8)
        {
            var dir = AppContext.BaseDirectory;
            for (int i = 0; i < maxLevels; i++)
            {
                var candidate = Path.GetFullPath(Path.Combine(dir, relativePath));
                if (File.Exists(candidate) || Directory.Exists(candidate)) return candidate;
                var parent = Directory.GetParent(dir);
                if (parent == null) break;
                dir = parent.FullName;
            }
            return null;
        }

        private string GetSolutionAssemblyPath()
        {
            var found = FindUpwards(Path.Combine("dotnetwebapi", "dotnetapp", "bin", "Debug", "net6.0", "dotnetapp.dll"));
            if (found == null) throw new FileNotFoundException("Could not locate dotnetapp.dll");
            return found;
        }

        private string GetProjectRoot()
        {
            var found = FindUpwards(Path.Combine("dotnetwebapi", "dotnetapp"));
            if (found == null) throw new DirectoryNotFoundException("Could not locate project root");
            return found;
        }

        // ==================== FILE EXISTENCE TESTS ====================

        [Test]
        public void FileExistence_AircraftModelExists()
        {
            string projectRoot = GetProjectRoot();
            string filePath = Path.Combine(projectRoot, "Models", "Aircraft.cs");
            Assert.IsTrue(File.Exists(filePath), $"{filePath} should exist");
        }

        [Test]
        public void FileExistence_MaintenanceModelExists()
        {
            string projectRoot = GetProjectRoot();
            string filePath = Path.Combine(projectRoot, "Models", "MaintenanceRecord.cs");
            Assert.IsTrue(File.Exists(filePath), $"{filePath} should exist");
        }

        [Test]
        public void FileExistence_ExceptionExists()
        {
            string projectRoot = GetProjectRoot();
            string filePath = Path.Combine(projectRoot, "Exceptions", "AircraftNotFoundException.cs");
            Assert.IsTrue(File.Exists(filePath), $"{filePath} should exist");
        }

        // ==================== REFLECTION TESTS ====================

        [Test]
        public void Reflection_AircraftModel_HasRequiredProperties()
        {
            var assemblyPath = GetSolutionAssemblyPath();
            var assembly = Assembly.LoadFrom(assemblyPath);
            var type = assembly.GetType("dotnetapp.Models.Aircraft");
            Assert.IsNotNull(type, "Aircraft class should exist");

            var props = type.GetProperties();
            Assert.IsTrue(props.Any(p => p.Name == "AircraftId" && p.PropertyType == typeof(int)),
                "Aircraft should have int AircraftId property");
            Assert.IsTrue(props.Any(p => p.Name == "RegistrationNumber" && p.PropertyType == typeof(string)),
                "Aircraft should have string RegistrationNumber property");
        }

        [Test]
        public void Reflection_AircraftController_HasCreateMethod()
        {
            var assemblyPath = GetSolutionAssemblyPath();
            var assembly = Assembly.LoadFrom(assemblyPath);
            var type = assembly.GetType("dotnetapp.Controllers.AircraftController");
            Assert.IsNotNull(type, "AircraftController class should exist");

            var method = type.GetMethod("Create");
            Assert.IsNotNull(method, "Create method should exist");
        }

        [Test]
        public void Reflection_DbContext_HasDbSetProperties()
        {
            var assemblyPath = GetSolutionAssemblyPath();
            var assembly = Assembly.LoadFrom(assemblyPath);
            var type = assembly.GetType("dotnetapp.Data.ApplicationDbContext");
            Assert.IsNotNull(type, "ApplicationDbContext should exist");

            var props = type.GetProperties();
            Assert.IsTrue(props.Any(p => p.Name == "Aircrafts"),
                "DbContext should have Aircrafts DbSet");
            Assert.IsTrue(props.Any(p => p.Name == "MaintenanceRecords"),
                "DbContext should have MaintenanceRecords DbSet");
        }

        // ==================== API / FUNCTIONAL TESTS ====================

        [Test]
        public async Task API_PostAircraft_ReturnsCreated()
        {
            var aircraft = new { RegistrationNumber = "N12345", Model = "A320", Manufacturer = "Airbus", Capacity = 180 };
            var content = new StringContent(JsonConvert.SerializeObject(aircraft), Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/Aircraft", content);
            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        }

        [Test]
        public async Task API_GetAircraftById_NotFound_Returns500_or_404()
        {
            var response = await _client.GetAsync("/api/Aircraft/99999");
            Assert.IsTrue(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.InternalServerError);
        }

        [Test]
        public async Task API_FullCrud_Aircraft_Workflow()
        {
            // Create
            var aircraft = new { RegistrationNumber = "N54321", Model = "B737", Manufacturer = "Boeing", Capacity = 160 };
            var content = new StringContent(JsonConvert.SerializeObject(aircraft), Encoding.UTF8, "application/json");
            var create = await _client.PostAsync("/api/Aircraft", content);
            Assert.AreEqual(HttpStatusCode.Created, create.StatusCode);
            var createdStr = await create.Content.ReadAsStringAsync();
            dynamic created = JsonConvert.DeserializeObject(createdStr)!;
            int id = (int)created.aircraftId;

            // Read
            var get = await _client.GetAsync($"/api/Aircraft/{id}");
            Assert.AreEqual(HttpStatusCode.OK, get.StatusCode);

            // Update
            var updated = new { RegistrationNumber = "N54321", Model = "B737MAX", Manufacturer = "Boeing", Capacity = 170 };
            var upContent = new StringContent(JsonConvert.SerializeObject(updated), Encoding.UTF8, "application/json");
            var put = await _client.PutAsync($"/api/Aircraft/{id}", upContent);
            Assert.AreEqual(HttpStatusCode.OK, put.StatusCode);

            // Delete
            var del = await _client.DeleteAsync($"/api/Aircraft/{id}");
            Assert.AreEqual(HttpStatusCode.NoContent, del.StatusCode);
        }

        [Test]
        public async Task API_GetAllAircrafts_ReturnsOk()
        {
            var response = await _client.GetAsync("/api/Aircraft");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [Test]
        public async Task Negative_PostMaintenance_ForNonExistingAircraft_Returns500()
        {
            var rec = new { Date = DateTime.UtcNow, Description = "Engine check", AircraftId = 99999 };
            var content = new StringContent(JsonConvert.SerializeObject(rec), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/api/Maintenance", content);
            Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Test]
        public async Task API_Maintenance_Crud_Workflow()
        {
            // create aircraft first
            var aircraft = new { RegistrationNumber = "N77777", Model = "A350", Manufacturer = "Airbus", Capacity = 300 };
            var aContent = new StringContent(JsonConvert.SerializeObject(aircraft), Encoding.UTF8, "application/json");
            var createA = await _client.PostAsync("/api/Aircraft", aContent);
            Assert.AreEqual(HttpStatusCode.Created, createA.StatusCode);
            var createdStr = await createA.Content.ReadAsStringAsync();
            dynamic created = JsonConvert.DeserializeObject(createdStr)!;
            int id = (int)created.aircraftId;

            // Create maintenance
            var rec = new { Date = DateTime.UtcNow, Description = "Routine check", AircraftId = id };
            var content = new StringContent(JsonConvert.SerializeObject(rec), Encoding.UTF8, "application/json");
            var create = await _client.PostAsync("/api/Maintenance", content);
            Assert.AreEqual(HttpStatusCode.Created, create.StatusCode);
            var createdRecStr = await create.Content.ReadAsStringAsync();
            dynamic createdRec = JsonConvert.DeserializeObject(createdRecStr)!;
            int recId = (int)createdRec.maintenanceRecordId;

            // Get
            var get = await _client.GetAsync($"/api/Maintenance/{recId}");
            Assert.AreEqual(HttpStatusCode.OK, get.StatusCode);

            // Update
            var updated = new { Date = DateTime.UtcNow, Description = "Updated check", AircraftId = id };
            var upContent = new StringContent(JsonConvert.SerializeObject(updated), Encoding.UTF8, "application/json");
            var put = await _client.PutAsync($"/api/Maintenance/{recId}", upContent);
            Assert.AreEqual(HttpStatusCode.OK, put.StatusCode);

            // Delete
            var del = await _client.DeleteAsync($"/api/Maintenance/{recId}");
            Assert.AreEqual(HttpStatusCode.NoContent, del.StatusCode);
        }

        // ==================== BOUNDARY / NEGATIVE TESTS ====================

        [Test]
        public async Task Negative_CreateAircraft_MissingRegistration_ReturnsBadRequest()
        {
            var aircraft = new { Model = "A320", Manufacturer = "Airbus", Capacity = 180 };
            var content = new StringContent(JsonConvert.SerializeObject(aircraft), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/api/Aircraft", content);
            Assert.IsTrue(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.InternalServerError);
        }

        [Test]
        public void MethodExistence_AircraftController_HasDeleteMethod()
        {
        var assemblyPath = GetSolutionAssemblyPath();
        var assembly = Assembly.LoadFrom(assemblyPath);
        var type = assembly.GetType("dotnetapp.Controllers.AircraftController");
        var method = type.GetMethod("Delete");
        Assert.IsNotNull(method, "Delete method should exist");
        }

        [Test]
        public void Boundary_Aircraft_Capacity_ZeroAcceptableOrNot()
        {
        var assemblyPath = GetSolutionAssemblyPath();
        var assembly = Assembly.LoadFrom(assemblyPath);
        var type = assembly.GetType("dotnetapp.Models.Aircraft");
        Assert.IsNotNull(type);
        var props = type.GetProperties();
        var cap = props.SingleOrDefault(p => p.Name == "Capacity");
        Assert.IsNotNull(cap);
        }

    }
}
