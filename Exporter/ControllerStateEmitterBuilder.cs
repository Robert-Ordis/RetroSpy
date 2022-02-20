using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

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

        public bool readXml(string path, out string errmsg)
        {
            errmsg = "";
            try
            {
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
                var doc = XDocument.Load(path);
                var root = doc.Root;
                if (root.Name != "plotter")
                {
                    throw new Exception("Root elem must be named as \"plotter\".");
                }
                


            }
            catch(Exception e) {
                errmsg = e.Message;
            }
            
            errmsg = "NOT IMPLEMENTED";
            return false;
        }

        public static ControllerStateEmitterBuilder newInstance()
        {
            return new ControllerStateEmitterBuilder();
        }

    }
}
