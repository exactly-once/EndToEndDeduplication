﻿using System;
using System.Threading.Tasks;
using ExactlyOnce.ClaimCheck;
using NServiceBus.Pipeline;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    class LoadMessageBodyBehavior : Behavior<IIncomingPhysicalMessageContext>
    {
        IMessageStore messageStore;

        public LoadMessageBodyBehavior(IMessageStore messageStore)
        {
            this.messageStore = messageStore;
        }

        public override async Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
        {
            var messageBody = await messageStore.TryGet(context.MessageId).ConfigureAwait(false);
            if (messageBody == null)
            {
                //Message has been processed
                return;
            }
            context.UpdateMessage(messageBody);

            await next().ConfigureAwait(false);
        }
    }
}