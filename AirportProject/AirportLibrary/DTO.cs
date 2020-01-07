using System;
using System.Collections.Generic;
using System.Text;

namespace AirportLibrary.DTO
{
    // Schedule component
    // ===================================
    // With Timetable, Cashbox, Registration 
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
    // With GroundService
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
    // With Airplane
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

    // All vehicles
    // ===================================
    // 
    // ===================================

    // Airplane Component
    // ===================================
    // With GroundService
    public class AirplaneServiceRequest
    {
        public string PlaneId { get; set; }
        public int LocationVertex { get; set; }
        public List<Tuple<AirplaneNeeds, int>> Needs { get; set; }
    }
    public enum AirplaneNeeds
    {
        PickUpPassengers, PickUpBaggage, Refuel
    }
    public class DepartureSignal
    {
        public string PlaneId { get; set; }
    }

    // With Bus
    public class PassengersTransfer
    {
        public string BusId { get; set; }
        public int PassengerCount { get; set; }
    }
    public class PassengerTransferRequest
    {
        public string PlaneId { get; set; }
        public string BusId { get; set; }
        public TransferAction Action { get; set; }
        public int PassengersCount { get; set; }
    }
    public enum TransferAction
    {
        Take, Give
    }

    // With Baggage
    public class BaggageTransfer
    {
        public string BaggageCarId { get; set; }
        public int BaggageCount { get; set; }
    }
    public class BaggageTransferRequest
    {
        public string PlaneId { get; set; }
        public string BaggageCarId { get; set; }
        public TransferAction Action { get; set; }
        public int BaggageCount { get; set; }
    }
    // With FollowMe
    public class ArrivalConfirmation
    {
        public string PlaneId { get; set; }
        public string FollowMeId { get; set; }
        public int LocationVertex { get; set; }
    }
    public class FollowMeCommand
    {
        public string PlaneId { get; set; }
        public string FollowMeId { get; set; }
        public int DestinationVertex { get; set; }
    }
    // With Deicing
    public class DeicingCompletion
    {
        public string PlaneId { get; set; }
    }
    // With FuelTruck
    public class RefuelCompletion
    {
        public string PlaneId { get; set; }
        public int Fuel { get; set; }
    }
    // With Catering
    public class CateringCompletion
    {
        public string PlaneId { get; set; }
        public List<Tuple<Food, int>> FoodList { get; set; }
    }
    // ===================================

    // Registration component
    // ===================================
    // From Passenger
    public class CheckInRequest
    {
        public string PassengerId { get; set; }
        public string FlightId { get; set; }
        public bool HasBaggage { get; set; }
        public Food FoodType { get; set; }
    }

    public enum Food
    {
        Standard,
        Vegan,
        Child
    }

    // To Passenger
    public class CheckInResponse
    {
        public string PassengerId { get; set; }
        public CheckInStatus Status { get; set; }
    }

    public enum CheckInStatus
    {
        Early,
        Late,
        WrongTicket,
        Terminal
    }

    // To CashBox
    public class CheckTicketRequest
    {
        public string PassengerId { get; set; }
        public string FlightId { get; set; }
    }

    // From CashBox
    public class CheckTicketResponse
    {
        public string PassengerId { get; set; }
        public bool HasTicket { get; set; }
    }

    // From GroundService
    public class FoodInfoRequest
    {
        public string FlightId { get; set; }
    }

    // To GroundService
    public class FoodInfoResponse
    {
        public string FlightId { get; set; }
        public List<Tuple<Food, int>> FoodList { get; set; }
    }

    // Passing passenger to Storage
    public class PassengerStoragePass
    {
        public string PassengerId { get; set; }
        public string FlightId { get; set; }
    }

    // Passing baggage to Storage
    public class BaggageStoragePass
    {
        public string FlightId { get; set; }
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
