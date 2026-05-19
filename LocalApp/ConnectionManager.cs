using Contracts.Envelopes;
using Contracts.Interfaces;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Linq;

namespace LocalApp
{
    internal class ConnectionManager
    {
        private HubConnection _connection;
        private readonly CommandRouter _router;
        private readonly string _hubUrl = "http://43.204.253.228:8080/teklahub"; // Replace with AWS URL

        //private readonly string _hubUrl = "http://localhost:5000/teklahub";
        public ConnectionManager(CommandRouter router)
        {
            _router = router;
        }

        // for automatically configuring user's claude desktop config file for connecting mcp server
        private void AutoConfigureClaudeDesktop(string userId)
        {
            try
            {
                // 1. Dynamically find the path (resolves C:\Users\{Username}\AppData\Local)
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var packagesDir = Path.Combine(localAppData, "Packages");

                // Use a wildcard to find the Claude folder just in case the suffix changes
                var claudeDir = Directory.GetDirectories(packagesDir, "Claude_*").FirstOrDefault();

                if (claudeDir == null)
                {
                    Console.WriteLine("[Warning] Claude Desktop folder not found on this machine.");
                    return;
                }

                var configPath = Path.Combine(claudeDir, "LocalCache", "Roaming", "Claude", "claude_desktop_config.json");

                JObject config;

                // 2. Read existing config to preserve their preferences, or create new if missing
                if (File.Exists(configPath))
                {
                    var existingJson = File.ReadAllText(configPath);
                    config = JObject.Parse(existingJson);
                }
                else
                {
                    config = new JObject();
                }

                // 3. Ensure 'mcpServers' object exists
                if (config["mcpServers"] == null)
                {
                    config["mcpServers"] = new JObject();
                }

                // 4. Inject the Tekla Bridge configuration with the dynamic User ID
                // Note: Change localhost to your AWS domain before distributing
                var mcpUrl = $"http://43.204.253.228:8080/mcp?userId={userId}";

                config["mcpServers"]["tekla-bridge"] = new JObject(
                    new JProperty("command", "npx"),
                    new JProperty("args", new JArray(
                        "-y", // Added '-y' so npx doesn't prompt the user to install packages
                        "mcp-remote",
                        mcpUrl,
                        "--allow-http"
                    ))
                );

                // 5. Ensure the directory exists and save the file safely
                Directory.CreateDirectory(Path.GetDirectoryName(configPath));
                File.WriteAllText(configPath, config.ToString(Formatting.Indented));

                Console.WriteLine("[Config] Claude Desktop successfully configured for Tekla Bridge.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Could not update Claude config: {ex.Message}");
            }
        }
        // Local App (.NET 4.8) Logic
        public static string GetUniqueMachineId()
        {
            // Returns the SID of the local machine/user - very stable
            return System.Security.Principal.WindowsIdentity.GetCurrent().User.Value;
        }

        public async System.Threading.Tasks.Task StartAsync(string b2bToken)
        {
            var machineId = GetUniqueMachineId();
            AutoConfigureClaudeDesktop(machineId);
            // 1. Configure the Connection
            //var hubUrlWithUser = $"{_hubUrl}?userId={GetUniqueMachineId()}";
            _connection = new HubConnectionBuilder()
                .WithUrl(_hubUrl, options =>
                {
                    // Add userId in the headers
                    options.Headers.Add("X-Tekla-User-Id",machineId);
                    // Attach your B2B JWT Token for security
                    options.AccessTokenProvider = () => System.Threading.Tasks.Task.FromResult(b2bToken);
                })
                // CRITICAL: Must match the Server's protocol to avoid serialization errors
                .AddNewtonsoftJsonProtocol()
                .WithAutomaticReconnect(new[] {
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(30)
                })
                .Build();

            // 2. The Core Bridge: Listening for "ExecuteGenericCommand"
            // This matches the ITeklaClient interface method name exactly
            _connection.On<GenericEnvelope, GenericEnvelope>(
                nameof(ITeklaClient.ExecuteGenericCommand),
                async (requestEnvelope) =>
                {
                    try
                    {
                        // Pass the envelope to the Router (O(1) lookup)
                        return await _router.RouteAsync(requestEnvelope);
                    }
                    catch (Exception ex)
                    {
                        // Return a failure envelope so the Cloud/Claude knows what went wrong
                        return new GenericEnvelope
                        {
                            CommandName = requestEnvelope.CommandName + "_Error",
                            Payload = $"Local Error: {ex.Message}"
                        };
                    }
                });
            _connection.On<string>(nameof(ITeklaClient.ForceDisconnect), (reason) =>
                {
                    Console.WriteLine($"Server requested disconnect: {reason}");
                    // Shut down the connection or the app
                    Environment.Exit(0);
                 });

            // 3. Lifecycle Events (Useful for UI Status Icons)
            _connection.Closed += (error) => {
                Console.WriteLine("Connection Lost. Trying to recover...");
                return System.Threading.Tasks.Task.CompletedTask;
            };

            // 4. Fire it up
            try
            {
                await _connection.StartAsync();
                Console.WriteLine("Connected to SignalR hub.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Initial connection failed: {ex.Message}");
            }
        }

        public async System.Threading.Tasks.Task StopAsync()
        {
            if (_connection != null)
            {
                await _connection.StopAsync();
                await _connection.DisposeAsync();
            }
        }
    }
}
