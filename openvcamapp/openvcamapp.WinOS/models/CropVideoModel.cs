using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace openvcamapp.winos.models
{
    public class CropVideoModel
    {
        private double? m_start;
        private double? m_end;

        public CropVideoModel() : this(null, null) { }
        public CropVideoModel(double? Start, double? End)
        {
            m_start = Start;
            m_end = End;
        }

        public double? Start { get { return (m_start); } set { m_start = value; } }
        public double? End { get { return (m_end); } set { m_end = value; } }
    }
}
