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
        public void TakeOrGiveBaggageFromPlane(string planeId, string carId, TransferAction action, int baggageCount)
        {
            BaggageTransferRequest btr = new BaggageTransferRequest()
            {
                PlaneId = planeId,
                BaggageCarId = carId,
                Action = action,
                BaggageCount = baggageCount
            };

            mqClient.Send(queueToAirPlane, btr); //отправляем сообщение самолёту о переданном багаже или о том, сколько вмещается в машину

            if(action == TransferAction.Take)
            {
                TakeBaggageFromPlane();
            }
            //теперь надо как-то уехать отсюдова на стоянку
        }

        public void TakeBaggageFromPlane()
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

            //теперь надо отдать багаж накопителю
        }


        //public void BaggageToCar(BaggageTransfer bt)
        //{
        //    for(int i = 0; i < cars.Count; i++)
        //    {
        //        if (bt.BaggageCarId.Equals(cars[i].BaggageCarID))
        //        {
        //            cars[i].CountOfBaggage += bt.BaggageCount;
        //            break;
        //        }
        //    }
        //    //теперь надо отдать багаж накопителю
        //}

        //С БАГАЖНЫМ НАКОПИТЕЛЕМ

        public void ToStorageRequest(string carId, string flightId, int capacity)
        {
            BaggageFromStorageRequest bfr = new BaggageFromStorageRequest()
            {
                CarId = carId,
                FlightId = flightId,
                Capacity = capacity
            };
            mqClient.Send(queuetoStorage, bfr);

            //потом получаем багаж от накопителя
        }

        public void TakeBaggageFromStorage()
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

            //едем к самолёту
        }

    }
}
