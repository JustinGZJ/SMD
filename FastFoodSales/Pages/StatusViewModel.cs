using System;
using System.Threading.Tasks;
using System.Timers;
using DAQ.Service;
using LiveCharts;
using LiveCharts.Defaults;
using Stylet;
using StyletIoC;

namespace DAQ.Pages
{
    public class StatusViewModel : Screen
    {
        [Inject] public IEventAggregator @event;
        public int StationId { get; }
        private int updatecircle = 0;
        public string StatusInfo { get; set; }
        public RunstatusService Service1 { get; }
        private DataStorage _storage;

        public ChartValues<ObservableValue> Runtime { get; set; } =
            new ChartValues<ObservableValue>()
                { new ObservableValue(1)};
        public ChartValues<ObservableValue> Stoptime { get; set; } =
            new ChartValues<ObservableValue>()
                { new ObservableValue(0)};
        public ChartValues<ObservableValue> Pretime { get; set; } =
            new ChartValues<ObservableValue>()
                { new ObservableValue(0)};

        public TimeSpan RunSpan { get; set; }
        public TimeSpan StopSpan { get; set; }
        public TimeSpan PreSpan { get; set; }

        private Timer timer = new Timer(1000);

        public StatusViewModel()
        {

        }

        public void ShowHistory()
        {
            @event.Publish(this, "STATUS");
        }
        public StatusViewModel(DataStorage storage, int stationId, IEventAggregator Event)
        {
            StationId = stationId;
            Service1 = new RunstatusService(stationId);
            _storage = storage;
            @event = Event;
            DisplayName = $"N{StationId}";
            //Task.Run(() =>
            //{
            //    while (true)
            //    {
            //        if (_storage[$"W_N{StationId}_STATUS"] is short v1)
            //        {
            //            Service1.SetStatus((ushort)v1);
            //            if (Service1.Status.StatusInfo != null)
            //            {
            //                StatusInfo = Service1.Status.StatusInfo.AlarmContent;
            //            }
            //        }
            //        System.Threading.Thread.Sleep(500);
            //    }
            //});
            //timer.Start();
            //timer.Elapsed += (s, e) =>
            //{
            //    if (StatusInfo != null)
            //    {
            //        if (StatusInfo == "运行中")
            //        {
            //            RunSpan += TimeSpan.FromSeconds(1);
            //        }
            //        else if (StatusInfo.Contains("准备中"))
            //        {
            //            PreSpan += (TimeSpan.FromSeconds(1));
            //        }
            //        else
            //        {
            //            StopSpan += (TimeSpan.FromSeconds(1));
            //        }
            //    }
            //    if (updatecircle++ > 10)
            //    {
            //        Runtime[0] = new ObservableValue(RunSpan.TotalSeconds);
            //        Stoptime[0] = new ObservableValue(StopSpan.TotalSeconds);
            //        Pretime[0] = new ObservableValue(PreSpan.TotalSeconds);
            //        updatecircle = 0;
            //        Refresh();
            //    }
            //};
        }
    }

}
