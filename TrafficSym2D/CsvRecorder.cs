using System;
using System.Collections.Generic;
using System.Globalization;
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
        private CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");

        public CsvRecorder(string csvFilePath)
        {
            OpenFile(csvFilePath);
            WriteHeader(_fileStream);
        }

        private void WriteHeader(StreamWriter _fileStream)
        {
            _fileStream.Write(string.Format(_format,"frame"));
            _fileStream.Write(string.Format(_format,"timestamp"));
            _fileStream.Write(string.Format(_format,"light_config_id"));
            _fileStream.Write(string.Format(_format,"car_Id"));
            _fileStream.Write(string.Format(_format,"car_position_X"));
            _fileStream.Write(string.Format(_format,"car_position_Y"));
            _fileStream.Write(string.Format(_format,"car_steer"));
            _fileStream.Write(string.Format(_format,"car_rotation"));
            _fileStream.Write(string.Format(_format,"car_aggressiveness"));
            _fileStream.Write(string.Format(_format,"car_velocity"));
            _fileStream.Write(string.Format(_format,"car_acceleration"));
            _fileStream.WriteLine(string.Format(_format, "car_brake"));
        }

        public void AddData(long frame, TimeSpan timestamp, int lightConfigId, Car car)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(culture, _format, frame);
            sb.AppendFormat(culture, _format, timestamp);
            sb.AppendFormat(culture, _format, lightConfigId);
            sb.AppendFormat(culture, _format, car.Id);
            sb.AppendFormat(culture, _format, car.position.X);
            sb.AppendFormat(culture, _format, car.position.Y);
            sb.AppendFormat(culture, _format, car.steer * 180f / Math.PI); //steer in deg
            sb.AppendFormat(culture, _format, car.rotation * 180f / Math.PI); //rotation in deg
            sb.AppendFormat(culture, _format, car.aggressiveness);
            sb.AppendFormat(culture, _format, car.velocity);
            sb.AppendFormat(culture, _format, car.userAcc > 0 ? car.userAcc : 0);
            sb.AppendFormat(culture, _format, car.userAcc < 0 ? -car.userAcc : 0); //brake

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
