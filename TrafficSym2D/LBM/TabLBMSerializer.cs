using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace TrafficSym2D.LBM
{
    public class TabLBMSerializer
    {
        private string _configDirPath;

        public TabLBMSerializer(string configDirPath)
        {
            this._configDirPath = configDirPath;
        }
        public void SaveTabLBM(LBMElement[,] tab, int index)
        {
            //save the car list to a file
            ObjectToSerialize objectToSerialize = new ObjectToSerialize();
            objectToSerialize.tabLBM = tab;

            var lbmFileName = "tabLBM" + index.ToString() + ".bin";
            var lbmFilePath = Path.Combine(_configDirPath, lbmFileName);
            SerializeObject(lbmFilePath, objectToSerialize);
        }

        public LBMElement[,] LoadTabLBM(int index)
        {
            var lbmFileName = "tabLBM" + index.ToString() + ".bin";
            var lbmFilePath = Path.Combine(_configDirPath, lbmFileName);
            if (File.Exists(lbmFilePath))
            {
                ObjectToSerialize objectToSerialize = DeSerializeObject(lbmFilePath);
                return objectToSerialize.tabLBM;
            }
            return null;
        }

        public void SerializeObject(string filePath, ObjectToSerialize objectToSerialize)
        {
            Stream stream = File.Open(filePath, FileMode.Create);
            BinaryFormatter bFormatter = new BinaryFormatter();
            bFormatter.Serialize(stream, objectToSerialize);
            stream.Close();
        }

        public ObjectToSerialize DeSerializeObject(string filePath)
        {
            ObjectToSerialize objectToSerialize;
            Stream stream = File.Open(filePath, FileMode.Open);
            BinaryFormatter bFormatter = new BinaryFormatter();
            objectToSerialize = (ObjectToSerialize)bFormatter.Deserialize(stream);
            stream.Close();
            return objectToSerialize;
        }
    }

    [Serializable()]
    public class ObjectToSerialize : ISerializable
    {
        public LBMElement[,] tabLBM;

        public ObjectToSerialize()
        {
        }

        public ObjectToSerialize(SerializationInfo info, StreamingContext ctxt)
        {
            tabLBM = (LBMElement[,])info.GetValue("tabLBM", typeof(LBMElement[,]));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            info.AddValue("tabLBM", tabLBM);
        }
    }
}
