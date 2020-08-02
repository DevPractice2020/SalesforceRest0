using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SalesforceRest
{
    public partial class Form1 : Form
    {
        public static string oauthToken = "";
        public static string serviceUrl = "";
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            Authenticate();
            PostPO();
        }

        public void Authenticate()
        {
            try
            {
                Task.Run(async () =>
                {
                    await DoAuthentication();
                }).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                var innerException = ex.InnerException;
            }
        }

        private static async Task DoAuthentication()
        {
            int li = 0;
            HttpClient authClient = new HttpClient();
            string sfdcConsumerKey = "3MVG9YDQS5WtC11qpsEUcIwI0oDrzHxC_yppJXYPzHJ1H8WYOkYyIsbrMOEpvOghvQ9PR_M0TBZP2K.53rV4Q";
            string sfdcConsumerSecret = "9014630030068766543";
            string sfdcUserName = "its.samraat@gmail.com";
            string sfdcPassword = "drowssapmca@123";
            string sfdcToken = "mU9Z8m2bd3RdiuO6szoWp0o6";

            string loginPassword = sfdcPassword + sfdcToken;

            HttpContent content = new FormUrlEncodedContent(
                new Dictionary<string, string>{
                                 {"grant_type","password"},
                                 {"client_id",sfdcConsumerKey},
                                 {"client_secret",sfdcConsumerSecret},
                                 {"username",sfdcUserName},
                                 {"password",loginPassword}
                               }
            );

            HttpResponseMessage message = await authClient.PostAsync("https://login.salesforce.com/services/oauth2/token", content);
            string responseString = await message.Content.ReadAsStringAsync();
            JObject obj = JObject.Parse(responseString);
            oauthToken = (string)obj["access_token"];
            serviceUrl = (string)obj["instance_url"];
        }

        public void PostPO()
        {
            try
            {
                Task.Run(async () =>
                {
                    await DoPOPosting();
                }).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                var innerException = ex.InnerException;
            }
        }

        private static async Task DoPOPosting()
        {
            HttpClient createClient = new HttpClient();
                        
            Root root = new Root();
            CompositeRequest compositeReq;
            CompositeRequest InnercompositeReq;

            root.compositeRequest = new List<CompositeRequest>();

            for(int i=1;i<5;i++)
            {
                compositeReq = new CompositeRequest();
                compositeReq.method = "POST";
                compositeReq.url = "/services/data/v43.0/sobjects/Manager__c";
                compositeReq.referenceId = "refManager_" + i.ToString();
                compositeReq.body = new Manager()
                {
                    ManFirstName__c = "ManagerF_" + i.ToString(),
                    ManLastName__c = "ManagerL_" + i.ToString()
                };

                root.compositeRequest.Add(compositeReq);

                for (int j = 1; j < 2; j++)
                {
                    InnercompositeReq = new CompositeRequest();
                    InnercompositeReq.method = "POST";
                    InnercompositeReq.url = "/services/data/v43.0/sobjects/Employee__c";
                    InnercompositeReq.referenceId = "refEmployee_" + i.ToString() + j.ToString();
                    InnercompositeReq.body = new Employee()
                    {                       
                        EmpAge__c = (j*10).ToString(),
                        EmpFirstName__c = "EmployeeF_" + i.ToString() + j.ToString(),
                        EmpLastName__c = "EmployeeL_" + i.ToString() + j.ToString(),
                        ManagerID__c = "@{" + compositeReq.referenceId + ".id}"
                    };

                    root.compositeRequest.Add(InnercompositeReq);
                }
            }

            string requestMessage = JsonConvert.SerializeObject(root);
            
            HttpContent content = new StringContent(requestMessage, Encoding.UTF8, "application/json");

            //string requestMessage = "<root><name>DevForce21</name><accountnumber>8994432</accountnumber></root>";
            //HttpContent content = new StringContent(requestMessage, Encoding.UTF8, "application/xml");

            string uri = serviceUrl + "/services/data/v43.0/composite";

            //create request message associated with POST verb
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uri);

            //add token to header
            request.Headers.Add("Authorization", "Bearer " + oauthToken);

            //return xml to the caller
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = content;

            HttpResponseMessage response = await createClient.SendAsync(request);

            string result = await response.Content.ReadAsStringAsync();

        }
    }


    public class Manager
    {
        public string ManFirstName__c { get; set; }
        public string ManLastName__c { get; set; }       
    }

    public class Employee
    {
        public string EmpAge__c { get; set; }
        public string EmpFirstName__c { get; set; }
        public string EmpLastName__c { get; set; }
        public string ManagerID__c { get; set; }
    }

    public class CompositeRequest
    {
        public string method { get; set; }
        public string url { get; set; }
        public string referenceId { get; set; }
        public Object body { get; set; }
    }

    public class Root
    {
        public List<CompositeRequest> compositeRequest { get; set; }
    }
}
