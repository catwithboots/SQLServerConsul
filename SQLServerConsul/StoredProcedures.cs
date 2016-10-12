using System;
using System.Collections.Generic;
using System.Linq;
using Consul;

namespace SQLServerConsul
{
    public static class StoredProcedures
    {
        public static void UpdateConsulServices(string activeDatabases, string suffix = "-db", int ttlSeconds = 60)
        {
            // Consul Environment --> Input from SQL Server
            // List of Active Databases --> Input from SQL Server
            // List of Current consul services --> GetCurrentServices()
            // DB is leading, remove services not in active database list
            // Register services which are not registered yet
            var currentDatabases = activeDatabases.Split(',').ToList();
            var registeredDatabases = GetCurrentServices(suffix);

            var toRegister = currentDatabases.Except(registeredDatabases).ToList();
            var toDeRegister = registeredDatabases.Except(currentDatabases).ToList();
            var toUpdate = registeredDatabases.Intersect(currentDatabases).ToList();

            RegisterServices(toRegister, suffix, ttlSeconds);
            DeRegisterServices(toDeRegister, suffix);
            UpdateServices(toUpdate, suffix);

            var returnstring = "Registered: " + string.Join(", ", toRegister.ToArray()) + Environment.NewLine + 
                "DeRegistered : " + string.Join(", ", toDeRegister.ToArray()) + Environment.NewLine + 
                "Updated : " + string.Join(", ", toUpdate.ToArray());

            return;
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

        public static void RegisterServices(List<string> services, string suffix, int ttlSeconds)
        {
            foreach (string service in services)
            {
                RegisterServiceInAgent(CreateAgentServiceRegistration(service + suffix, ttlSeconds));
                UpdateServiceInAgent(service + suffix);
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
                    var ret = new QueryResult<Dictionary<string, AgentService>>();
                    ret.StatusCode = System.Net.HttpStatusCode.NotFound;
                    ret.Response = new Dictionary<string, AgentService>();
                    return ret;
                }
            }
        }

        private static AgentServiceRegistration CreateAgentServiceRegistration(string svc, int ttlSeconds)
        {
            var reg = new AgentServiceRegistration();
            var chk = new AgentServiceCheck();
            chk.DeregisterCriticalServiceAfter = new TimeSpan(1, 0, 0, 0);
            chk.TTL = new TimeSpan(0, 0, ttlSeconds);
            reg.Name = svc;
            reg.Port = 1433;
            reg.Check = chk;
            return reg;
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
