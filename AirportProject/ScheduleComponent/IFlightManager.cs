using System;
using System.Collections.Generic;
using System.Text;

namespace ScheduleComponent
{
    interface IFlightManager
    {
        void GenerateNewFlight();
        void SetCurrentTime(DateTime currentTime);
    }
}
