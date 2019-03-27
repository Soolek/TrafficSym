using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TrafficSym2D
{
    [Serializable()]
    public class LBMElement : ISerializable
    {
        private float _x, _y;
        private float _density;
        public byte nodeType;

        public LBMElement()
        {
            x = y = 0f;
            _density = 1f;
            nodeType = 0;
        }

        public LBMElement(SerializationInfo info, StreamingContext ctxt)
        {
            this._x = (float)info.GetValue("_x", typeof(float));
            this._y = (float)info.GetValue("_y", typeof(float));
            this._density = (float)info.GetValue("_density", typeof(float));
            this.nodeType = (byte)info.GetValue("nodeType", typeof(byte));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            info.AddValue("_x", this._x);
            info.AddValue("_y", this._y);
            info.AddValue("_density", this._density);
            info.AddValue("nodeType", this.nodeType);
        }

        public bool isNormal
        {
            get
            {
                return (nodeType == 0);
            }
            set
            {
                if (value)
                    nodeType = 0;
            }
        }

        public bool isWall
        {
            get
            {
                return (nodeType == 1);
            }
            set
            {
                if (value)
                    nodeType = 1;
            }
        }

        public bool isSource
        {
            get
            {
                return (nodeType == 2);
            }
            set
            {
                if (value)
                    nodeType = 2;
            }
        }

        public bool isHole
        {
            get
            {
                return (nodeType == 3);
            }
            set
            {
                if (value)
                    nodeType = 3;
            }
        }

        public float density
        {
            get
            {
                if (isSource) return 1f;
                else
                    if (isHole) return -1f;
                    else
                        if (isWall) return 0f;
                        else
                            return _density;
            }
            set
            {
                if (!isSource && !isHole) _density = value;
            }
        }

        public float max
        {
            get
            {
                float _max;
                if (Math.Abs(_x) > Math.Abs(_y))
                    _max = Math.Abs(_x);
                else
                    _max = Math.Abs(_y);

                if (_max < 0.0001f) _max = 0.0001f;
                return _max;
            }
        }

        public float x
        {
            get
            {
                if (!isNormal) return 0f;
                else return _x / max;
            }
            set
            {
                _x = value;
            }
        }
        public float y
        {
            get
            {
                if (!isNormal) return 0f;
                else return _y / max;
            }
            set
            {
                _y = value;
            }
        }
    }
}
