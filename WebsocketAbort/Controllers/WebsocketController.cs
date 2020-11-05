using System;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace WebsocketAbort.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WebsocketController : ControllerBase
    {

        private readonly ILogger<WebsocketController> _logger;

        public WebsocketController(
            ILogger<WebsocketController> logger
            )
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> TestConnectionAsync()
        {
            using var canceller = new CancellationTokenSource();
            using var client = await HttpContext.WebSockets.AcceptWebSocketAsync();
            _ = Task.Run(() => {
                Thread.Sleep(TimeSpan.FromMinutes(1));
                client.Abort();
                _logger.LogWarning("aborted");
            });

            var buffer = new Memory<byte>(new byte[100 * 1024]);
            try
            {
                while (new[] { WebSocketState.Open, WebSocketState.CloseSent }.Contains(client.State))
                {
                    var res = await client.ReceiveAsync(buffer, canceller.Token);
                    if (res.MessageType == WebSocketMessageType.Close)
                        _logger.LogInformation("received Close from the client");
                    _logger.LogWarning("received data");
                }
            }
            finally
            {
                _logger.LogWarning("ended loop");
            }
            return new EmptyResult();
        }
    }
}
