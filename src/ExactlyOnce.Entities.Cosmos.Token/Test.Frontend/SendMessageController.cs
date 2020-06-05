//using System;
//using System.Threading.Tasks;
//using ExactlyOnce.Entities.ClaimCheck.NServiceBus;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Azure.Cosmos;
//using NServiceBus;

//[ApiController]
//[Route("")]
//public class SendMessageController : Controller
//{
//    readonly IConnector connector;

//    #region ConnectorInjection
//    public SendMessageController(IConnector connector)
//    {
//        this.connector = connector;
//    }
//    #endregion


//    #region MessageSessionUsage
//    [HttpGet]
//    public async Task<string> Get(string partition)
//    {
//        var session = await connector.OpenSession(Guid.NewGuid().ToString(), new PartitionKey(partition));
//        session.Commit()
//    }
//    #endregion
//}
