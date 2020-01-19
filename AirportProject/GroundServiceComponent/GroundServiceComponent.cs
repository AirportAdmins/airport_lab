using System;
using AirportLibrary;
using RabbitMqWrapper;
using AirportLibrary.DTO;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace GroundServiceComponent
{
    public class GroundServiceComponent
    {
        public static readonly string ComponentName = Component.GroundService;
        public static readonly List<string> Receivers = new List<string>()
        {
            Component.Airplane,
            Component.FollowMe,
            Component.Bus,
            Component.Baggage,
            Component.FuelTruck,
            Component.Catering,
            Component.Deicing,
            Component.Schedule
        };
        public static readonly List<string> Senders = new List<string>()
        {
            Component.Airplane,
            Component.FollowMe,
            Component.Bus,
            Component.Baggage,
            Component.FuelTruck,
            Component.Catering,
            Component.Deicing,
            Component.Schedule,
            Component.Registration
        };
        public static readonly List<string> GroundTransport = new List<string>()
        {
            Component.FollowMe,
            Component.Bus,
            Component.Baggage,
            Component.FuelTruck,
            Component.Catering,
            Component.Deicing
        };

        readonly object airplaneLock = new object();

        List<GroundServiceCycles> serviceAirplanes = new List<GroundServiceCycles>();
        RabbitMqClient mqClient = new RabbitMqClient();
        
        
        public GroundServiceComponent()
        {
        }
        public void Start()
        {
            //Declare queues
            foreach (var receiver in Receivers)
                mqClient.DeclareQueues(ComponentName+receiver);
            foreach (var sender in Senders)
                mqClient.DeclareQueues(sender+ComponentName);

            //Receieve from airplane to parking
            mqClient.SubscribeTo<AirplaneServiceCommand>(Component.Airplane + ComponentName, (mes) =>
            {
                GroundServiceCycles cycle;
                lock (airplaneLock)
                {
                    cycle = serviceAirplanes.Find(x => x.FlightId == mes.FlightId);
                    if (cycle == null)
                    {
                        cycle = new GroundServiceCycles(mqClient, mes.PlaneId, mes.FlightId, mes.LocationVertex);
                        serviceAirplanes.Add(cycle);
                    }
                    else
                    {
                        cycle.PlaneId = mes.PlaneId;
                        cycle.PlaneLocationVertex = mes.LocationVertex;
                    }
                }
                new Task(cycle.StartFisrtCycle,mes.Needs).Start();
            }
            );

            //Receieve from registration that second cycle begin 
            mqClient.SubscribeTo<FlightInfo>(Component.Registration + ComponentName, (mes) =>
            {
                GroundServiceCycles cycle;
                lock (airplaneLock)
                {
                    cycle = serviceAirplanes.Find(x => x.FlightId == mes.FlightId);
                    if (cycle == null)
                    {
                        cycle = new GroundServiceCycles(mqClient, mes.FlightId);
                        serviceAirplanes.Add(cycle);
                    }
                }
                new Task(cycle.StartSecondCycle, mes).Start();
            }
            );

            //Receieve from Schedule that time to Departure
            mqClient.SubscribeTo<AirplaneDepartureTimeSignal>(Component.Schedule + ComponentName, (mes) =>
            {
                GroundServiceCycles cycle;
                lock (airplaneLock)
                {
                    cycle = serviceAirplanes.Find(x => x.FlightId == mes.FlightId);
                    if (cycle == null)
                    {
                        cycle = new GroundServiceCycles(mqClient, mes.FlightId);
                        serviceAirplanes.Add(cycle);
                    }
                }
                new Task(cycle.StartDeparture).Start();
            }
            );

            //Subscribe to receive from GroundTransport
            foreach (var car in GroundTransport)
                mqClient.SubscribeTo<ServiceCompletionMessage>(car + ComponentName, (mes) =>
                  {
                      GroundServiceCycles cycle;
                      lock (airplaneLock)
                          cycle = serviceAirplanes.Find(x => x.PlaneId == mes.PlaneId);
                      if (cycle == null)
                      {
                          //log
                          throw new Exception();
                      } 
                      new Task(cycle.FinishAction, mes.Component).Start();
                  }
                );
        }
        //free Cycles???
    }
}
