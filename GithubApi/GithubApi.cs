using NLog;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;

namespace AppDynamics.Dexter
{
    public class GithubApi : IDisposable
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        #region Private variables

        private HttpClient _httpClient;
        private HttpClientHandler _httpClientHandler;
        private CookieContainer _cookieContainer;

        #endregion

        #region Public properties

        public string GithubUrl { get; set; }

        public int Timeout
        {
            get
            {
                return this._httpClient.Timeout.Minutes;
            }
            set
            {
                this._httpClient.Timeout = new TimeSpan(0, value, 0);
            }
        }

        #endregion

        #region Constructor, Destructor and overrides

        public GithubApi(string githubUrl)
        {
            this.GithubUrl = githubUrl;

            this._cookieContainer = new CookieContainer();
            this._httpClientHandler = new HttpClientHandler();
            this._httpClientHandler.UseCookies = true;
            this._httpClientHandler.CookieContainer = this._cookieContainer;

            HttpClient httpClient = new HttpClient(this._httpClientHandler);
            // Default to 1 minute timeout. Can be adjusted as needed
            httpClient.Timeout = new TimeSpan(0, 1, 0);
            httpClient.BaseAddress = new Uri(this.GithubUrl);
            httpClient.DefaultRequestHeaders.Add("User-Agent", String.Format("AppDynamics DEXTER {0}", Assembly.GetEntryAssembly().GetName().Version));

            this._httpClient = httpClient;
        }

        public override String ToString()
        {
            return String.Format(
                "GithubApi: ApiURL='{0}'",
                this.GithubUrl);
        }

        public void Dispose()
        {
            this._httpClientHandler.Dispose();
            this._httpClient.Dispose();
        }

        #endregion

        #region Check for releases

        public string GetReleases()
        {
            return this.apiGET("repos/Appdynamics/AppDynamics.DEXTER/releases", "application/json");
        }

        #endregion

        #region Data retrieval 

        /// <summary>
        /// Invokes Controller API using GET method
        /// </summary>
        /// <param name="restAPIUrl">REST URL to retrieve with GET</param>
        /// <param name="acceptHeader">Desired Content Type of response</param>
        /// <returns>Raw results if successful, empty string otherwise</returns>
        private string apiGET(string restAPIUrl, string acceptHeader)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            {
                MediaTypeWithQualityHeaderValue accept = new MediaTypeWithQualityHeaderValue(acceptHeader);
                if (this._httpClient.DefaultRequestHeaders.Accept.Contains(accept) == false)
                {
                    this._httpClient.DefaultRequestHeaders.Accept.Add(accept);
                }

                HttpResponseMessage response = this._httpClient.GetAsync(restAPIUrl).Result;
                if (response.IsSuccessStatusCode)
                {
                    return response.Content.ReadAsStringAsync().Result;
                }
                else
                {
                    // For the times when the system throws 500 with some meaningful message
                    string resultString = response.Content.ReadAsStringAsync().Result;
                    if (resultString.Length > 0)
                    {
                        logger.Error("{0}/{1} GET as {2} returned {3} ({4}) with {5}", this.GithubUrl, restAPIUrl, "Anonymous", (int)response.StatusCode, response.ReasonPhrase, resultString);
                    }
                    else
                    {
                        logger.Error("{0}/{1} GET as {2} returned {3} ({4})", this.GithubUrl, restAPIUrl, "Anonymous", (int)response.StatusCode, response.ReasonPhrase);
                    }
                    return String.Empty;
                }
            }
            catch (Exception ex)
            {
                logger.Error("{0}/{1} GET as {2} threw {3} ({4})", this.GithubUrl, restAPIUrl, "Anonymous", ex.Message, ex.Source);
                logger.Error(ex);

                return String.Empty;
            }
            finally
            {
                stopWatch.Stop();
                logger.Trace("{0}/{1} GET as {2} took {3:c} ({4} ms)", this.GithubUrl, restAPIUrl, "Anonymous", stopWatch.Elapsed.ToString("c"), stopWatch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Invokes Controller API using POST method
        /// </summary>
        /// <param name="restAPIUrl">REST URL to retrieve with POST</param>
        /// <param name="acceptHeader">Desired Content Type of response</param>
        /// <param name="requestBody">Body of the message</param>
        /// <returns>Raw results if successful, empty string otherwise</returns>
        private string apiPOST(string restAPIUrl, string acceptHeader, string requestBody, string requestTypeHeader)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            {
                MediaTypeWithQualityHeaderValue accept = new MediaTypeWithQualityHeaderValue(acceptHeader);
                if (this._httpClient.DefaultRequestHeaders.Accept.Contains(accept) == false)
                {
                    this._httpClient.DefaultRequestHeaders.Accept.Add(accept);
                }
                StringContent content = new StringContent(requestBody);
                content.Headers.ContentType = new MediaTypeWithQualityHeaderValue(requestTypeHeader);

                HttpResponseMessage response = this._httpClient.PostAsync(restAPIUrl, content).Result;
                if (response.IsSuccessStatusCode)
                {
                    return response.Content.ReadAsStringAsync().Result;
                }
                else
                {
                    string resultString = response.Content.ReadAsStringAsync().Result;
                    if (resultString.Length > 0)
                    {
                        logger.Error("{0}/{1} POST as {2} returned {3} ({4}) with {5}", this.GithubUrl, restAPIUrl, "Anonymous", (int)response.StatusCode, response.ReasonPhrase, resultString);
                    }
                    else
                    {
                        logger.Error("{0}/{1} POST as {2} returned {3} ({4})", this.GithubUrl, restAPIUrl, "Anonymous", (int)response.StatusCode, response.ReasonPhrase);
                    }

                    return String.Empty;
                }
            }
            catch (Exception ex)
            {
                logger.Error("{0}/{1} POST as {2} threw {3} ({4})", this.GithubUrl, restAPIUrl, "Anonymous", ex.Message, ex.Source);
                logger.Error(ex);

                return String.Empty;
            }
            finally
            {
                stopWatch.Stop();
                logger.Trace("{0}/{1} POST as {2} took {3:c} ({4} ms)", this.GithubUrl, restAPIUrl, "Anonymous", stopWatch.Elapsed.ToString("c"), stopWatch.ElapsedMilliseconds);
                logger.Trace("POST body {0}", requestBody);
            }
        }

        #endregion
    }
}
