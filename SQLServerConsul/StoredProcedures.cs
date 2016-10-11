using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Server;
using Consul;

namespace SQLServerConsul
{
    public class StoredProcedures
    {
        public static string UpdateConsulServices(string activeDatabases)
        {
            // Consul Environment --> Input from SQL Server
            // List of Active Databases --> Input from SQL Server
            // List of Current consul services --> GetCurrentServices()
            // DB is leading, remove services not in active database list
            // Register services which are not registered yet

            return String.Join(", ", GetCurrentServices().ToArray());
        }

        // Get the current services in consul
        public static List<string> GetCurrentServices()
        {
            List<string> currentServices = new List<string>();

            // Get the current services magic
            var result = GetServicesFromAgent().Response;
            currentServices = (from obj in result where obj.Key.EndsWith("-db") select obj.Key).ToList();
            return currentServices; 
        }

        private static QueryResult<Dictionary<string,AgentService>> GetServicesFromAgent()
        {
            using (var client = new ConsulClient())
            {
                var task = client.Agent.Services();
                try
                {
                    task.Wait();
                    return task.Result;
                }
                catch (Exception e)
                {
                    var ret = new QueryResult<Dictionary<string, AgentService>>();
                    ret.StatusCode = System.Net.HttpStatusCode.NotFound;
                    ret.Response = new Dictionary<string, AgentService>();
                    return ret;
                }
            }
        }

        private static WriteResult RegisterServiceInAgent(AgentServiceRegistration svc)
        {
            using (var client = new ConsulClient())
            {
                var task = client.Agent.ServiceRegister(svc);
                try
                {
                    task.Wait();
                    return task.Result;
                }
                catch (Exception e)
                {
                    var ret = new WriteResult();
                    ret.StatusCode = System.Net.HttpStatusCode.NotFound;
                    return ret;
                }
            }
        }
    }
}
