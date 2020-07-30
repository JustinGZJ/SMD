using System;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DAQ.Pages;
using DAQ.Properties;
using SimpleTCP;
using Stylet;
using StyletIoC;

namespace DAQ.Service
{
    public interface IIoService
    {
        bool GetInput(uint index);
        void SetOutput(uint index, bool value);
        bool IsConnected { get; }
    }

    public class LaserService : IDisposable
    {
        IEventAggregator Events;
        private readonly FileSaverFactory _factory;
        SimpleTcpClient _laserClient = null;

        private bool laserFinish = false;
        Properties.Settings settings = Settings.Default;
        private IIoService _ioService;
        public event EventHandler<Laser> LaserHandler;

        public LaserService([Inject] IEventAggregator @event, [Inject] IIoService ioService, [Inject] FileSaverFactory factory)
        {
            Events = @event;
            _factory = factory;
            _ioService = ioService;
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


            Task.Run(async () =>
            {
                _ioService.SetOutput(0, false);
                while (true)
                {
                    if (!_ioService.IsConnected)
                    {
                        Events.PostError("远程IO未连接");
                        await Task.Delay(500);
                    }
                    var input = _ioService.GetInput(0);
                    if (input)
                    {
                        GetProcTime("获取镭射等级", () =>
                         {
                             GetLaserData();
                         });

                        SpinWait.SpinUntil(() => _ioService.GetInput(0) == false);
                    }
                    await Task.Delay(10);
                }
            });
        }
        private void GetProcTime(String name, Action action)
        {

            Stopwatch stopwatch = new Stopwatch();
            Events.PostMessage(name + "开始");
            stopwatch.Start();
            action();
            stopwatch.Stop();
            Events.PostMessage($"{name}耗时: " + stopwatch.ElapsedMilliseconds / 1000.0 + "S");
        }

        public void GetLaserData()
        {
            _ioService.SetOutput(0, true);
            var enables = new bool[] { settings.checkLoc1, settings.checkLoc2, settings.checkLoc3 };

            if (true)  //读取二维码等级
            {
                for (int i = 0; i < 3; i++)
                {
                    if (enables[i])
                    {
                        var index = i;
                        GetProcTime($"读取第{i}组镭射二维码", () => { ReadLaserCode(index); });
                    }
                }
            }
            _ioService.SetOutput(0, false);
        }

        private void ReadLaserCode(int i)
        {
            var locations = new string[] { settings.LaserLoc1, settings.LaserLoc2, settings.LaserLoc3 };     
            for (int j = 0; j < 2; j++)
            {
                string cmd = $"WX,Check2DCode=1,{locations[i]}{Environment.NewLine}";
                Events.PostMessage($"LASER SEND:{cmd}");
                var m1 = _laserClient.WriteLineAndGetReply(cmd, TimeSpan.FromMilliseconds(3000));
                if (m1 != null)
                {
                    Events.PostMessage($"LASER RECV: {m1.MessageString}");
                    var splits = m1.MessageString.Split(',');
                    var nunit = i + 1;
                    if (splits.Length >= 3)
                    {
                        if (m1.MessageString.ToUpper().Contains("WX,OK"))
                        {
                            if (!(splits[2].Contains("A") || splits[2].Contains("B")))
                            {
                                continue;
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
                            _factory.GetFileSaver<Laser>((nunit).ToString()).Save(laser);
                            _factory.GetFileSaver<Laser>((nunit).ToString(), @"D:\\SumidaFile\Monitor").Save(laser);
                            break;
                        }
                        else
                        {
                            cmd = $"UY,{settings.MarkingNo.ToString().PadLeft(3, '0')},{(nunit - 1).ToString().PadLeft(3, '0')},0{Environment.NewLine}";
                            Events.PostMessage($"LASER SEND: {cmd}");
                            var reply = _laserClient.WriteLineAndGetReply(cmd, TimeSpan.FromMilliseconds(2000));
                            if (reply == null) return;
                            Events.PostMessage($"LASER RECV: {reply.MessageString}");
                            SaveLaserLog2(reply, nunit);
                            break;
                        }
                    }
                    else
                    {
                        Events.PostError(new Exception("Format error " + splits[2]));
                        break;
                    }
                }
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
                        if (splits[2].Length > 5)
                        {
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
                            _factory.GetFileSaver<Laser>((nunit).ToString()).Save(laser);
                            _factory.GetFileSaver<Laser>((nunit).ToString(), @"D:\\SumidaFile\Monitor").Save(laser);
                        }
                        else
                        {
                        }
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
            LaserHandler?.Invoke(this, e);
        }
    }
}