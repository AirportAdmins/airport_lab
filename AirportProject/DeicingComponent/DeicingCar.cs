using System;
using System.Collections.Generic;
using System.Text;
using AirportLibrary.DTO;

namespace DeicingComponent
{
    public enum Status
    {
        Free, Busy
    }
    class DeicingCar
    {
        public string DeicingCarID { get; }
        public string PlaneId { get; set; }
        public Status Status { get; set; }

        public bool MotionPermitted { get; set; }

        public static int Speed { get => 50; }
        public int LocationVertex { get; set; }

        public DeicingCar(int id)
        {
            LocationVertex = RandomHomeVertex.GetHomeVertex();
            DeicingCarID = "Deicing-" + id;
            MotionPermitted = false;
            Status = Status.Free;
        }

    }
}
