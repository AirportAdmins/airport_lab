using AirportLibrary;
using AirportLibrary.DTO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeicingComponent
{
    partial class DeicingComponent
    {
        static object locker = new object(); //При поиске свободной машины блокируем поиск для других потоков

        ConcurrentDictionary<string, Task> carTasks = new ConcurrentDictionary<string, Task>();

        private void WorkWithPlane(string planeId)
        {
            DeicingCompletion deicingCompletion = new DeicingCompletion()
            {
                PlaneId = planeId
            };

            source.CreateToken().Sleep(2*60*1000);//чистим лёд

            mqClient.Send<DeicingCompletion>(queueToAirPlane, deicingCompletion);
        }

        private void MessageFromGroundService()
        {
            mqClient.SubscribeTo<ServiceCommand>(queueFromGroundService, (sc) =>
            {
                Console.WriteLine(DateTime.Now + " " + Component.Deicing + " Получил сообщение от СНО");
                DeicingCar car = SearchFreeCar();
                Console.WriteLine($"нашли свободную машину {car.DeicingCarID}");
                Task t = new Task(() =>
                {

                    //приехать к самолёту 
                    GoPath(GoToVertexAlone, car, sc.PlaneLocationVertex);
                    Console.WriteLine($"{DateTime.Now} {car.DeicingCarID} приехала к самолёту");

                    //чистим самолёт от льда
                    WorkWithPlane(sc.PlaneId);
                    Console.WriteLine($"{DateTime.Now} {car.DeicingCarID} почистила самолёт");

                    ServiceCompletionMessage deicingCompletion = new ServiceCompletionMessage()
                    {
                        PlaneId = sc.PlaneId,
                        Component = Component.Deicing
                    };

                    mqClient.Send<ServiceCompletionMessage>(queueToGroundService, deicingCompletion);
                    Console.WriteLine($"{DateTime.Now} {car.DeicingCarID} отправила сообщение СНО");

                    var source = new CancellationTokenSource();     //adds token and remove it after went home/new cmd
                    tokens.TryAdd(car.DeicingCarID, source);
                    //уезжаем на стоянку 
                    GoPathHome(car, RandomHomeVertex.GetHomeVertex(), tokens[car.DeicingCarID]);    
                    if (!tokens[car.DeicingCarID].IsCancellationRequested)     //если не было отмены пути домой
                        Console.WriteLine($"{DateTime.Now} {car.DeicingCarID} приехала домой");
                    tokens.Remove(car.DeicingCarID, out source);
                });

                carTasks.TryAdd(car.DeicingCarID, t);
                t.Start();

            });

        }


        private DeicingCar SearchFreeCar()
        {
            //При поиске свободной машины блокируем поиск для других потоков
            lock (locker)
            {
                DeicingCar car = cars.Values.FirstOrDefault(car => car.Status == Status.Free);

                //если не нашли свободную машину, начинаем поиск заново
                while(car == null)
                {
                    source.CreateToken().Sleep(15);
                    car = cars.Values.FirstOrDefault(car => car.Status == Status.Free);
                }
                 //иначе прерываем движение на стоянку и ставим статус busy               
                car.Status = Status.Busy;

                if (carTasks.ContainsKey(car.DeicingCarID))
                {

                    if (tokens.TryGetValue(car.DeicingCarID, out var cancellationToken))
                    {
                        cancellationToken.Cancel();
                        Task task = carTasks[car.DeicingCarID];
                        task.Wait();
                        carTasks.Remove(car.DeicingCarID, out task);
                    }
                }
                return car;
            }
        }
    }
}
