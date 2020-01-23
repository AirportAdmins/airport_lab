﻿using AirportLibrary.DTO;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AirportLibrary;
using System.Collections.Concurrent;

namespace Baggage
{
    /// <summary>
    /// Здесь описаны методы только багажной машины
    /// Главный метод -- MessageFromGroundService(). С него-то всё и начинается, ага
    /// </summary>
    partial class Baggage
    {
        static object locker = new object(); //При поиске свободной машины блокируем поиск для других потоков

        ConcurrentDictionary<string, Task> carTasks = new ConcurrentDictionary<string, Task>();

        //С САМОЛЁТОМ
        private void TakeOrGiveBaggageFromPlane(string planeId, string carId, TransferAction action, int baggageCount)
        {
            BaggageTransferRequest btr = new BaggageTransferRequest()
            {
                PlaneId = planeId,
                BaggageCarId = carId,
                Action = action,
                BaggageCount = baggageCount
            };

            mqClient.Send(queueToAirPlane, btr); //отправляем сообщение самолёту о переданном багаже или о том, сколько вмещается в машину

            
        }

        private void TakeBaggageFromPlane()
        {
            mqClient.SubscribeTo<BaggageTransfer>(queueFromAirPlane, (bt)=>
            {
                cars[bt.BaggageCarId].CountOfBaggage += bt.BaggageCount;
                
            });

        }

        //С БАГАЖНЫМ НАКОПИТЕЛЕМ
        private void ToStorageRequest(string carId, string flightId, int capacity)
        {
            BaggageFromStorageRequest bfr = new BaggageFromStorageRequest()
            {
                CarId = carId,
                FlightId = flightId,
                Capacity = capacity
            };
            mqClient.Send(queuetoStorage, bfr);
        }

        private void TakeBaggageFromStorage()
        {
            mqClient.SubscribeTo<BaggageFromStorageResponse>(queuetoStorage, (bfsr) =>
            {
                cars[bfsr.BaggageCarId].CountOfBaggage += bfsr.BaggageCount;
            });
            
        }

