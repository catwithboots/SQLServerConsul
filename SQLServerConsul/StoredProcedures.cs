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
        public static void UpdateConsulServices()
        {
            // Consul Environment --> Input from SQL Server
            // List of Active Databases --> Input from SQL Server
            // List of Current consul services --> GetCurrentServices()
            // DB is leading, remove services not in active database list
            // Register services which are not registered yet

            using (var client = new ConsulClient())
            {
                
            }
        }

        // Get the current services in consul
        private static List<string> GetCurrentServices()
        {
            List<string> currentServices = new List<string>();

            // Get the current services magic

            return currentServices; 
        }
    }
}
