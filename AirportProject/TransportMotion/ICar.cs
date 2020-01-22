using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace TransportMotion
{

    public interface ICar
    {
        public string CarId { get; }
        public string PlaneId { get; set; }

        //motion
        public bool MotionPermission { get; set; }        
        public int Speed { get; set; }
        public int LocationVertex { get; set; }
    }
}
