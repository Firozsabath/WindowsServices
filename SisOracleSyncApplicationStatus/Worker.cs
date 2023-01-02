using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SisOracleSyncApplicationStatus
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _config;

        public Worker(ILogger<Worker> logger, IConfiguration Config)
        {
            _logger = logger;
            _config = Config;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Worker started by FirozOICUpdated at: {time}", DateTimeOffset.Now);
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Worker Stopped by FirozOICUpdated at: {time}", DateTimeOffset.Now);
            return base.StopAsync(cancellationToken);
        }

        public int StartDelayTime()
        {
            int Delay1 = Convert.ToInt32(_config["AppDataConfig:DelayTimeInMilliseconds"]);
            int Delay2 = Convert.ToInt32(_config["AppDataConfig:DelayTimeInHours"]);
            int DelayTime = Delay1 * Delay2;
            return DelayTime;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
         {
            int DelayTime = StartDelayTime();
            while (!stoppingToken.IsCancellationRequested)
            {
                if (GetResponse().Result)
                {
                    _logger.LogInformation("Worker-OICUpdated running Response is ok: {time}", DateTimeOffset.Now);
                    await Task.Delay(DelayTime, stoppingToken);
                }
                else
                {
                    _logger.LogInformation("Worker-OICUpdated running at No Response: {time}", DateTimeOffset.Now);
                    await Task.Delay(DelayTime, stoppingToken);
                }
            }
        }

    

        public async Task<bool> GetResponse()
        {
            string connectionstringex = _config.GetConnectionString("DefaultConnection");
            string token = _config["AppDataConfig:ActiveCampaignToken"];
            string responseCode = string.Empty;
            DataSet ds = new DataSet();
            //bool EmailExists = false;
            try
            {
                using (SqlConnection con = new SqlConnection(connectionstringex))
                {
                    con.Open();
                    string SPName = "ActiveCampaignDataset";
                    SqlCommand cmd = new SqlCommand(SPName, con);
                    cmd.Parameters.Add(new SqlParameter("@Type", SqlDbType.VarChar)).Value = "ApplicantStatus";
                    cmd.CommandType = CommandType.StoredProcedure;
                    SqlDataAdapter sqldata = new SqlDataAdapter(cmd);
                    sqldata.Fill(ds);

                    if (ds.Tables[0].Rows.Count > 0)
                    {
                        for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                        {
                            string email = string.Empty;                           
                            email = ds.Tables[0].Rows[i]["Email1"].ToString();
                            string Status = ds.Tables[0].Rows[i]["DisplayText"].ToString();
                            string proid = ds.Tables[0].Rows[i]["ProspectID"].ToString();
                            DateTime? CamsLastUpdate = String.IsNullOrEmpty(ds.Tables[0].Rows[i]["UpdateTime"].ToString()) ? null : Convert.ToDateTime(ds.Tables[0].Rows[i]["UpdateTime"].ToString());
                            string Term = ds.Tables[0].Rows[i]["OracleTerm"].ToString();

                            //if (!IsWriitern(proid, CamsLastUpdate, "Application_status"))
                            //{

                                EloquaContactview contactviewel = new EloquaContactview
                                {
                                    contact = new EloquaInteg
                                    {
                                        Email = email,
                                        Application_Status = Status,
                                        entrance_term = Term,
                                        ProspectID_c = proid
                                    }
                                };
                                string EloquaDataset = JsonConvert.SerializeObject(contactviewel).ToString();
                                Verboselog(proid, email, "updating", "", "Application_status");

                                using (HttpClient request = new HttpClient())
                                {
                                    request.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes("rajasekhar.mandala@aspiresys.com:Password99669")));
                                    request.DefaultRequestHeaders.ConnectionClose = true;
                                    request.Timeout = TimeSpan.FromSeconds(1000);
                                    StringContent content = new StringContent(EloquaDataset, Encoding.UTF8, "application/json");

                                // using (var response = await request.PostAsync("https://cudint-fryxusjfwk20-fr.integration.ocp.oraclecloud.com:443/ic/api/integration/v1/flows/rest/SYNCUPDATEFROMSISTOELOQUA/1.0/contact", content))
                                using (var response = await request.PostAsync("https://cudint-fryxusjfwk20-fr.integration.ocp.oraclecloud.com:443/ic/api/integration/v1/flows/rest/SYNCUPDATEFROMSISTOCRM/1.0/contact", content))                               
                                {
                                        string apiResponse = await response.Content.ReadAsStringAsync();

                                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                                        {
                                            string respstatus = String.IsNullOrEmpty(apiResponse) ? "success" : apiResponse;
                                            Verboselog(proid, email, "completed", respstatus, "Application_status");
                                            Verboselogreference(proid, email, CamsLastUpdate, "Application_status");
                                        }
                                    }
                                }
                            //}
                        }
                    }
                    con.Close();
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Worker running at No Response: {time}", DateTimeOffset.Now + "-" + ex.Message + "-" + ex.InnerException);
                throw;
            }
            return true;
        }

        public bool Verboselog(string ID, string Email, string status, string Responsemsg, string updatefield)
        {
            bool rtn = true;
            string connectionstringexternal = _config.GetConnectionString("DefaultConnection_ex");
            DataSet ds = new DataSet();
            try
            {
                using (SqlConnection con = new SqlConnection(connectionstringexternal))
                {
                    con.Open();
                    string spName = "SisOracleVerboseLog";
                    SqlCommand cmd = new SqlCommand(spName, con);
                    cmd.Parameters.Add(new SqlParameter("@studentid", SqlDbType.VarChar)).Value = ID;
                    cmd.Parameters.Add(new SqlParameter("@Email", SqlDbType.VarChar)).Value = Email;
                    cmd.Parameters.Add(new SqlParameter("@status", SqlDbType.VarChar)).Value = status;
                    cmd.Parameters.Add(new SqlParameter("@updatedfield", SqlDbType.VarChar)).Value = updatefield;
                    cmd.Parameters.Add(new SqlParameter("@Responsemsg", SqlDbType.VarChar)).Value = Responsemsg;
                    cmd.CommandType = CommandType.StoredProcedure;
                    SqlDataAdapter sqldata = new SqlDataAdapter(cmd);
                    sqldata.Fill(ds);
                    con.Close();
                }
            }
            catch (Exception ex)
            {
                rtn = false;
                throw;
            }
            return rtn;
        }

        public bool Verboselogreference(string ID, string Email, DateTime? updatedtime, string updatefield)
        {
            bool rtn = true;
            string connectionstringexternal = _config.GetConnectionString("DefaultConnection_ex");
            DataSet ds = new DataSet();
            try
            {
                using (SqlConnection con = new SqlConnection(connectionstringexternal))
                {
                    con.Open();
                    string spName = "ProcSisOracleVerboseLogReference";
                    SqlCommand cmd = new SqlCommand(spName, con);
                    cmd.Parameters.Add(new SqlParameter("@prospectid", SqlDbType.VarChar)).Value = ID;
                    cmd.Parameters.Add(new SqlParameter("@Email", SqlDbType.VarChar)).Value = Email;
                    cmd.Parameters.Add(new SqlParameter("@CamsUpdatedTime", SqlDbType.DateTime)).Value = updatedtime;
                    cmd.Parameters.Add(new SqlParameter("@updatedfield", SqlDbType.VarChar)).Value = updatefield;                    
                    cmd.CommandType = CommandType.StoredProcedure;
                    SqlDataAdapter sqldata = new SqlDataAdapter(cmd);
                    sqldata.Fill(ds);
                    con.Close();
                }
            }
            catch (Exception ex)
            {
                rtn = false;
                throw;
            }
            return rtn;
        }

        public bool IsWriitern(string proid, DateTime? CamsLastUpdated, string UpdatedField)
        {
            bool wrtn = false;
            string connectionstringexternal = _config.GetConnectionString("DefaultConnection_ex");
            DataSet ds = new DataSet();
            try
            {
                using (SqlConnection con = new SqlConnection(connectionstringexternal))
                {
                    con.Open();
                    string spName = "select * from tbl_sis_oracle_sync_log_reference where Cams_Lastupdatedtime = '"+CamsLastUpdated + "' and UpdatedField = '" + UpdatedField + "' and ProspectID = '" + proid + "'";                    

                    SqlCommand cmd = new SqlCommand(spName, con);
                    cmd.CommandType = CommandType.Text;
                    SqlDataAdapter sqldata = new SqlDataAdapter(cmd);
                    sqldata.Fill(ds);
                    if (ds.Tables[0].Rows.Count > 0)
                    {
                        wrtn = true;
                    }
                    else
                    {
                        wrtn = false;
                    }
                    con.Close();
                }
            }
            catch (Exception ex)
            {
                wrtn = false;
                throw;
            }
            return wrtn;
        }

        public string HttpPost(string URI, string Parameters, string type)
        {
            System.Net.WebRequest req = System.Net.WebRequest.Create(URI);
            req.ContentType = "application/json; charset=utf-8";
            req.Method = "POST";
            req.Timeout = 600000;            
            req.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes("rajasekhar.mandala@aspiresys.com:Password99669"));
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(Parameters);
            req.ContentLength = bytes.Length;
            System.IO.Stream os = req.GetRequestStream();
            os.Write(bytes, 0, bytes.Length);
            os.Close();
            System.Net.WebResponse resp = req.GetResponse();
            if (resp == null)
                return null;
            System.IO.StreamReader sr = new System.IO.StreamReader(resp.GetResponseStream());
            return sr.ReadToEnd().Trim();
        }
    }
}
