using System;
using System.Diagnostics;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using DAQ.Pages;
using DAQ.Properties;
using Serilog;
using SimpleTCP;
using Stylet;
using StyletIoC;

namespace DAQ.Service
{
    public interface IIoService
    {
        bool GetInput(uint index);
        void SetOutput(uint index, bool value);
        bool[] GetOutputs();
        bool IsConnected { get; }
    }

    public class LaserService : IDisposable
    {
        IEventAggregator Events;
        private readonly FileSaverFactory _factory;
        private readonly ILogger logger;
        SimpleTcpClient _laserClient = null;
        Settings settings = Settings.Default;
        private IIoService _ioService;
        public event EventHandler<Laser> LaserHandler;
        ActionBlock<(int, Message)> block;
        ActionBlock<(int, Message)> block2;

        public LaserService([Inject] IEventAggregator @event, [Inject] IIoService ioService, [Inject] FileSaverFactory factory,[Inject] ILogger logger) 
        {
            Events = @event;
            _factory = factory;
            this.logger = logger;
            _ioService = ioService;
            enables = new bool[] { settings.checkLoc1, settings.checkLoc2, settings.checkLoc3 };
            locations = new string[] { settings.LaserLoc1, settings.LaserLoc2, settings.LaserLoc3 };
            block = new ActionBlock<(int, Message)>(f =>
             {
                 var splits = f.Item2.MessageString.Split(',');
                 if (splits.Length < 4)
                 {
                     return;
                 }
                 var laser = new Laser
                 {
                     BobbinCode = splits[3].Trim('\r', '\n'),
                     BobbinLotNo = settings.BobbinLotNo,
                     LineNo = settings.LineNo,
                     Shift = settings.Shift,
                     CodeQuality = splits[2],
                     ProductionOrder = settings.ProductionOrder,
                     BobbinPartName = settings.BobbinPartName,
                     EmployeeNo = settings.EmployeeNo,
                     MachineNo = settings.MachineNo,
                     BobbinCavityNo = settings.BobbinCavityNo,
                     BobbinToolNo = settings.BobbinToolNo,
                     ShiftName = settings.ShiftName
                 };
                 OnLaserHandler(laser);
                 _factory.GetFileSaver<Laser>((f.Item1).ToString()).Save(laser);
                 _factory.GetFileSaver<Laser>((f.Item1).ToString(), @"D:\\SumidaFile\Monitor").Save(laser);
             })
            ;
            block2 = new ActionBlock<(int, Message)>(f =>
            {
                var splits = f.Item2.MessageString.Split(',');
                if (!int.TryParse(splits[2].Substring(splits[2].Length - 5), out int value)) return;
                var code = splits[2].Substring(0, splits[2].Length - 6) + (value - 2).ToString().PadLeft(5, '0');
                var laser = new Laser
                {
                    BobbinCode = code.Trim('\r', '\n'),
                    BobbinLotNo = settings.BobbinLotNo,
                    LineNo = settings.LineNo,
                    Shift = settings.Shift,
                    CodeQuality = "E",
                    ProductionOrder = settings.ProductionOrder,
                    EmployeeNo = settings.EmployeeNo,
                    MachineNo = settings.MachineNo,
                    BobbinPartName = settings.BobbinPartName,
                    BobbinCavityNo = settings.BobbinCavityNo,
                    BobbinToolNo = settings.BobbinToolNo,
                    ShiftName = settings.ShiftName
                };
                OnLaserHandler(laser);
                _factory.GetFileSaver<Laser>((f.Item1).ToString()).Save(laser);
                _factory.GetFileSaver<Laser>((f.Item1).ToString(), @"D:\\SumidaFile\Monitor").Save(laser);
            });
            Task.Run(() =>
            {
                CreateServer();
            });
        }

