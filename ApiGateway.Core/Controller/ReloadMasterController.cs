using ApiGateway.Core.Service;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Core.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReloadMasterController : ControllerBase
    {
        private readonly MasterConnection _masterConnection;

        public ReloadMasterController(MasterConnection masterConnection)
        {
            _masterConnection = masterConnection;
        }

        [HttpGet("ReloadMaster")]
        public IActionResult ReloadMaster()
        {
            var result = _masterConnection.LoadMasterConnection();
            return Ok(result);
        }
    }
}
