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

        public int Position { get; set; } //положение на графе

        public const int MaxCountOfBaggage = 50;
        public int CountOfBaggage = 0;

        //motion data
        public bool MotionPermitted { get; set; }
        public bool GotAirplaneResponse { get; set; }
        //
        public static int Speed { get => 50; }
        public int LocationVertex { get; set; }

        public BaggageCar(int id)
        {
            Position = 10; 
            BaggageCarID = "Baggage-" + id;
            MotionPermitted = false;
            GotAirplaneResponse = false;
            Status = Status.Free;
        }
    }
}
