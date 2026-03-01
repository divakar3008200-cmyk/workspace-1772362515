using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using NUnit.Framework;
using dotnetapp;
using dotnetapp.Models;
using System.Reflection;

namespace dotnetapp.Tests
{
    [TestFixture]
    public class ResponderTests
    {
        private string connectionString = ConnectionStringProvider.ConnectionString;
        private StringWriter consoleOutput;
        private TextWriter originalConsoleOut;

        [SetUp]
        public void Setup()
        {
            // Clear the database before each test
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("DELETE FROM Responders", conn);
                cmd.ExecuteNonQuery();
            }

            // Redirect console output to capture messages
            originalConsoleOut = Console.Out;
            consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);
        }

        [TearDown]
        public void TearDown()
        {
            // Reset console output
            Console.SetOut(originalConsoleOut);

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("DELETE FROM Responders", conn);
                cmd.ExecuteNonQuery();
            }
        }

  [Test, Order(1)]
          public async Task Test_Responder_Class_Should_Exist()
        {
            // Arrange
            var assembly = typeof(Responder).Assembly;
            Type responderType = assembly.GetType("dotnetapp.Models.Responder");

            // Assert
            Assert.IsNotNull(responderType, "Responder class should exist.");
        }

        [Test, Order(2)]
          public async Task Test_Responder_Properties_Should_Exist()
        {
            // Arrange
            Type responderType = typeof(Responder);

            // Act
            PropertyInfo responderIdProperty = responderType.GetProperty("ResponderID");
            PropertyInfo nameProperty = responderType.GetProperty("Name");
            PropertyInfo roleProperty = responderType.GetProperty("Role");
            PropertyInfo experienceYearsProperty = responderType.GetProperty("ExperienceYears");
            PropertyInfo incidentsHandledProperty = responderType.GetProperty("IncidentsHandled");
            PropertyInfo totalRespondsHoursProperty = responderType.GetProperty("TotalRespondsHours");


            // Assert
            Assert.IsNotNull(responderIdProperty, "ResponderID property should exist.");
            Assert.IsNotNull(nameProperty, "Name property should exist.");
            Assert.IsNotNull(roleProperty, "Role property should exist.");
            Assert.IsNotNull(experienceYearsProperty, "ExperienceYears property should exist.");
            Assert.IsNotNull(incidentsHandledProperty, "IncidentsHandled property should exist.");
            Assert.IsNotNull(totalRespondsHoursProperty, "TotalRespondsHours property should exist.");

        }

        [Test, Order(3)]
          public async Task Test_AddResponderRecord_Method_Exists()
        {
            // Check if AddResponderRecord method exists
            var method = typeof(Program).GetMethod("AddResponderRecord");
            Assert.IsNotNull(method, "The AddResponderRecord method should exist in the Program class.");
        }

        [Test, Order(4)]
          public async Task Test_DisplayLimitedResponderRecords_Method_Exists()
        {
            // Check if DisplayLimitedResponderRecords method exists
            var method = typeof(Program).GetMethod("DisplayLimitedResponderRecords");
            Assert.IsNotNull(method, "The DisplayLimitedResponderRecords method should exist in the Program class.");
        }

        [Test, Order(5)]
          public async Task Test_UpdateResponderRecord_Method_Exists()
        {
            // Check if UpdateResponderRecord method exists
            var method = typeof(Program).GetMethod("UpdateResponderRecord");
            Assert.IsNotNull(method, "The UpdateResponderRecord method should exist in the Program class.");
        }

        [Test, Order(6)]
          public async Task Test_DeleteResponderRecord_Method_Exists()
        {
            // Check if DeleteResponderRecord method exists
            var method = typeof(Program).GetMethod("DeleteResponderRecord");
            Assert.IsNotNull(method, "The DeleteResponderRecord method should exist in the Program class.");
        }


       [Test, Order(7)]
          public async Task Test_AddResponderRecord_Should_Insert_Record()
        {
            // Arrange
            Type responderType = typeof(Responder);
            object responderInstance = Activator.CreateInstance(responderType);
            responderType.GetProperty("Name").SetValue(responderInstance, "Jane Smith");
            responderType.GetProperty("Role").SetValue(responderInstance, "Firefighter");
            responderType.GetProperty("ExperienceYears").SetValue(responderInstance, 5);
            responderType.GetProperty("IncidentsHandled").SetValue(responderInstance, 5);
            responderType.GetProperty("TotalRespondsHours").SetValue(responderInstance, 1000.00m);

            // Act
            MethodInfo addResponderMethod = typeof(Program).GetMethod("AddResponderRecord");
            addResponderMethod.Invoke(null, new object[] { responderInstance });

            // Assert
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Responders WHERE Name = 'Jane Smith'", conn);
                int count = (int)cmd.ExecuteScalar();
                Assert.AreEqual(1, count, "Responder record should be inserted into the database.");
            }
        }

        [Test, Order(8)]
          public async Task Test_UpdateResponderRecord_Should_Modify_Record()
        {
            // Arrange
            InsertResponderIntoDatabase("Johnson", "Paramedic", 6, 2, 400.00m);

            Type responderType = typeof(Responder);
            object updatedResponderInstance = Activator.CreateInstance(responderType);
            responderType.GetProperty("Name").SetValue(updatedResponderInstance, "Jane Carter");
            responderType.GetProperty("Role").SetValue(updatedResponderInstance, "Disaster Relief Coordinator");
            responderType.GetProperty("ExperienceYears").SetValue(updatedResponderInstance, 5);
            responderType.GetProperty("IncidentsHandled").SetValue(updatedResponderInstance, 5);
            responderType.GetProperty("TotalRespondsHours").SetValue(updatedResponderInstance, 1000.00m);

            // Act
            var output = CaptureConsoleOutput(() =>
            {
                MethodInfo updateResponderMethod = typeof(Program).GetMethod("UpdateResponderRecord");
                updateResponderMethod.Invoke(null, new object[] { "Johnson", updatedResponderInstance });
            });

            // Assert
            Assert.IsTrue(output.ToLower().Contains("responder record updated successfully"));
        }

        [Test, Order(9)]
          public async Task Test_DisplayLimitedResponderRecords_Should_Output_Records()
        {
            // Arrange
            InsertResponderIntoDatabase("Jane Carter", "Disaster Relief Coordinator", 12, 5, 1000.00m);
            InsertResponderIntoDatabase("Sarah", "Rescue Technician", 8, 3, 600.00m);

            // Act
            var output = CaptureConsoleOutput(() =>
            {
                MethodInfo displayRespondersMethod = typeof(Program).GetMethod("DisplayLimitedResponderRecords");
                displayRespondersMethod.Invoke(null, new object[] { 2 });
            });

            // Assert
            Assert.IsTrue(output.ToLower().Contains("jane carter"));
            Assert.IsTrue(output.ToLower().Contains("sarah"));
        }

        [Test, Order(10)]
          public async Task Test_DeleteResponderRecord_Should_Remove_Record()
        {
            // Arrange
            InsertResponderIntoDatabase("Brown", "Emergency Manager", 15, 7, 1400.00m);

            // Act
            var output = CaptureConsoleOutput(() =>
            {
                MethodInfo deleteResponderMethod = typeof(Program).GetMethod("DeleteResponderRecord");
                deleteResponderMethod.Invoke(null, new object[] { "Brown" });
            });

            // Assert
            Assert.IsTrue(output.Contains("Responder record deleted successfully"));

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Responders WHERE Name = 'Brown'", conn);
                int count = (int)cmd.ExecuteScalar();
                Assert.AreEqual(0, count, "Responder record should be deleted from the database.");
            }
        }

        [Test, Order(11)]
