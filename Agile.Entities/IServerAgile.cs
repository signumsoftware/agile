using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Services;
using Signum.Entities.Disconnected;
using Signum.Utilities;
using Signum.Entities.SMS;
using Signum.Entities.Services;

namespace Agile.Services
{
    //Defines the WPF contract between client and server applications
    [ServiceContract(SessionMode = SessionMode.Required)]
    [ExceptionMarshallingBehavior]
    public interface IServerAgile : IBaseServer, IDynamicQueryServer, IOperationServer,
        ILoginServer, IProcessServer, IQueryServer, IChartServer, IExcelReportServer, IUserQueryServer, IDashboardServer, IUserAssetsServer,
        IProfilerServer, IQueryAuthServer, IPropertyAuthServer, ITypeAuthServer, IPermissionAuthServer, IOperationAuthServer,
        IDisconnectedServer, ISmsServer, IHelpServer
    {

    }


    [ServiceContract(SessionMode = SessionMode.NotAllowed)]
    public interface IServerAgileTransfer : IDisconnectedTransferServer
    {

    }
}
