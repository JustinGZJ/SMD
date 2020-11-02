using System;
using System.CodeDom;
using System.Collections;
using Stylet;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Text;
using HslCommunication.Core;

using StyletIoC;

namespace DAQ.Service
{
    public class DataStorage : PropertyChangedBase
    {
        private const string DEFAULT_PATH = "./Settings/DataValues.json";
        IReadWriteNet _readwriter;

        
        private IByteTransform _transform;
        private IEventAggregator _events;

        private int PDU = 100;
      

       static  object _enable = new object();
         public static object Locker=>_enable;
        public void Load(string FilePath = DEFAULT_PATH)
        {
            lock (_enable)
            {
                if (!File.Exists(FilePath))
                    return;
                var str = File.ReadAllText(FilePath);
                try
                {
                    var obj = JsonConvert.DeserializeObject<IEnumerable<VAR>>(str);
                    DataValues.Clear();
                    foreach (var a in obj)
                    {
                        DataValues.Add(a);
                    }
                }
                catch (Exception ex)
                {

                }            
            }
        }
        public  void Save(string FilePath = DEFAULT_PATH)
        {
            string path = Path.GetDirectoryName(FilePath);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            var v = DataValues.Select(x => new
            {
                x.Name,
                x.StartIndex,
                x.Type,
                x.Tag
            });
            File.WriteAllText(FilePath, JsonConvert.SerializeObject(v));
        }
        public DataStorage([Inject]IReadWriteNet readwriter,[Inject]IByteTransform transform,[Inject] IEventAggregator events)
        {
   
            this._events = events;
            this._transform = transform;
            _readwriter = readwriter;
            Load();
           // Task.Run(() =>
           //{
           //    while (true)
           //    {
           //        lock (_enable)
           //        {
           //         //   对数据进行分组，每组内间隔不超过PDU
           //            int arrIndex = 0;
           //            var sortedVars = DataValues.OrderBy(x => x.StartIndex).ToList();
           //            var arrList = new List<List<VAR>> { new List<VAR>() { sortedVars[0] } };
           //            for (int i = 1; i < sortedVars.Count; i++)
           //            {
           //                var a = sortedVars[i].StartIndex + sortedVars[i].GetLength();
           //                var b = sortedVars[i - 1].StartIndex;
           //                if (a - b < PDU)
           //                {
           //                    arrList[arrIndex].Add(sortedVars[i]);
           //                }
           //                else
           //                {
           //                    arrList.Add(new List<VAR>());
           //                    arrIndex++;
           //                }
           //            }
           //            foreach (var list in arrList)
           //            {
           //             //   获取组内需要请求的字数
           //                int nShorts = 0;
           //                foreach (var s in list)
           //                {
           //                    nShorts += s.GetLength();
           //                }
           //                if (nShorts > 10)
           //                {
           //                    var s = list.First().StartIndex;
           //                    var l = list.Last().GetLength() + list.Last().StartIndex;
           //                    var r = _readwriter.Read($"x:3;{s}", (ushort)(l - s));
           //                    if (r.IsSuccess)
           //                    {
           //                        var buffer = r.Content;
           //                        foreach (var v in list)
           //                        {
           //                            switch (v.Type)
           //                            {
           //                                case TYPE.BOOL:
           //                                    var cs = transform.TransUInt16(buffer, (v.StartIndex - s) * 2);
           //                                    v.Value = (cs & (1 << v.Tag)) > 0;
           //                                    break;
           //                                case TYPE.FLOAT:
           //                                    v.Value = transform.TransSingle(buffer, (v.StartIndex - s) * 2);
           //                                    break;
           //                                case TYPE.INT:
           //                                    v.Value = transform.TransInt32(buffer, (v.StartIndex - s) * 2);
           //                                    break;
           //                                case TYPE.SHORT:
           //                                    v.Value = transform.TransInt16(buffer, (v.StartIndex - s) * 2);
           //                                    break;
           //                                case TYPE.STRING:
           //                                    v.Value = transform.TransString(buffer, (v.StartIndex - s) * 2, v.Tag, Encoding.UTF8);
           //                                    break;
           //                            }
           //                            v.Time = DateTime.Now;
           //                        }
           //                    }
           //                    else
           //                    {
           //                        _events.Publish(new MsgItem()
           //                        {
           //                            Level = "E",
           //                            Time = DateTime.Now,
           //                            Value = "Read from data server fail"
           //                        });
           //                    }
           //                }
           //                else
           //                {
           //                    foreach (var v in list)
           //                    {

           //                        switch (v.Type)
           //                        {
           //                            case TYPE.BOOL:
           //                                var r = _readwriter.ReadUInt16($"x=3;{v.StartIndex}");
           //                                v.Value = (r.Content & (1 << v.Tag)) > 0;
           //                                break;
           //                            case TYPE.FLOAT:
           //                                var f = _readwriter.ReadFloat($"x=3;{v.StartIndex}");
           //                                v.Value = f.Content;
           //                                break;
           //                            case TYPE.INT:
           //                                var i = _readwriter.ReadInt32($"x=3;{v.StartIndex}");
           //                                v.Value = i.Content;
           //                                break;
           //                            case TYPE.STRING:
           //                                var s = _readwriter.ReadString($"x=3;{v.StartIndex}", (ushort)v.Tag);
           //                                v.Value = s.Content;
           //                                break;
           //                            case TYPE.SHORT:
           //                                var us = _readwriter.ReadInt16($"x=3;{v.StartIndex}");
           //                                v.Value = us.Content;
           //                                break;
           //                        }
           //                        v.Time = DateTime.Now;
           //                    }
           //                }
           //            }
           //            Refresh();
           //        }

           //        System.Threading.Thread.Sleep(100);
           //    }
           //});
        }
        public void AddItem(VAR var)
        {
            lock (_enable)
            {
                DataValues.Add(var);
            } 
            Save();
        }
        public void RemoveItem(VAR var)
        {
            lock (_enable)
            {
                DataValues.Remove(var);
            }
            Save();
        }

        public void ModifyItem(VAR SelectedItem, VAR v)
        {
            lock (_enable)
            {
                SelectedItem.Name = v.Name;
                SelectedItem.StartIndex = v.StartIndex;
                SelectedItem.Type = v.Type;
                SelectedItem.Tag = v.Tag;
            }
            Save();
        }
        public object this[string Name]
        {
            set
            {
                if (DataValues.Any(x => x.Name == Name))
                    DataValues.First(x => x.Name == Name).Value = value;
            }
            get
            {
                if (DataValues.Any(x => x.Name == Name))
                {
                    object v= DataValues.First(x => x.Name == Name).Value;
                    return v;
                }              
                else
                {
                    return new object();
                }
            }
        }
        public BindableCollection<VAR> DataValues { get; set; } = new BindableCollection<VAR>();

    }
}