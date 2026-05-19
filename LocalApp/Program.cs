//using Microsoft.Extensions.DependencyInjection;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using static System.Net.Mime.MediaTypeNames;

//namespace LocalApp
//{
//    internal class Program
//    {
//        // This holds all the registered tools and services
//        public static IServiceProvider ServiceProvider { get; private set; }
//        [STAThread]
//        static void Main(string[] args)
//        {
//            // 1. Initialize the "Brain" (Dependency Injection)
//            ServiceProvider = Startup.ConfigureServices();

//            // 2. Resolve the Connection Manager to start the SignalR pipe
//            var connectionManager = ServiceProvider.GetRequiredService<ConnectionManager>();

//            // Start the background connection task
//            try
//            {
//                // 3. Start the connection synchronously to avoid the 'async Main' error.
//                // We use .GetAwaiter().GetResult() to block until it connects.
//                // Note: Passing a dummy token since your StartAsync requires a B2B token string.
//                connectionManager.StartAsync("dev_testing_token").GetAwaiter().GetResult();
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Failed to start connection: {ex.Message}");
//            }

//            // 4. Keep the application running in the background
//            Console.WriteLine("Tekla Bridge is active and listening.");
//            Console.WriteLine("Press ENTER to exit the application...");
//            Console.ReadLine();
//        }
//    }
//}

using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Win32;

namespace LocalApp
{
    internal class Program
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        [STAThread]
        static void Main(string[] args)
        {
            // 1. ATTACH RESOLVER FIRST - Before any other code runs
            AppDomain.CurrentDomain.AssemblyResolve += TeklaAssemblyResolver;

            // 2. RUN APP IN SEPARATE METHOD
            // We call this in a separate method to ensure the JIT compiler doesn't 
            // look for Tekla DLLs before the resolver is attached.
            RunApp(args);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void RunApp(string[] args)
        {
            try
            {
                ServiceProvider = Startup.ConfigureServices();
                var connectionManager = ServiceProvider.GetRequiredService<ConnectionManager>();

                Console.WriteLine("Tekla Bridge is active and listening.");

                // Start connection
                connectionManager.StartAsync("dev_testing_token").GetAwaiter().GetResult();

                Console.WriteLine("Press ENTER to exit...");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Critical Error: {ex.Message}");
                Console.ReadLine();
            }
        }

        private static Assembly TeklaAssemblyResolver(object sender, ResolveEventArgs args)
        {
            // Only resolve Tekla-related assemblies
            if (args.Name.Contains("Tekla") || args.Name.Contains("Trimble.Remoting"))
            {
                string assemblyName = new AssemblyName(args.Name).Name + ".dll";
                string teklaPath = GetTeklaBinPath();
                string fullPath = Path.Combine(teklaPath, assemblyName);

                if (File.Exists(fullPath))
                {
                    return Assembly.LoadFrom(fullPath);
                }
            }
            return null;
        }

        private static string GetTeklaBinPath()
        {
            // Try to get path from Registry for Tekla 2024
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Trimble\Tekla Structures\2024.0\Setup"))
                {
                    if (key != null)
                    {
                        return key.GetValue("BinDirectory")?.ToString();
                    }
                }
            }
            catch { /* Fallback to default if registry fails */ }

            return @"C:\Program Files\Tekla Structures\2024.0\bin\";
        }
    }
}