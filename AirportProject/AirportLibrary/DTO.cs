using Newtonsoft.Json;
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
        public int BaggagePlaces { get; set; }
        public int Fuel { get; set; }
        public string Model { get; set; }
        private AirplaneModel(string model, int seats, int fuel)
        {
            Model = model;
            Seats = seats;
            BaggagePlaces = seats;
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
    public class AirplaneServiceCommand
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
        Registered,
        Terminal,
        LateForTerminal,
        NoSuchFlight
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

    // To GroundService
    public class FlightInfo
    {
        public string FlightId { get; set; }
        public int PassengerCount { get; set; }
        public int BaggageCount { get; set; }
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

    // GroundService Component
    // ===================================
    // With Storage
    public class FlightStorageInfoRequest
    {
        public string FlightId { get; set; }
    }
    public class FlightStorageInfoResponse
    {
        public string FlightId { get; set; }
        public int PassengersCount { get; set; }
        public int BaggageCount { get; set; }
    }
    // Common to Service Commands
    public class ServiceCommand
    {
        public int PlaneLocationVertex { get; set; }
        public string PlaneId { get; set; }
    }
    // With FollowMe
    public class AirplaneTransferCommand : ServiceCommand
    {
        public int DestinationVertex { get; set; }
    }
    // With Bus
    public class PassengersServiceCommand : ServiceCommand
    {
        public int StorageVertex { get; set; }
        public TransferAction Action { get; set; }
        public int PassengersCount { get; set; }
        public string FlightId { get; set; }
    }
    // With Baggage
    public class BaggageServiceCommand : ServiceCommand
    {
        public int StorageVertex { get; set; }
        public TransferAction Action { get; set; }
        public int BaggageCount { get; set; }
        public string FlightId { get; set; }
    }
    // With FuelTruck
    public class RefuelServiceCommand : ServiceCommand
    {
        public int Fuel { get; set; }
    }
    // With Catering
    public class CateringServiceCommand : ServiceCommand
    {
        public List<Tuple<Food, int>> FoodList { get; set; }
    }
    // With all service cars
    public class ServiceCompletionMessage
    {
        // Constant from AirportLibrary.Component class
        public string Component { get; set; }
        public string PlaneId { get; set; }
    }
    // ===================================

    // GroundMotion Component
    // ===================================
    // With all transport
    public class MotionPermissionRequest
    {
        // Constant from AirportLibrary.Component class
        public string Component { get; set; }
        public string ObjectId { get; set; }
        public int StartVertex { get; set; }
        public int DestinationVertex { get; set; }
        public MotionAction Action { get; set; }
    }
    public enum MotionAction
    {
        Occupy, Free
    }
    public class MotionPermissionResponse
    {
        public string ObjectId { get; set; }
    }
    // ===================================

    // TimeService Component
    // ===================================
    public class NewTimeSpeedFactor
    {
        [JsonProperty("factor")]
        public double Factor { get; set; }
    }
    public class CurrentPlayTime
    {
        public DateTime PlayTime { get; set; }
    }
    // ===================================

    // Visualizer Component
    // ===================================
    public class VisualizationMessage
    {
        // Constant from AirportLibrary.Component class or AirplaneModel.Models.Model names
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("id")]
        public string ObjectId { get; set; }
        [JsonProperty("start")]
        public int StartVertex { get; set; }
        [JsonProperty("end")]
        public int DestinationVertex { get; set; }
        [JsonProperty("speed")]
        public int Speed { get; set; }
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
