using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartBuildingController
{
    internal interface IBuildingController
    {
        string GetCurrentState();
        bool SetCurrentState(string state);
        string GetBuildingID();
        void SetBuildingID(string id);
        string GetStatusReport();
    }
}
