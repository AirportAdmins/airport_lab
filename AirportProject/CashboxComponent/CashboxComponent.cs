using RabbitMqWrapper;
using System;
using System.Collections.Generic;
using System.Text;

namespace CashboxComponent
{
    class CashboxComponent
    {
        RabbitMqClient mqClient = new RabbitMqClient();

        public void Start()
        {
            mqClient.DeclareQueues(queues.ToArray());
            mqClient.PurgeQueues(queues.ToArray());


        }
    }
}
