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
        ILogger logger;
        
        public GroundServiceComponent(ILogger logger)
        {
            this.logger = logger;
        }
        public void Start()
        {
            logger?.Info($"{ComponentName}: Start");
            //Declare and purge queues
            foreach (var receiver in Receivers)
            {
                mqClient.DeclareQueues(ComponentName + receiver);
                mqClient.PurgeQueues(ComponentName + receiver);
            }
            foreach (var sender in Senders)
            {
                mqClient.DeclareQueues(sender + ComponentName);
                mqClient.PurgeQueues(sender + ComponentName);
            }

            //Receieve from airplane to parking
            mqClient.SubscribeTo<AirplaneServiceCommand>(Component.Airplane + ComponentName, (mes) =>
            {
                Task.Run(()=> {
                    logger?.Info($"{ComponentName}: Received message (PlaneId: {mes.PlaneId}, FlightId: {mes.FlightId}, PlaneLocation: {mes.LocationVertex}) from Airplane to start first cycle");

                    GroundServiceCycles cycle;
                    lock (airplaneLock)
                    {
                        cycle = serviceAirplanes.Find(x => x.FlightId == mes.FlightId);
                        if (cycle == null)
                        {
                            cycle = new GroundServiceCycles(mqClient, logger);
                            cycle.FlightId = mes.FlightId;
                            cycle.PlaneId = mes.PlaneId;
                            cycle.PlaneLocationVertex = mes.LocationVertex;

                            serviceAirplanes.Add(cycle);

                            logger?.Debug($"{ComponentName}: Added new cycle with (PlaneId: {mes.PlaneId}, FlightId: {mes.FlightId}, PlaneLocation: {mes.LocationVertex})");
                        }
                        else
                        {
                            cycle.PlaneId = mes.PlaneId;
                            cycle.PlaneLocationVertex = mes.LocationVertex;
                        }
                    }
                    cycle.StartFisrtCycle(mes.Needs);
                });
            }
            );

            //Receieve from registration that second cycle begin 
            mqClient.SubscribeTo<FlightInfo>(Component.Registration + ComponentName, (mes) =>
            {
                Task.Run(() => {
                    logger?.Info($"{ComponentName}: Received message (FlightId: {mes.FlightId}) from Registration to start second cycle");

                    GroundServiceCycles cycle;
                    lock (airplaneLock)
                    {
                        cycle = serviceAirplanes.Find(x => x.FlightId == mes.FlightId);
                        if (cycle == null)
                        {
                            cycle = new GroundServiceCycles(mqClient, logger);
                            cycle.FlightId = mes.FlightId;

                            serviceAirplanes.Add(cycle);

                            logger?.Debug($"{ComponentName}: Added new cycle with (FlightId: {mes.FlightId})");
                        }
                    }
                    cycle.StartSecondCycle(mes);
                });
            }
            );

            //Receieve from Schedule that time to Departure
            mqClient.SubscribeTo<AirplaneDepartureTimeSignal>(Component.Schedule + ComponentName, (mes) =>
            {
                Task.Run(() =>
                {
                    logger?.Info($"{ComponentName}: Received message (PlaneId: {mes.PlaneId}, FlightId: {mes.FlightId}) from Schedule that now time to Departure");
                    GroundServiceCycles cycle;
                    lock (airplaneLock)
                    {
                        cycle = serviceAirplanes.Find(x => x.FlightId == mes.FlightId);
                        if (cycle == null)
                        {
                            cycle = new GroundServiceCycles(mqClient, logger);
                            cycle.FlightId = mes.FlightId;

                            serviceAirplanes.Add(cycle);

                            logger?.Debug($"{ComponentName}: Added new cycle with (PlaneId: {mes.PlaneId}, FlightId: {mes.FlightId})");
                        }
                    }
                    cycle.StartDeparture();
                });
                
            }
            );

            //Subscribe to receive from GroundTransport
            foreach (var car in GroundTransport)
                mqClient.SubscribeTo<ServiceCompletionMessage>(car + ComponentName, (mes) =>
                  {
                      Task.Run(() => {
                          logger?.Info($"{ComponentName}: Received message (Component: {mes.Component}, PlaneId: {mes.PlaneId}) that service completion");

                          GroundServiceCycles cycle;
                          lock (airplaneLock)
                              cycle = serviceAirplanes.Find(x => x.PlaneId == mes.PlaneId);
                          if (cycle == null)
                          {
                              logger?.Error($"{ComponentName}: Cycle not found");
                              throw new Exception();
                          }
                          cycle.FinishAction(mes.Component);
                      });
                  }
                );
        }
        //free Cycles???
    }
}
