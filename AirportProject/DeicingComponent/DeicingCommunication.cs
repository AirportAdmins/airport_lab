using System;
using System.Collections.Generic;
using System.Text;

namespace DeicingComponent
{
    partial class DeicingComponent
    {


        private void MessageFromGroundService()
        {
            mqClient.SubscribeTo<BaggageServiceCommand>(queueFromGroundService, (bsc) =>
            {

            });

        }
    }
}
