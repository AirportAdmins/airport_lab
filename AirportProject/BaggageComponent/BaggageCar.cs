using System;
using System.Collections.Generic;
using System.Text;

namespace Baggage
{
    public enum Status
    {
        Free, Busy
    }
    public class BaggageCar
    {
        public string BaggageCarID { get; }
        public string PlaneId { get; set; }
        public Status Status { get; set; }


        public const int MaxCountOfBaggage = 50;
        public int CountOfBaggage = 0;

        //motion data
        public bool MotionPermitted { get; set; }
        //
        public static int Speed { get => 50; }
        public int LocationVertex { get; set; }

        public BaggageCar(int id)
        {
            LocationVertex = RandomHomeVertex.GetHomeVertex(); 
            BaggageCarID = "Baggage-" + id;
            MotionPermitted = false;
            Status = Status.Free;
        }
    }
}
