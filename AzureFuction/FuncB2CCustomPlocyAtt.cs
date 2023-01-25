using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Graph;
using B2CCustomPolicy.Models;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace B2CCustomPolicy.AzureFuction
{
    public class FuncB2CCustomPlocyAtt
    {
        private readonly GraphServiceClient _graphServiceClient;

        public FuncB2CCustomPlocyAtt(GraphServiceClient graphServiceClient)
        {
            _graphServiceClient = graphServiceClient;
        }


        [FunctionName("FuncB2CCustomPlocyAtt")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string responseMessage = ""; //default
            //string extensionproperty = "";
            UserModel userModel = null;
            // Check HTTP basic authorization
            if (!Authorize(req, log))
            {
                responseMessage = "HTTP basic authentication validation failed.";
                log.LogError("HTTP basic authentication validation failed.");
                return GetB2cApiConnectorResponse("ValidationError", responseMessage, 400, string.Empty);
                //return (ActionResult)new UnauthorizedResult();
            }
            else
            {
                log.LogInformation(" autehntication passed.");
            }


            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            log.LogInformation("Request body: " + requestBody);
            dynamic dataDynamic = JsonConvert.DeserializeObject(requestBody);//we need that
            JObject data = JObject.Parse(requestBody);

            JProperty ObjectIdProperty = data.Properties().FirstOrDefault(p => p.Name.Equals("objectId"));

            if (data == null)
            {
                responseMessage = "There was a problem with your request. body is empty";
                log.LogError(responseMessage);
                return GetB2cApiConnectorResponse("ValidationError", responseMessage, 400, string.Empty);
                //return new OkObjectResult(responseMessage);
            }

            // If email claim not found, show block page. Email is required and sent by default.
            if (ObjectIdProperty == null)
            {
                responseMessage = "objectId is mandatory in claims.";
                log.LogError(responseMessage);
                return GetB2cApiConnectorResponse("ValidationError", responseMessage, 400, string.Empty);
                //return new OkObjectResult(responseMessage);
            }
            else
            {
                // Get domain of email address
                string objectId = Convert.ToString(((Newtonsoft.Json.Linq.JValue)ObjectIdProperty.Value).Value);
                //string objectId = data.objectId.ToString();
                log.LogInformation("objectId: " + objectId);
                _graphServiceClient.BaseUrl = "https://graph.microsoft.com/beta";
                var user = await _graphServiceClient.Users[objectId].Request().GetAsync(); //beta

                if (user != null && user.Identities != null)
                {
                    log.LogInformation("got user");
                    foreach (var identity in user.Identities)
                    {

                        if (identity.SignInType.ToLower().Equals("phonenumber"))
                        {
                            log.LogInformation("phonenumber inside");
                            userModel = new UserModel();
                            userModel.oid = user.Id;
                            userModel.mobileAttribute = identity.IssuerAssignedId;
                            break;//quit
                        }
                    }
                    if ((userModel != null) && (!string.IsNullOrWhiteSpace(userModel.mobileAttribute)))
                    {
                        //update this user exension attribute 
                        await UpdateUserAsync(userModel, log);
                        //update the json 
                        data["extension_1db2d9f4-bf7a-4093-ad68-fa84bc67798c_MobileAtt"] = userModel.mobileAttribute;
                        requestBody = data.ToString(Newtonsoft.Json.Formatting.None);
                        log.LogInformation(",modified " + requestBody);
                    }
                    else
                    {
                        log.LogInformation("normal email  registration inside");
                    }

                }
            }


         
            if (userModel != null && (!string.IsNullOrWhiteSpace(userModel.mobileAttribute)))
                return GetB2cApiConnectorResponse("Continue", "all success", 200, userModel.mobileAttribute);
            else
                return GetB2cApiConnectorResponse("Continue", "all success", 200, string.Empty); //anyhow return success
            //return new OkObjectResult(GetB2cApiConnectorResponse);//in success case always
        }


        private static IActionResult GetB2cApiConnectorResponse(string action, string userMessage, int statusCode, string extensionproperty)
        {
            var responseProperties = new Dictionary<string, object>
            {
                { "version", "1.0.0" },
                { "action", action },
                { "userMessage", userMessage },
                { "extension_1db2d9f4-bf7a-4093-ad68-fa84bc67798c_MobileAtt", extensionproperty }, // Note: returning just 
            };
            if (statusCode != 200)
            {
                // Include the status in the body as well, but only for validation errors.
                responseProperties["status"] = statusCode.ToString();
            }
            return new JsonResult(responseProperties) { StatusCode = statusCode };
        }

        public static object GetPropValue(object src, string propName)
        {
            return src.GetType().GetProperty(propName).GetValue(src, null);
        }
        bool doesPropertyExist(dynamic obj, string property)
        {
            return ((Type)obj.GetType()).GetProperties().Where(p => p.Name.EndsWith(property)).Any();
        }

        string GetActualPropertyName(dynamic obj, string property)
        {
            string extensionPropoerty = "";
            var prop = ((Type)obj.GetType()).GetProperties().FirstOrDefault(p => p.Name.EndsWith(property));
            if (prop != null)
                extensionPropoerty = prop.Name;

            return extensionPropoerty;
        }

        public async Task UpdateUserAsync(UserModel userModel, ILogger log)
        {
            StringBuilder error = new StringBuilder();
            try
            {
                var userPatch = new Microsoft.Graph.User();
                userPatch.MobilePhone = userModel.mobileAttribute;
                userPatch.AdditionalData = new Dictionary<string, object>();
                userPatch.AdditionalData["extension_1db2d9f4-bf7a-4093-ad68-fa84bc67798c_MobileAtt"] = userModel.mobileAttribute;
                await this._graphServiceClient.Users[userModel.oid].Request().UpdateAsync(userPatch);
                log.LogInformation("user saved scuccessfully: ");
            }
            catch (Exception ex)
            {
                error.AppendLine("UpdateUserAsync");
                error.AppendLine("UpdateUserAsync: message " + ex.Message);
                if (ex.InnerException != null)
                    error.AppendLine("UpdateUserAsync: message " + ex.InnerException.Message);
            }


        }

        private static bool Authorize(HttpRequest req, ILogger log)
        {
            // Get the environment's credentials 
            string username = System.Environment.GetEnvironmentVariable("BASIC_AUTH_USERNAME", EnvironmentVariableTarget.Process);
            string password = System.Environment.GetEnvironmentVariable("BASIC_AUTH_PASSWORD", EnvironmentVariableTarget.Process);

            // Returns authorized if the username is empty or not exists.
            if (string.IsNullOrEmpty(username))
            {
                log.LogError("HTTP basic authentication is not set.");
                return true;
            }

            // Check if the HTTP Authorization header exist
            if (!req.Headers.ContainsKey("Authorization"))
            {
                log.LogError("Missing HTTP basic authentication header.");
                return false;
            }

            // Read the authorization header
            var auth = req.Headers["Authorization"].ToString();

            // Ensure the type of the authorization header id `Basic`
            if (!auth.StartsWith("Basic "))
            {
                log.LogError("HTTP basic authentication header must start with 'Basic '.");
                return false;
            }

            // Get the the HTTP basinc authorization credentials
            var cred = System.Text.UTF8Encoding.UTF8.GetString(Convert.FromBase64String(auth.Substring(6))).Split(':');

            // Evaluate the credentials and return the result
            return (cred[0] == username && cred[1] == password);
        }


    }
}

