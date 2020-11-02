using System;
using Stylet;

namespace DAQ.Service
{
    public static class PostMsg
    {
        public static void PostError(this IEventAggregator @event,Exception exception)
        {
            @event?.PublishOnUIThread(new MsgItem { Level = "E", Time = DateTime.Now, Value = exception.Message });
        }

        public static void PostMessage(this IEventAggregator @event, string message)
        {
            @event?.PublishOnUIThread(new MsgItem { Level = "D", Time = DateTime.Now, Value = message });
        }

    }
}