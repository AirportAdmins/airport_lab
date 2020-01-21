using AirportLibrary.DTO;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeicingComponent
{
    partial class DeicingComponent
    {

        private void WorkWithPlane(string planeId)
        {
            DeicingCompletion deicingCompletion = new DeicingCompletion()
            {
                PlaneId = planeId
            };

            source.CreateToken().Sleep(50);//чистим лёд

            mqClient.Send<DeicingCompletion>(queueToAirPlane, deicingCompletion);
        }

        private void MessageFromGroundService()
        {
            mqClient.SubscribeTo<ServiceCommand>(queueFromGroundService, (sc) =>
            {

                //приехать к самолёту TODO

                //чистим самолёт от льда
                WorkWithPlane(sc.PlaneId);

                //уезжаем на стоянку TODO

            });

        }
    }
}
