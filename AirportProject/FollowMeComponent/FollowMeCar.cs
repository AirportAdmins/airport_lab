using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FollowMeComponent
{
    public enum Status
    {
        Free, Busy
    }
    public class FollowMeCar
    {
        string followMeId;
        public AutoResetEvent MotionPermission { get; set; } = new AutoResetEvent(false);
        public FollowMeCar(int id)
        {
            followMeId = "FollowMe-" + id;
            MotionPermitted = false;
            GotAirplaneResponse = false;
        }

        public string FollowMeId { get => followMeId; }
        public string PlaneId { get; set; }
        public Status Status { get; set; }

        //motion data
        public bool MotionPermitted { get; set; }
        public bool GotAirplaneResponse { get; set; }
        public static int Speed { get => 15; }
        public int LocationVertex { get; set; }
    }
}
