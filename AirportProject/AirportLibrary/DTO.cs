using System;
using System.Collections.Generic;
using System.Text;

namespace AirportLibrary.DTO
{
    // Schedule component
    // ===================================
    // To Timetable, Cashbox, Registration 
    public class FlightStatusUpdate
    {
        public string FlightId { get; set; }
        public FlightStatus Status { get; set; }
        public DateTime DepartureTime { get; set; }
        public int TicketCount { get; set; }
    }
    public enum FlightStatus
    {
        New, CheckIn, Boarding, Delayed, Departed
    }
    // To GroundService
    public class AirplaneServiceSignal
    {
        public ServiceSignal Signal { get; set; }
        public string PlaneId { get; set; }
        public string FlightId { get; set; }
    }
    public enum ServiceSignal
    {
        Boarding, Departure
    }
    public class AirplaneServiceStatus
    {
        public ServiceStatus Status { get; set; }
        public string PlaneId { get; set; }
    }
    public enum ServiceStatus
    {
        Delayed, Departed
    }
    // To Airplane
    public class AirplaneModel
    {
        public static readonly IList<AirplaneModel> Models = new List<AirplaneModel>() {
            new AirplaneModel("Boeing 737", 60, 1000)
        };
        public int Seats { get; set; }
        public int Fuel { get; set; }
        public string Model { get; set; }
        private AirplaneModel(string model, int seats, int fuel)
        {
            Model = model;
            Seats = seats;
            Fuel = fuel;
        }
    }
    public class AirplaneGenerationRequest
    {
        // From AirplaneModel.Models.Model names
        public string AirplaneModelName { get; set; }
        public string FlightId { get; set; }
    }
    public class AirplaneGenerationResponse
    {
        // From AirplaneModel.Models.Model 
        public string FlightId { get; set; }
        public string PlaneId { get; set; }
    }
    // ===================================








    // TimeService Component
    // ===================================
    public class NewTimeSpeedFactor
    {
        public double Factor { get; set; }
    }
    public class CurrentPlayTime
    {
        public DateTime PlayTime { get; set; }
    }
    // ===================================

    // Logs Component
    // ===================================
    public class LogMessage
    {
        // Constant from AirportLibrary.Component class
        public string Component { get; set; }
        public string Message { get; set; }
    }
    // ===================================
}
