using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Consul;
using Microsoft.SqlServer.Server;

namespace SQLServerConsul
{
    public static class StoredProcedures
    {
        [SqlProcedure()]
        public static void UpdateConsulServices(string activeDatabases, string suffix = "-db", int ttlSeconds = 60, bool useFqdn = false)
        {
            List<string> list1 = ((IEnumerable<string>)activeDatabases.Split(',')).ToList<string>();
            list1 = list1.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
            List<string> currentServices = GetCurrentServices(suffix);
            currentServices = currentServices.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
            List<string> list2 = list1.Except<string>((IEnumerable<string>)currentServices).ToList<string>();
            List<string> list3 = currentServices.Except<string>((IEnumerable<string>)list1).ToList<string>();
            List<string> list4 = currentServices.Intersect<string>((IEnumerable<string>)list1).ToList<string>();
            RegisterServices(list2, suffix, ttlSeconds, useFqdn);
            DeRegisterServices(list3, suffix);
            UpdateServices(list4, suffix);

            var info = "Registered: " + string.Join(", ", list2.ToArray()) + Environment.NewLine + "DeRegistered : " + string.Join(", ", list3.ToArray()) + Environment.NewLine + "Updated : " + string.Join(", ", list4.ToArray());
            if (SqlContext.IsAvailable)
            {
                SqlContext.Pipe.Send(info);
            } 
        }

        // Get the current services in consul
        public static List<string> GetCurrentServices(string suffix)
        {
            List<string> currentServices = new List<string>();

            // Get the current services magic
            var result = GetServicesFromAgent().Response;
            currentServices = (from obj in result where obj.Key.EndsWith(suffix) select obj.Key.TrimEnd(suffix)).ToList();
            return currentServices; 
        }

        public static void RegisterServices(List<string> services, string suffix, int ttlSeconds, bool useFqdn = false)
        {
            foreach (string service in services)
            {
                RegisterServiceInAgent(CreateAgentServiceRegistration(service + suffix, ttlSeconds, useFqdn));
                UpdateServiceInAgent(service + suffix, "Updated by SQLServerConsul");
            }            
        }

        public static void DeRegisterServices(List<string> services, string suffix)
        {
            foreach (string service in services)
            {
                var wr = DeRegisterServiceInAgent(service + suffix);
            }
        }

        public static void UpdateServices(List<string> services, string suffix)
        {
            foreach (string service in services)
            {
                UpdateServiceInAgent(service + suffix);
            }
        }

        private static QueryResult<Dictionary<string, AgentService>> GetServicesFromAgent()
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
                    if (SqlContext.IsAvailable)
                    {
                        SqlContext.Pipe.Send("Error in GetServicesFromAgent");
                        SqlContext.Pipe.Send(e.Message);
                        SqlContext.Pipe.Send(e.InnerException.Message);
                        SqlContext.Pipe.Send(e.StackTrace);
                    }
                    var ret = new QueryResult<Dictionary<string, AgentService>>();
                    ret.StatusCode = System.Net.HttpStatusCode.NotFound;
                    ret.Response = new Dictionary<string, AgentService>();
                    return ret;
                }
            }
        }

        private static AgentServiceRegistration CreateAgentServiceRegistration(string svc, int ttlSeconds, bool useFqdn = false)
        {
            AgentServiceRegistration serviceRegistration = new AgentServiceRegistration();
            AgentServiceCheck agentServiceCheck = new AgentServiceCheck();
            string hostName = Dns.GetHostEntry("LocalHost").HostName;
            agentServiceCheck.DeregisterCriticalServiceAfter = new TimeSpan?(new TimeSpan(1, 0, 0, 0));
            agentServiceCheck.TTL = new TimeSpan?(new TimeSpan(0, 0, ttlSeconds));
            serviceRegistration.Name = svc;
            if (useFqdn)
                serviceRegistration.Address = hostName;
            serviceRegistration.Port = 1433;
            serviceRegistration.Check = agentServiceCheck;
            return serviceRegistration;
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
                    if (SqlContext.IsAvailable)
                    {
                        SqlContext.Pipe.Send("Error in RegisterServiceInAgent");
                        SqlContext.Pipe.Send(e.Message);
                        SqlContext.Pipe.Send(e.InnerException.Message);
                        SqlContext.Pipe.Send(e.StackTrace);
                    }
                    var ret = new WriteResult();
                    ret.StatusCode = System.Net.HttpStatusCode.NotFound;
                    return ret;
                }
            }
        }

        private static void UpdateServiceInAgent(string svc, string note = "Updated by SQLServerConsul")
        {
            using (var client = new ConsulClient())
            {
                var task = client.Agent.PassTTL("service:" + svc, note);
                try
                {
                    task.Wait();
                }
                catch (Exception e)
                {
                    if (SqlContext.IsAvailable)
                    {
                        SqlContext.Pipe.Send("Error in UpdateServiceInAgent");
                        SqlContext.Pipe.Send(e.Message);
                        SqlContext.Pipe.Send(e.InnerException.Message);
                        SqlContext.Pipe.Send(e.StackTrace);
                    }
                }
            }
        }

        private static WriteResult DeRegisterServiceInAgent(string svc)
        {
            using (var client = new ConsulClient())
            {
                var task = client.Agent.ServiceDeregister(svc);
                try
                {
                    task.Wait();
                    return task.Result;
                }
                catch (Exception e)
                {
                    if (SqlContext.IsAvailable)
                    {
                        SqlContext.Pipe.Send("Error in DeRegisterServiceInAgent");
                        SqlContext.Pipe.Send(e.Message);
                        SqlContext.Pipe.Send(e.InnerException.Message);
                        SqlContext.Pipe.Send(e.StackTrace);
                    }
                    var ret = new WriteResult();
                    ret.StatusCode = System.Net.HttpStatusCode.NotFound;
                    return ret;
                }
            }
        }

        public static string TrimEnd(this string source, string value)
        {
            if (!source.EndsWith(value))
                return source;

            return source.Remove(source.LastIndexOf(value));
        }
    }
}
