using System;
using System.Collections.Generic;
using System.Text;

namespace TransportMotion
{

    public interface ICar
    {
        public string CarId { get => CarId; }
        public string PlaneId { get; set; }

        //motion
        public bool MotionPermitted { get; set; }        
        public int Speed { get; set; }
        public int LocationVertex { get; set; }
    }
}
