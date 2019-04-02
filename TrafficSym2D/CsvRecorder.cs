using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrafficSym2D
{
    public class CsvRecorder : IDisposable
    {
        private const string _format = "{0};";

        private StreamWriter _fileStream;
        private Dictionary<int, string> _data = new Dictionary<int, string>();

        public CsvRecorder(string csvFilePath)
        {
            OpenFile(csvFilePath);
        }

        public void AddData(long frame, TimeSpan timestamp, int lightConfigId, Car car)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(_format, frame);
            sb.AppendFormat(_format, timestamp);
            sb.AppendFormat(_format, lightConfigId);
            sb.AppendFormat(_format, car.Id);
            sb.AppendFormat(_format, car.position.X);
            sb.AppendFormat(_format, car.position.Y);
            sb.AppendFormat(_format, car.steer * 180f / Math.PI); //steer in deg
            sb.AppendFormat(_format, car.rotation * 180f / Math.PI); //rotation in deg
            sb.AppendFormat(_format, car.aggressiveness);
            sb.AppendFormat(_format, car.velocity);
            sb.AppendFormat(_format, car.userAcc > 0 ? car.userAcc : 0);
            sb.AppendFormat(_format, car.userAcc < 0 ? -car.userAcc : 0); //brake

            _data[car.Id] = sb.ToString();
        }

        void OpenFile(string filePath)
        {
            _fileStream = new StreamWriter(filePath);
        }

        void CloseFile()
        {
            if (_fileStream != null)
            {
                _fileStream.Flush();
                _fileStream.Dispose();
            }
        }

        public void Flush()
        {
            if (_fileStream == null)
                return;

            foreach(var carData in _data.Values)
            {
                _fileStream.WriteLine(carData);
            }
            _data.Clear();
        }

        public void Dispose()
        {
            Flush();
            CloseFile();
        }

    }
}
