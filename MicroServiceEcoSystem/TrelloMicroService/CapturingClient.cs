using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrelloMicroService
{
    using System.Threading;
    using Manatee.Trello.Rest;

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   A capturing client. </summary>
    ///
    /// <seealso cref="T:Manatee.Trello.Rest.IRestClient"/>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    public class CapturingClient : IRestClient
    {
        /// <summary>   The client. </summary>
        private readonly IRestClient _client;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the capture request. </summary>
        ///
        /// <value> The capture request. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Action<IRestRequest> CaptureRequest { get; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the capture response. </summary>
        ///
        /// <value> The capture response. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Action<IRestResponse> CaptureResponse { get; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Initializes a new instance of the TrelloMicroService.CapturingClient class.
        /// </summary>
        ///
        /// <param name="client">           The client. </param>
        /// <param name="captureRequest">   The capture request. </param>
        /// <param name="captureResponse">  The capture response. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public CapturingClient(IRestClient client,
                               Action<IRestRequest> captureRequest,
                               Action<IRestResponse> captureResponse)
        {
            _client = client;
            CaptureRequest = captureRequest;
            CaptureResponse = captureResponse;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Makes a RESTful call and ignores any return data. </summary>
        ///
        /// <param name="request">  The request. </param>
        /// <param name="ct">       A cancellation token for async processing. </param>
        ///
        /// <returns>   The asynchronous result that yields an IRestResponse. </returns>
        ///
        /// <seealso cref="M:Manatee.Trello.Rest.IRestClient.Execute(IRestRequest,CancellationToken)"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public async Task<IRestResponse> Execute(IRestRequest request, CancellationToken ct)
        {
            CaptureRequest(request);
            var response = await _client.Execute(request, ct);
            CaptureResponse(response);

            return response;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Makes a RESTful call and expects a single object to be returned. </summary>
        ///
        /// <typeparam name="T">    The expected type of object to receive in response. </typeparam>
        /// <param name="request">  The request. </param>
        /// <param name="ct">       A cancellation token for async processing. </param>
        ///
        /// <returns>   The response. </returns>
        ///
        /// <seealso cref="M:Manatee.Trello.Rest.IRestClient.Execute{T}(IRestRequest,CancellationToken)"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public async Task<IRestResponse<T>> Execute<T>(IRestRequest request, CancellationToken ct) where T : class
        {
            CaptureRequest(request);
            var response = await _client.Execute<T>(request, ct);
            CaptureResponse(response);

            return response;
        }
    }
}
