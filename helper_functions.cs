using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pachyderm_Acoustic
{
    class Helper_Functions
    {
        public static string ConvertToCSVString(params object[] list)
        {
            string results = null;
            for (int i = 0; i < list.Length; i++)
                results += list[i] + ",";
            return results;
        }
    }
}
