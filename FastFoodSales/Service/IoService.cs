using System.Threading;
using System.Threading.Tasks;
using HslCommunication.ModBus;

namespace DAQ.Service
{
    public class IoService : IIoService
    {
        private ModbusTcpNet modbusTcp;
        private bool[] _inputs = new bool[4];
        private bool[] _outputs = new bool[4];
        public bool IsConnected { get; private set; }

        public IoService()
        {
            modbusTcp = new ModbusTcpNet { IpAddress = "192.168.0.240", Port = 502, AddressStartWithZero = false };
            modbusTcp.ConnectServer();
            Task.Run(() =>
            {
                while (true)
                {
                    var r1 = modbusTcp.ReadCoil("1");
                    IsConnected = r1.IsSuccess;
                    if (r1.IsSuccess)
                    {
                        _inputs[0] = r1.Content;
                        modbusTcp.WriteCoil("17", _outputs[0]);
                    }
                    Thread.Sleep(10);
                }
            });
        }

        public bool GetInput(uint index)
        {
            return index < 4 && _inputs[index];
        }

        public bool[] GetOutputs()
        {
            return _outputs;
        }

        public void SetOutput(uint index, bool value)
        {
            if (index < (_outputs.Length - 1))
                _outputs[index] = value;
        }
    }
}