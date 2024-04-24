using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Overlay.Objects
{
    internal class LocationData : Singleton<LocationData>
    {
        public class LOC
        {
            public Point pos;
            public bool isSelected;
            public LOC() { this.pos = new Point(0, 0); this.isSelected = false; }
            public LOC(Point pos, bool isSelected)
            {
                this.pos = pos;
                this.isSelected = isSelected;
            }
            public LOC(int x, int y, bool isSelected = false)
            {
                this.pos = new Point(x, y);
                this.isSelected = isSelected;
            }
        }

        public LOC charPos = new LOC();

        private List<LOC> items = new List<LOC>();

        public LOC this[int index]
        {
            get 
            {
                if (index < 0 || index > this.items.Count)
                    throw new IndexOutOfRangeException("Index is out of range");
                return this.items[index];
            }
        }

        public int Length
        {
            get { return this.items.Count; }
        }


        private LocationData() { }

        public void Add(LOC loc)
        {
            if (this.IsExist(loc) == false)
                this.items.Add(loc);
        }

        public void Add(int x, int y, bool isSelected = false)
        {
            if (this.IsExist(x, y) == false)
                this.items.Add(new LOC(x, y, isSelected));
        }

        public bool IsExist(LOC loc)
        {
            foreach (LOC item in this.items)
            {
                if (item.pos.X == loc.pos.X && item.pos.Y == loc.pos.Y)
                    return true;
            }

            return false;
        }

        public bool IsExist(int x, int y)
        {
            foreach (LOC item in this.items)
            {
                if (item.pos.X == x && item.pos.Y == y)
                    return true;
            }

            return false;
        }

        public LOC Contains(int x, int y)
        {
            foreach (LOC item in this.items)
            {
                if (item.pos.X == x && item.pos.Y == y)
                    return item;
            }

            return default(LOC);
        }

        public void Remove(int x, int y)
        {
            LOC remove = this.Contains(x, y);

            if (remove != default(LOC))
            {
                this.items.Remove(remove);
            }
        }

        public LOC GetSelectedItem()
        {
            foreach (LOC item in this.items)
            {
                if (item.isSelected == true) return item;
            }
            return default(LOC);
        }

        public void SetSelect(int x, int y)
        {
            LOC select = this.Contains(x, y);
            if (select != default(LOC))
            {
                this.DeSelectAll();
                select.isSelected = true;
            }
        }

        public void DeSelectAll()
        {
            foreach (LOC item in this.items)
            {
                item.isSelected = false;
            }
        }

        public void ClearAll()
        {
            this.items.Clear();
        }

        public LOC[] GetAllData()
        {
            return this.items.ToArray();
        }

        public LOC GetSelectedData()
        {
            LOC loc = FindNearDot(this.charPos.pos.X, this.charPos.pos.Y);
            if (loc != default(LOC))
            {
                if(loc.isSelected == true) 
                    return loc;
            }

            return default(LOC);
        }

        public LOC FindNearDot(int x, int y)
        {
            double min = 200;
            LOC ret = default(LOC);

            foreach (LOC item in this.items)
            {
                double distance = Math.Sqrt(Math.Pow(x - item.pos.X, 2) + Math.Pow(y - item.pos.Y, 2));

                if (distance < min)
                {
                    min = distance;
                    ret = item;
                }
            }

            return ret;
        }
    }
}
