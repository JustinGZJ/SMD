using System;
using Stylet;
using StyletIoC;
using System.IO.Ports;
using DAQ.Service;

namespace DAQ
{
    public class Bootstrapper : Bootstrapper<MainWindowViewModel>
    {
        protected override void ConfigureIoC(IStyletIoCBuilder builder)
        {
            // Configure the IoC container in here
            builder.Bind<IEventAggregator>().To<EventAggregator>().InSingletonScope();
            builder.Bind<PortAService>().ToSelf().InSingletonScope();
            builder.Bind<PortBService>().ToSelf().InSingletonScope();
            builder.Bind<HomeViewModel>().ToSelf();
            builder.Autobind();

        }

        protected override void Configure()
        {
            
            // Perform any other configuration before the application starts
        }
    }
}