public async Task Test_DisplayLimitedResponderRecords_Should_Handle_No_Records_Found()
{
    // Act - Capture console output
    var output = CaptureConsoleOutput(() =>
    {
        MethodInfo displayRespondersMethod = typeof(Program).GetMethod("DisplayLimitedResponderRecords");
        displayRespondersMethod.Invoke(null, new object[] { 5 }); // Attempt to display records when the table is empty
    });
 Console.WriteLine(output);
    // Assert
    Assert.IsTrue(output.ToLower().Contains("no responder records found"), "The message 'No responder records found' should be displayed.");
}

[Test, Order(12)]
public async Task Test_DeleteResponderRecord_Should_Handle_No_Record_Found()
{
    // Act - Capture console output
    var output = CaptureConsoleOutput(() =>
    {
        MethodInfo deleteResponderMethod = typeof(Program).GetMethod("DeleteResponderRecord");
        deleteResponderMethod.Invoke(null, new object[] { "NonExistentResponder" });
    });
    // Assert
    Assert.IsTrue(output.ToLower().Contains("no matching responder record found"));
}

[Test, Order(13)]
public async Task Test_UpdateResponderRecord_Should_Handle_No_Record_Found()
{
    // Arrange
    Type responderType = typeof(Responder);
    object updatedResponderInstance = Activator.CreateInstance(responderType);
    responderType.GetProperty("Role").SetValue(updatedResponderInstance, "Emergency Manager");
    responderType.GetProperty("ExperienceYears").SetValue(updatedResponderInstance, 5);
    responderType.GetProperty("IncidentsHandled").SetValue(updatedResponderInstance, 5);
    responderType.GetProperty("TotalRespondsHours").SetValue(updatedResponderInstance, 1000.00m);

    // Act - Capture console output
    var output = CaptureConsoleOutput(() =>
    {
        MethodInfo updateResponderMethod = typeof(Program).GetMethod("UpdateResponderRecord");
        updateResponderMethod.Invoke(null, new object[] { "NonExistentResponder", updatedResponderInstance }); // Attempt to update a non-existent responder
    });

    // Assert
    Assert.IsTrue(output.ToLower().Contains("no matching responder record found"));
}


   private void InsertResponderIntoDatabase(string name, string role, decimal experienceYears, int incidentsHandled, decimal totalRespondsHours)
{
    using (SqlConnection conn = new SqlConnection(connectionString))
    {
        // Open connection
        conn.Open();

        // Create a DataAdapter to fetch the schema of the Responders table
        SqlDataAdapter adapter = new SqlDataAdapter("SELECT * FROM Responders", conn);
        SqlCommandBuilder commandBuilder = new SqlCommandBuilder(adapter);

        // Fill a DataSet with the Responders table structure
        DataSet dataSet = new DataSet();
        adapter.Fill(dataSet, "Responders");

        // Access the DataTable from the DataSet
        DataTable respondersTable = dataSet.Tables["Responders"];

        // Create a new DataRow and populate it with data
        DataRow newRow = respondersTable.NewRow();
        newRow["Name"] = name;
        newRow["Role"] = role;
        newRow["ExperienceYears"] = experienceYears;
        newRow["IncidentsHandled"] = incidentsHandled;
        newRow["TotalRespondsHours"] = totalRespondsHours;


        // Add the new DataRow to the DataTable
        respondersTable.Rows.Add(newRow);

        // Use the DataAdapter to update the database with changes made in the DataSet
        adapter.Update(dataSet, "Responders");

        Console.WriteLine("Responder record inserted successfully using disconnected architecture.");
    }
}


        private string CaptureConsoleOutput(Action action)
        {
            consoleOutput.GetStringBuilder().Clear();
            action.Invoke();
            return consoleOutput.ToString();
        }
 

    }

}
