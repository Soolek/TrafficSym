using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace TrafficSym2D
{
    public static class TabLBMSerializer
    {
        public static TrafficSymGame parent;

        public static void SaveTabLBM(LBMElement[,] tab, int index)
        {
            //save the car list to a file
            ObjectToSerialize objectToSerialize = new ObjectToSerialize();
            objectToSerialize.tabLBM = tab;

            SerializeObject("tabLBM"+index.ToString()+".bin", objectToSerialize);
        }

        public static LBMElement[,] LoadTabLBM(int index)
        {
            if (File.Exists("tabLBM" + index.ToString() + ".bin"))
            {
                ObjectToSerialize objectToSerialize = DeSerializeObject("tabLBM" + index.ToString() + ".bin");
                return objectToSerialize.tabLBM;
            }
            else if (File.Exists("..\\..\\..\\vector_maps\\tabLBM" + index.ToString() + ".bin"))
            {
                ObjectToSerialize objectToSerialize = DeSerializeObject("..\\..\\..\\vector_maps\\tabLBM" + index.ToString() + ".bin");
                return objectToSerialize.tabLBM;
            }
            return null;
        }


        public static void SerializeObject(string filename, ObjectToSerialize objectToSerialize)
        {
            Stream stream = File.Open(filename, FileMode.Create);
            BinaryFormatter bFormatter = new BinaryFormatter();
            bFormatter.Serialize(stream, objectToSerialize);
            stream.Close();
        }

        public static ObjectToSerialize DeSerializeObject(string filename)
        {
            ObjectToSerialize objectToSerialize;
            Stream stream = File.Open(filename, FileMode.Open);
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
