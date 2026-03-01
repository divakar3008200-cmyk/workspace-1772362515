using System;

namespace dotnetapp.Exceptions
{
    public class AircraftNotFoundException : Exception
    {
        public AircraftNotFoundException(int id) : base($"Aircraft with id {id} not found") { }
    }
}