        //C СЛУЖБОЙ НАЗЕМНОГО ОБСЛУЖИВАНИЯ
        private void MessageFromGroundService() //забрать/сдать багаж на самолет
        {
            Console.WriteLine(DateTime.Now + "Получил сообщение от СНО");
            mqClient.SubscribeTo<BaggageServiceCommand>(queueFromGroundService, (bsc) =>
            {
                new Task(() =>
                {
                    int numOfCars = Convert.ToInt32(Math.Ceiling((double)(bsc.BaggageCount / BaggageCar.MaxCountOfBaggage))); //сколько машин нужно выделить под задачу

                    int carsEndWork = numOfCars;


                    if (bsc.Action == TransferAction.Give)
                    {

                        for (int i=0;i<numOfCars;i++)
                        {
                            BaggageCar  car = SearchFreeCar();
                            Task t = new Task(() =>
                            {
                                //поехать к накопителю 
                                GoPath(GoToVertexAlone, car, storageVertex);
                                Console.WriteLine($"{DateTime.Now} {car.BaggageCarID} приехала к накопителю");

                                ToStorageRequest(car.BaggageCarID, bsc.FlightId, BaggageCar.MaxCountOfBaggage);
                                Console.WriteLine($"{DateTime.Now} {car.BaggageCarID} запросила багаж у накопителя");

                                //поехать к самолёту 
                                GoPath(GoToVertexAlone, car, bsc.PlaneLocationVertex);
                                Console.WriteLine($"{DateTime.Now} {car.BaggageCarID} приехала к самолёту");
                                //отдаём багаж самолёту
                                sourceDelay.CreateToken().Sleep(2 * 60 * 1000); //отдаём багаж 15 минут
                                TakeOrGiveBaggageFromPlane(bsc.PlaneId, car.BaggageCarID, TransferAction.Give, car.CountOfBaggage);
                                car.CountOfBaggage = 0;
                                Console.WriteLine($"{DateTime.Now} {car.BaggageCarID} отдала багаж самолёту");

                                carsEndWork--; //машина обслужила самолёт

                                var source = new CancellationTokenSource();     //adds token and remove it after went home/new cmd
                                tokens.TryAdd(car.BaggageCarID, source);
                                //вернуться на стоянку
                                GoPathHome(car, RandomHomeVertex.GetHomeVertex(), tokens[car.BaggageCarID]);
                                tokens.Remove(car.BaggageCarID, out source);
                                Console.WriteLine($"{DateTime.Now} {car.BaggageCarID} вернулась на стоянку");
                            });

                            carTasks.TryAdd(car.BaggageCarID, t);
                            t.Start();
                        }

                    }
                    else if (bsc.Action == TransferAction.Take)
                    {
                        for (int i = 0; i < numOfCars; i++)
                        {
                            BaggageCar car = SearchFreeCar();
                            Task t = new Task(() =>
                            {

                                //поехать к самолёту 
                                GoPath(GoToVertexAlone, car, bsc.PlaneLocationVertex);
                                Console.WriteLine($"{DateTime.Now} {car.BaggageCarID} приехала к самолёту");

                                sourceDelay.CreateToken().Sleep(2 * 60 * 1000); //забираем багаж 15 минут
                                TakeOrGiveBaggageFromPlane(bsc.PlaneId, car.BaggageCarID, TransferAction.Take, car.CountOfBaggage);
                                Console.WriteLine($"{DateTime.Now} {car.BaggageCarID} отдала багаж самолёту");

                                carsEndWork--; //машина обслужила самолёт

                                //поехать к накопителю (багаж отдавать не надо) 
                                GoPath(GoToVertexAlone, car, storageVertex);
                                car.CountOfBaggage = 0;
                                Console.WriteLine($"{DateTime.Now} {car.BaggageCarID} приехала к накопителю");

                                var source = new CancellationTokenSource();     //adds token and remove it after went home/new cmd
                                tokens.TryAdd(car.BaggageCarID, source);
                                //едем на стоянку
                                GoPathHome(car, RandomHomeVertex.GetHomeVertex(), tokens[car.BaggageCarID]);
                                tokens.Remove(car.BaggageCarID, out source);
                                Console.WriteLine($"{DateTime.Now} {car.BaggageCarID} вернулась на стоянку");
                            });
                            carTasks.TryAdd(car.BaggageCarID, t);
                            t.Start();
                        }
                    }


                    //ждём, пока все машины не завершат работу
                    while (carsEndWork != 0)
                    {
                        sourceDelay.CreateToken().Sleep(100);
                    }
                    //отправляем СНО сообщение о том, что обслуживание самолёта завершено
                    ServiceCompletionMessage mes = new ServiceCompletionMessage()
                    {
                        Component = Component.Baggage,
                        PlaneId = bsc.PlaneId
                    };
                    mqClient.Send<ServiceCompletionMessage>(queueToGroundService, mes);
                    Console.WriteLine($"{DateTime.Now} завершено обслуживание самолёта");
                });
                
            });
        }

        private BaggageCar SearchFreeCar()
        {
            //При поиске свободной машины блокируем поиск для других потоков
            lock (locker)
            {
                BaggageCar car = cars.Values.First(car => car.Status == Status.Free);

                //если не нашли свободную машину, начинаем поиск заново
                if (car == null)
                {
                    sourceDelay.CreateToken().Sleep(15);
                    return SearchFreeCar();
                }
                else //иначе прерываем движение на стоянку и ставим статус busy
                {
                    car.Status = Status.Busy;
                    if (tokens.TryGetValue(car.BaggageCarID, out var cancellationToken))
                    {
                        cancellationToken.Cancel();
                    }
                    Task task = carTasks[car.BaggageCarID];
                    task.Wait();
                    carTasks.Remove(car.BaggageCarID, out task);

                    return car;
                }
            }
        }

    }
}