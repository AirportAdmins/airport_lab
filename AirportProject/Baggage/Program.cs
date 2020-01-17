using System;
using RabbitMqWrapper;
using AirportLibrary;
using AirportLibrary.DTO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using AirportLibrary.Graph;
using System.Collections.Concurrent;

namespace Baggage
{
    class Program
    {
        static void Main(string[] args)
        {

            var bg = new Baggage();
             bg.mqClient.DeclareQueues(bg.queues.ToArray());//обьявление
             bg.mqClient.PurgeQueues(bg.queues.ToArray());//очистка


            bg.mqClient.SubscribeTo<NewTimeSpeedFactor>(queueFromTimeService, (mes) =>
            {
                bg.timeCoef = mes.Factor;
                Console.WriteLine("Коэффициент времени изменился и стал: " + bg.timeCoef);
            });

            bg.mqClient.SubscribeTo<BaggageFromStorageResponse>(queueFromStorage, (mes) =>
           {
               bg.BcarId = mes.BaggageCarId;
               bg.baggageCount = mes.BaggageCount;
           });

            bg.mqClient.SubscribeTo<BaggageTransfer>(queueFromAirPlane, (mes) =>
            {
                bg.BcarId = mes.BaggageCarId;
                bg.baggageCount = mes.BaggageCount;
            });
            bg.mqClient.SubscribeTo<BaggageServiceCommand>(queueFromGroundService, (mes) =>
            {
                bg.StorVertex = mes.StorageVertex;
                bg.PlaneVertex = mes.PlaneLocationVertex;
                bg.planeID = mes.FlightId;
                bg.baggageCount = mes.BaggageCount;
            });
            bg.mqClient.SubscribeTo<MotionPermissionResponse>(queueFromGroundMotion, response => //groundmotion
                    cars[response.ObjectId].MotionPermitted = true);
        }
        

        
    }
}
