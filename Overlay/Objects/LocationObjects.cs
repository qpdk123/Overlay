using Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Overlay.Objects
{
    public class TrasureBox
    {
        public int X { get; set; }
        public int Y { get; set; }

        public TrasureBox() { }
        public TrasureBox(int _x, int _y) { X = _x; Y = _y; }
    }
    public class BeachBoxes
    {
        private List<TrasureBox> boxes = new List<TrasureBox>();
        public List<TrasureBox> BOXES { get => this.boxes; set => this.boxes = value; }
    }

    public class AbyssBoxes
    {
        private List<TrasureBox> boxes = new List<TrasureBox>();
        public List<TrasureBox> BOXES { get => this.boxes; set => this.boxes = value; }
    }

    public class TrasureMaps
    {
        private List<TrasureBox> boxes = new List<TrasureBox>();
        public List<TrasureBox> MAPS { get => this.boxes; set { this.boxes = value; } }
    }

}