        public int GetMarkingNo()
        {

            Events.PostMessage($"LASER SEND:FE");
            var m = _laserClient.WriteLineAndGetReply("FE" + Environment.NewLine, TimeSpan.FromMilliseconds(1000));
            Events.PostMessage($"LASER RECV:{m?.MessageString}");
            if (m != null && m.MessageString.Contains("FE,0"))
            {

                if (int.TryParse(m.MessageString.Trim('\r', '\n').Substring(5), out int num))
                {

                    return num;
                }
                else
                {
                    return -1;
                }
            }
            else
            {
                return -1;
            }
        }
        IObservable<bool> trigger;
        public void CreateServer()
        {
            try
            {
                _laserClient?.Disconnect();
                _laserClient = new SimpleTcpClient();
                _laserClient.Connect("192.168.0.239", 9004);
                Events.Publish(new MsgItem { Level = "D", Time = DateTime.Now, Value = "Server initialize: " + IPAddress.Any.ToString() + ":9004" });
            }
            catch (Exception EX)
            {
                Events.PostError(EX);
                return;
            }
            _ioService.SetOutput(0, false);
            trigger = Observable.Interval(TimeSpan.FromMilliseconds(10)).Select((x) => _ioService.GetInput(0));

            trigger.Buffer(2, 1).Where(m => ((!m[0]) && (m[1]))).Subscribe((m) =>
            {
                GetProcTime("获取镭射数据", GetLaserData);
            });
          //  trigger.OnNext(false);

        }
        private void GetProcTime(string name, Action action)
        {

            Stopwatch stopwatch = new Stopwatch();
            logger.Information($"{name}开始");
            //  Events.PostMessage(name + "开始");
            stopwatch.Start();
            try
            {
                action();
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
               // throw;
            }
            stopwatch.Stop();
            logger.Information($"{name}耗时: " + stopwatch.ElapsedMilliseconds / 1000.0 + "S");
        }
        bool[] enables;
        string[] locations;
        public void GetLaserData()
        {
           _ioService.SetOutput(0, true);
            GetProcTime("循环3次", () =>
            {
                for (int i = 0; i < 3; i++)
                {
                    if (enables[i])
                    {
                        var index = i;
                        GetProcTime($"读取第{i + 1}组镭射二维码", () => { ReadLaserCode(index); });
                    }
                }
            });

          _ioService.SetOutput(0, false);
        }

        private void ReadLaserCode(int i)
        {

            string cmd = $"WX,Check2DCode=1,{locations[i]}{Environment.NewLine}";
            //     Events.PostMessage($"LASER SEND:{cmd}");
            var m1 = _laserClient.WriteLineAndGetReply(cmd, TimeSpan.FromMilliseconds(2000));
            if (m1 != null)
            {
                var nunit = i + 1;
                //         Events.PostMessage($"LASER RECV: {m1.MessageString}");
                if (m1.MessageString.Contains("WX,OK"))
                {

                    block.Post((nunit, m1));
                }
                else
                {
                    cmd = $"UY,{settings.MarkingNo.ToString().PadLeft(3, '0')},{(nunit - 1).ToString().PadLeft(3, '0')},0{Environment.NewLine}";
                    //                 Events.PostMessage($"LASER SEND: {cmd}");
                    var reply = _laserClient.WriteLineAndGetReply(cmd, TimeSpan.FromMilliseconds(2000));
                    if (reply == null) return;
                    //                   Events.PostMessage($"LASER RECV: {reply.MessageString}");
                    SaveLaserLog2(reply, nunit);
                }

            }
            else
            {
                Events.PostError(new Exception("接收超时"));
            }
        }



        private void SaveLaserLog2(Message m1, int nunit)
        {

            var splits = m1.MessageString.Split(',');
            if (splits.Length >= 3)
            {
                if (int.TryParse(splits[1], out int result))
                {
                    if (result != 0)
                    {
                        Events.PostError(new Exception("get laser info error.code " + splits[2]));
                    }
                    else
                    {
                        block2.Post((nunit, m1));
                    }
                }
                else
                {
                    Events.PostError(new Exception("Format error " + splits[2]));
                }
            }
        }

        public void Dispose()
        {
            _laserClient?.Disconnect();
        }

        protected virtual void OnLaserHandler(Laser e)
        {
            LaserHandler?.BeginInvoke(this, e, null, null);
        }
    }
}