using AirportLibrary.DTO;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AirportLibrary;

namespace Baggage
{
    /// <summary>
    /// Здесь описаны методы только багажной машины
    /// Главный метод -- MessageFromGroundService(). С него-то всё и начинается, ага
    /// </summary>
    partial class Baggage
    {
        static object locker = new object(); //При поиске свободной машины блокируем поиск для других потоков

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
            mqClient.SubscribeTo<BaggageServiceCommand>(queueFromGroundService, (bsc) =>
            {
                new Task(() =>
                {
                    int numOfCars = Convert.ToInt32(Math.Ceiling((double)(bsc.BaggageCount / BaggageCar.MaxCountOfBaggage))); //сколько машин нужно выделить под задачу

                    int carsEndWork = numOfCars;

                    Task[] tasks = new Task[numOfCars];

                    if (bsc.Action == TransferAction.Give)
                    {
                        for(int i=0;i<numOfCars;i++)
                        {
                            tasks[i] = ( new Task(() =>
                            {
                                BaggageCar car = SearchFreeCar();

                                //поехать к накопителю 
                                GoPath(GoToVertexAlone, car, bsc.StorageVertex);

                                ToStorageRequest(car.BaggageCarID, bsc.FlightId, BaggageCar.MaxCountOfBaggage);

                                //поехать к самолёту 
                                GoPath(GoToVertexAlone, car, bsc.PlaneLocationVertex);

                                //отдаём багаж самолёту
                                TakeOrGiveBaggageFromPlane(bsc.PlaneId, car.BaggageCarID, TransferAction.Give, car.CountOfBaggage);
                                car.CountOfBaggage = 0;
                                car.Status = Status.Free;

                                carsEndWork--; //машина обслужила самолёт

                                //вернуться на стоянку
                                GoPathHome(GoToVertexAlone, car, RandomHomeVertex.GetHomeVertex(), tokens[car.BaggageCarID]);



                            }));
                        }

                    }
                    else if (bsc.Action == TransferAction.Take)
                    {
                        for (int i = 0; i < numOfCars; i++)
                        {
                            tasks[i] = (new Task(() =>
                            {
                                BaggageCar car = SearchFreeCar();

                                //поехать к самолёту 
                                GoPath(GoToVertexAlone, car, bsc.PlaneLocationVertex);

                                TakeOrGiveBaggageFromPlane(bsc.PlaneId, car.BaggageCarID, TransferAction.Take, car.CountOfBaggage);

                                carsEndWork--; //машина обслужила самолёт

                                //поехать к накопителю (багаж отдавать не надо) 
                                GoPath(GoToVertexAlone, car, bsc.StorageVertex);

                                car.CountOfBaggage = 0;
                                car.Status = Status.Free;
                                //едем на стоянку
                                GoPathHome(GoToVertexAlone, car, RandomHomeVertex.GetHomeVertex(), tokens[car.BaggageCarID]);
                            }));
                           

                        }
                    }

                    foreach(Task t in tasks)
                    {
                        t.Start();
                    }

                    //ждём, пока все машины не завершат работу
                    while (carsEndWork != 0)
                    {
                        source.CreateToken().Sleep(100);
                    }
                    //отправляем СНО сообщение о том, что обслуживание самолёта завершено
                    ServiceCompletionMessage mes = new ServiceCompletionMessage()
                    {
                        Component = Component.Baggage,
                        PlaneId = bsc.PlaneId
                    };
                    mqClient.Send<ServiceCompletionMessage>(queueToGroundService, mes);
                });
                
            });
        }

        private BaggageCar SearchFreeCar()
        {
            //При поиске свободной машины блокируем поиск для других потоков
            lock (locker)
            {
                BaggageCar car = cars.Values.First(car => car.Status == Status.Free);
                car.Status = Status.Busy;

                //если не нашли свободную машину, начинаем поиск заново
                if (car == null)
                {
                    source.CreateToken().Sleep(15);
                    return SearchFreeCar();
                }
                else
                {
                    return car;
                }
            }
        }

    }
}
