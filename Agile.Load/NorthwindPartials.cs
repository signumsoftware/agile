using Signum.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Agile.Load
{
    public partial class NorthwindDataContext
    {
        public NorthwindDataContext() :
            base(UserConnections.Replace(global::Agile.Load.Properties.Settings.Default.NorthwindConnectionString), mappingSource)
        {
            OnCreated();
        }
    }
}
