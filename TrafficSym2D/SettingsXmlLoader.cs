using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace TrafficSym2D
{
    public class XmlSettings
    {
        public List<RouteConfig> RouteConfigList { get; private set; }
        public List<RouteWall> LightList {get; private set;}
        public List<LightConfig> LightConfigList { get; private set; }

        private TrafficSymGame _parent;

        private XmlSettings(TrafficSymGame parent)
        {
            RouteConfigList = new List<RouteConfig>();
            LightList = new List<RouteWall>();
            LightConfigList = new List<LightConfig>();

            this._parent = parent;
        }

        public static XmlSettings CreateFromFile(string xmlFilePath, TrafficSymGame parent)
        {
            var xmlSettings = new XmlSettings(parent);
            xmlSettings.LoadDataFromXML(xmlFilePath);
            return xmlSettings;
        }

        private void LoadDataFromXML(string xmlFilePath)
        {
            XmlDocument xdoc = new XmlDocument();
            if (File.Exists(xmlFilePath))
            {
                xdoc.Load(xmlFilePath);
            }
            else
            {
                throw new IOException(string.Format("Missing settings file: {0}", xmlFilePath));
            }

            #region ladowanie routeConfigs
            XmlElement xmlConfig = xdoc["TrafficSym2D"]["routeConfigs"];
            for (int i = 0; i < xmlConfig.ChildNodes.Count; i++)
            {
                XmlNode xmlNodeMain = xmlConfig.ChildNodes.Item(i);
                RouteConfig routeConfig = new RouteConfig();
                routeConfig.timeBetweenCarsMs = Convert.ToInt32(xmlNodeMain.Attributes["timeBetweenCarsMs"].Value);
                routeConfig.initialSpeed = xmlNodeMain.Attributes["initialSpeed"] != null ? float.Parse(xmlNodeMain.Attributes["initialSpeed"].Value) : 0;

                for (int i2 = 0; i2 < xmlNodeMain.ChildNodes.Count; i2++)
                {
                    XmlNode xmlNode = xmlNodeMain.ChildNodes.Item(i2);

                    int x1 = Convert.ToInt32(xmlNode.Attributes["x1"].Value);
                    int y1 = Convert.ToInt32(xmlNode.Attributes["y1"].Value);
                    int x2 = Convert.ToInt32(xmlNode.Attributes["x2"].Value);
                    int y2 = Convert.ToInt32(xmlNode.Attributes["y2"].Value);

                    if (xmlNode.Name.Equals("start") || xmlNode.Name.Equals("end"))
                    {
                        int temp;
                        if (x1 > x2)
                        {
                            temp = x1;
                            x1 = x2;
                            x2 = temp;
                        }
                        if (y1 > y2)
                        {
                            temp = y1;
                            y1 = y2;
                            y2 = temp;
                        }
                    }

                    if (x1 > (_parent.resX - _parent.elementSize)) x1 = (_parent.resX - _parent.elementSize);
                    if (x2 > (_parent.resX - _parent.elementSize)) x2 = (_parent.resX - _parent.elementSize);
                    if (y1 > (_parent.resY - _parent.elementSize)) y1 = (_parent.resY - _parent.elementSize);
                    if (y2 > (_parent.resY - _parent.elementSize)) y2 = (_parent.resY - _parent.elementSize);
                    if (x1 < _parent.elementSize) x1 = _parent.elementSize;
                    if (x2 < _parent.elementSize) x2 = _parent.elementSize;
                    if (y1 < _parent.elementSize) y1 = _parent.elementSize;
                    if (y2 < _parent.elementSize) y2 = _parent.elementSize;

                    switch (xmlNode.Name)
                    {
                        case "start":
                            {
                                routeConfig.routeStart.Add(new RouteStart(
                                    x1, y1, x2, y2,
                                    (float)Convert.ToInt32(xmlNode.Attributes["direction"].Value)
                                    ));
                            }; break;

                        case "end":
                            {
                                routeConfig.routeEnd.Add(new RouteEnd(
                                    x1, y1, x2, y2
                                    ));
                            }; break;

                        case "wall":
                            {
                                routeConfig.routeWall.Add(new RouteWall(
                                    x1, y1, x2, y2
                                    ));
                            }; break;
                    }
                }
                RouteConfigList.Add(routeConfig);
            }
            #endregion

            if (xdoc["TrafficSym2D"]["lightConfig"] != null)
            {
                #region ladowanie lightConfig.lights
                xmlConfig = xdoc["TrafficSym2D"]["lightConfig"]["lights"];
                if (xmlConfig != null)
                    for (int i = 0; i < xmlConfig.ChildNodes.Count; i++)
                    {
                        XmlNode xmlNode = xmlConfig.ChildNodes.Item(i);
                        int x1 = Convert.ToInt32(xmlNode.Attributes["x1"].Value);
                        int y1 = Convert.ToInt32(xmlNode.Attributes["y1"].Value);
                        int x2 = Convert.ToInt32(xmlNode.Attributes["x2"].Value);
                        int y2 = Convert.ToInt32(xmlNode.Attributes["y2"].Value);

                        if (x1 > (_parent.resX - _parent.elementSize)) x1 = (_parent.resX - _parent.elementSize);
                        if (x2 > (_parent.resX - _parent.elementSize)) x2 = (_parent.resX - _parent.elementSize);
                        if (y1 > (_parent.resY - _parent.elementSize)) y1 = (_parent.resY - _parent.elementSize);
                        if (y2 > (_parent.resY - _parent.elementSize)) y2 = (_parent.resY - _parent.elementSize);
                        if (x1 < _parent.elementSize) x1 = _parent.elementSize;
                        if (x2 < _parent.elementSize) x2 = _parent.elementSize;
                        if (y1 < _parent.elementSize) y1 = _parent.elementSize;
                        if (y2 < _parent.elementSize) y2 = _parent.elementSize;

                        LightList.Add(new RouteWall(x1, y1, x2, y2));
                    }
                #endregion

                #region ladowanie lightConfig.configs
                xmlConfig = xdoc["TrafficSym2D"]["lightConfig"]["configs"];
                if (xmlConfig != null)
                    for (int i = 0; i < xmlConfig.ChildNodes.Count; i++)
                    {
                        XmlNode xmlNodeMain = xmlConfig.ChildNodes.Item(i);
                        LightConfig lc = new LightConfig();
                        lc.timeToWaitMs = Convert.ToInt32(xmlNodeMain.Attributes["timeToWaitMs"].Value);
                        lc.comment = xmlNodeMain.Attributes["comment"].Value;

                        for (int i2 = 0; i2 < xmlNodeMain.ChildNodes.Count; i2++)
                        {
                            XmlNode xmlNode = xmlNodeMain.ChildNodes.Item(i2);
                            lc.lightId.Add(Convert.ToInt32(xmlNode.Attributes["id"].Value));
                        }
                        LightConfigList.Add(lc);
                    }
                #endregion
            }
        }
    }
}
