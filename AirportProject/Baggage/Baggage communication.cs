using AirportLibrary.DTO;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Baggage
{
    partial class Baggage
    {

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
                for (int i = 0; i < cars.Count; i++)
                {
                    if (bt.BaggageCarId.Equals(cars[i].BaggageCarID))
                    {
                        cars[i].CountOfBaggage += bt.BaggageCount;
                        break;
                    }
                }
                
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
                for (int i = 0; i < cars.Count; i++)
                {
                    if (bfsr.BaggageCarId.Equals(cars[i].BaggageCarID))
                    {
                        cars[i].CountOfBaggage += bfsr.BaggageCount;
                        break;
                    }
                }
            });
        }

        //C СЛУЖБОЙ НАЗЕМНОГО ОБСЛУЖИВАНИЯ
        private void MessageFromGroundService() //забрать/сдать багаж на самолет
        {
            mqClient.SubscribeTo<BaggageServiceCommand>(queueFromGroundService, (bsc) =>
            {
                int numOfCars = Convert.ToInt32(Math.Ceiling((double)(bsc.BaggageCount / BaggageCar.MaxCountOfBaggage))); //сколько машин нужно выделить под задачу

                

                if (bsc.Action == TransferAction.Give)
                {
                    while (numOfCars > 0)
                    {
                        BaggageCar car = SearchFreeCar();

                        //поехать к накопителю TODO
                        ToStorageRequest(car.BaggageCarID, bsc.FlightId, BaggageCar.MaxCountOfBaggage );
                        //поехать к самолёту TODO
                        TakeOrGiveBaggageFromPlane(bsc.PlaneId, car.BaggageCarID, TransferAction.Give, car.CountOfBaggage);
                        car.CountOfBaggage = 0;
                        car.Status = Status.Free;
                        //вернуться на стоянку TODO
                        numOfCars--;
                    }

                }
                else if(bsc.Action == TransferAction.Take)
                {
                    while (numOfCars > 0)
                    {
                        BaggageCar car = SearchFreeCar();

                        //поехать к самолёту TODO
                        TakeOrGiveBaggageFromPlane(bsc.PlaneId, car.BaggageCarID, TransferAction.Take, car.CountOfBaggage);
                        //поехать к накопителю (багаж отдавать не надо) TODO
                        car.CountOfBaggage = 0;
                        car.Status = Status.Free;
                        //едем на стоянку TODO
                        numOfCars--;
                    }
                }
            });
        }

        private BaggageCar SearchFreeCar()
        {
            foreach (BaggageCar bc in cars)
            {
                if (bc.Status == Status.Free)
                {
                    bc.Status = Status.Busy;
                    return bc;
                }
            }
            //если не нашли свободную машину, начинаем поиск заново
            return SearchFreeCar();
        }

    }
}
