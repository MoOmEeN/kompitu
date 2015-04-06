using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kompitu
{
    class Kontroler
    {
        MainPage mp;

        private static Kontroler kontroler;

        private Kontroler(MainPage mp)
        {
            this.mp = mp;
        }

        public static Kontroler GetInstance(MainPage mp)
        {
            if (kontroler == null)
            {
                kontroler = new Kontroler(mp);
            }
            return kontroler;
        }

        public 
    }
}
