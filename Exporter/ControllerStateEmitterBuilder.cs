/*!
 * RetroExporter.ControllerStateEmitterBuilder
 *
 * Copyright (c) 2022 Robert_Ordis
 *
 * Released under the MIT license.
 * see https://opensource.org/licenses/MIT
 *
 */

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;


/*
<plotter name="WindWaker-MSS" author="Robert_Ordis" type="gamecube">
    <spy-export>
        <local port="55555" threshold="512 protocol="udp"/>
        <dest ip="127.0.0.1" port="8934" protocol="udp"/>
        <dest ip="127.0.0.1" port="8934" protocol="udp"/>
        </spy-export>
    <plot-reading>
    <!-- this will also be used as dest above. -->
        <local port="8934" protocol="udp" rate_ms=100/>
        <dest dir="C:\retro-plotter\gamecube\windwaker"/>
    </plot-reading>

    <!-- Determin which input you are going to export and plot.-->
    <!-- coeff->val to multiply for plotting(default=1) -->
    <!-- name->Same as the "name" on skin.xml(MUST) -->
    <!-- pack->Name on packing to packet. Shorter please.(default=same as "name") -->
    <!-- group->Which panel do you want to be plotted?(default=0) -->
    <!-- color->Not necessary to describe(default=#00FF00FF) -->
    <mapping name="a" coeff="128"/>
    <mapping name="start" pack="st" coeff="128" group="1"/>
    <mapping name="lstick_y_raw" pack="lstick_y" group="1" color="#RRGGBBAA"/>
    <mapping name="lstick_x_raw" pack="lstick_x" group="1" color="#RRGGBBAA" default="false"/>
</plotter>
 * 
 */
namespace RetroExporter
{
    public class ControllerStateEmitterBuilder
    {
        private Dictionary<string, ControllerStateEmitter> readEmitters;
        private ControllerStateEmitterBuilder()
        {
            //registered Emitter.
            this.readEmitters = new Dictionary<string, ControllerStateEmitter>();
        }

        public ControllerStateEmitter ReadXml(string path)
        {
            ControllerStateEmitter res;

            var opt = LoadOptions.SetLineInfo;
            var doc = XDocument.Load(path, opt);
            var root = doc.Root;
            XElement tmp;
            int mappingCount = 0;
            if (root.Name != "plotter")
            {
                throw new ConfigurationErrorsException("Root elem must be named as \"plotter\".");
            }
            tmp = root.Element("spy-export");
            res = this.loadSpyExport(tmp);

            tmp = root.Element("plot-reading");
            this.loadPlotReading(ref res, tmp);
            

            foreach(var el in root.Elements("mapping"))
            {
                //"mapping": Determin which input is emitted.
                var name = xElemAttr<string>(el, "name", true);
                var packName = xElemAttr<string>(el, "pack", false, () => { return name; });
                res.registerOutput(name, packName);
                mappingCount++;
            }
            if(mappingCount <= 0)
            {
                throw new ConfigurationErrorsException("elem:\"mapping\" must be defined at least 1 time.");
            }
            this.readEmitters[path] = res;

            return res;
            
        }

        private ControllerStateEmitter loadSpyExport(XElement spyExport)
        {
            if(spyExport is null)
            {
                throw new ConfigurationErrorsException("spy-export doesn't exists");
            }
            ControllerStateEmitter ret;
            {
                //part: "local"
                Console.WriteLine("reading spy-export.local");
                var tmp = spyExport.Element("local");
                var port = xElemAttr<int>(tmp, "port", false, () => { return 0; });
                var threshold = xElemAttr<int>(tmp, "threshold", false, () => { return 512; });
                var prot = xElemAttr<string>(tmp, "protocol", false, () => { return "udp"; });
                Console.WriteLine("port: " + port + ", th: " + threshold + ", pr: " + prot);
                if(prot != "udp")
                {
                    throw new ConfigurationErrorsException("Currently, only udp is supported.");
                }
                ret = new ControllerStateEmitter(threshold, port);
            }
            
            foreach (var tmp in spyExport.Elements("dest").ToList())
            {
                //part: "dest"
                var ip = xElemAttr<System.Net.IPAddress>(tmp, "ip", false, 
                    () => { return System.Net.IPAddress.Loopback; }, System.Net.IPAddress.Parse);

                var port = xElemAttr<int>(tmp, "port", false, () => { return 8934; });
                var prot = xElemAttr<string>(tmp, "protocol", false, () => { return "udp"; });
                if(prot != "udp")
                {
                    continue;
                }
                if(port <= 0 || port > 0x0000FFFF)
                {
                    continue;
                }
                try
                {
                    ret.appendDestUdp(new System.Net.IPEndPoint(ip, port));
                }
                catch(Exception e)
                {
                    continue;
                }
            }

            return ret;
        }

        private void loadPlotReading(ref ControllerStateEmitter cs, XElement elem)
        {
            if (elem is null)
            {
                return;
            }
            {
                //part: "local" => only read attrs: "port" "protocol"
                var tmp = elem.Element("local");
                var port = xElemAttr<int>(tmp, "port", false, () => { return 0; });
                var prot = xElemAttr<string>(tmp, "protocol", false, () => { return "udp"; });
                if (prot != "udp")
                {
                    return;
                }
                cs.appendDestUdp(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, port));
            }
        }

        private static T xElemAttr<T>(XElement elem, string name, bool exceptIfWrong, 
            Func<T> ifNull = null, Func<string, T> parser = null)
        {
            var elemName = (elem is null) ? "<null>" : elem.Name;
            var attr = (elem is null) ? null : elem.Attribute(name);
            T ret;
            
            if(parser is null)
            {
                parser = (s) => { return (T)Convert.ChangeType(s, typeof(T)); };
            }

            if(ifNull is null)
            {
                ifNull = () =>
                {
                    throw new ConfigurationErrorsException(elemName + ":" + name + "->required.");
                };
            }

            if(attr is null)
            {
                return ifNull();
            }
            try
            {
                ret = parser(attr.Value);
                
            }
            catch(Exception e)
            {
                if (exceptIfWrong)
                {
                    throw new ConfigurationErrorsException(elemName + ":" + name + "->wrong value[" + attr.Value + "].", e);
                }
                return ifNull();
            }
            return ret;
        }

        public static ControllerStateEmitterBuilder newInstance()
        {
            return new ControllerStateEmitterBuilder();
        }

    }
}
