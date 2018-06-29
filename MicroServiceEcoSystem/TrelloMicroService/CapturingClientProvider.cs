using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrelloMicroService
{
    using Manatee.Trello.Rest;

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   A capturing client provider. This class cannot be inherited. </summary>
    ///
    /// <seealso cref="T:Manatee.Trello.Rest.IRestClientProvider"/>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    public sealed class CapturingClientProvider : IRestClientProvider
    {
        /// <summary>   The REST client provider implementation. </summary>
        private readonly IRestClientProvider _restClientProviderImplementation;

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
        /// Initializes a new instance of the TrelloMicroService.CapturingClientProvider class.
        /// </summary>
        ///
        /// <param name="restClientProviderImplementation"> The REST client provider implementation. </param>
        /// <param name="captureRequest">                   The capture request. </param>
        /// <param name="captureResponse">                  The capture response. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public CapturingClientProvider(IRestClientProvider restClientProviderImplementation,
                                       Action<IRestRequest> captureRequest,
                                       Action<IRestResponse> captureResponse)
        {
            _restClientProviderImplementation = restClientProviderImplementation;
            CaptureRequest = captureRequest;
            CaptureResponse = captureResponse;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Creates requests for the client. </summary>
        ///
        /// <value> The request provider. </value>
        ///
        /// <seealso cref="P:Manatee.Trello.Rest.IRestClientProvider.RequestProvider"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public IRestRequestProvider RequestProvider => _restClientProviderImplementation.RequestProvider;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Creates an instance of IRestClient. </summary>
        ///
        /// <param name="apiBaseUrl">   The base URL to be used by the client. </param>
        ///
        /// <returns>   An instance of IRestClient. </returns>
        ///
        /// <seealso cref="M:Manatee.Trello.Rest.IRestClientProvider.CreateRestClient(string)"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public IRestClient CreateRestClient(string apiBaseUrl)
        {
            var client = _restClientProviderImplementation.CreateRestClient(apiBaseUrl);

            return new CapturingClient(client, CaptureRequest, CaptureResponse);
        }
    }
}
