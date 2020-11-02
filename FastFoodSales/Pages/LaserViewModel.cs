using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using DAQ.Properties;
using DAQ.Service;
using MaterialDesignThemes.Wpf;
using Stylet;
using StyletIoC;

namespace DAQ.Pages
{
    public class LaserViewModel : Screen, IMainTabViewModel
    {
        private LaserService _laser;
        private IIoService _ioService;
        DispatcherTimer timer = new DispatcherTimer();
        public LaserViewModel([Inject]LaserService laser,[Inject]IIoService ioService)
        {
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += Timer_Tick;
            timer.Start();
            _laser = laser;
            _ioService = ioService;
            _laser.LaserHandler += _laser_LaserHandler;
        }
        private bool input;
        private bool output;
        public bool Input { get => input; set => SetAndNotify(ref input, value); }
        public bool Output { get => output; set => SetAndNotify(ref output, value); }

        private void Timer_Tick(object sender, EventArgs e)
        {
            Input = _ioService.GetInput(0);
            Output = _ioService.GetOutputs()[0];
           // throw new NotImplementedException();
        }

        public BindableCollection<Laser> Lasers { get; } = new BindableCollection<Laser>(){};

        private void _laser_LaserHandler(object sender, Laser e)
        {
            if (Lasers.Count > 5)
                Lasers.RemoveAt(0);
            Lasers.Add(e);
        }
        public int TabIndex { get; set; } = (int)Pages.TabIndex.LASER;
        public PackIconKind PackIcon { get; set; } = PackIconKind.FlashCircle;
        public string Header { get; set; } = "镭射";
        public bool Visible { get; set; } = true;

        public int markingNo { get; set; } = Settings.Default.MarkingNo;
        public async void GetMarkingNo()
        {
          markingNo = await Task.Run<int>(() =>_laser.GetMarkingNo());
          Settings.Default.MarkingNo = markingNo;
          Settings.Default.Save();
        }
    }
}
