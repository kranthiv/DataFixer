using DbUp;
using DbUp.Helpers;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Reflection;

namespace DataFixer
{
    class Program
    {
        static int Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddCommandLine(args);

            var configuration = builder.Build();

            var connectionString = configuration.GetConnectionString("Db");

            var upgradeEngineBuilder = DeployChanges
                .To
                .SqlDatabase(connectionString)
                .WithExecutionTimeout(TimeSpan.FromSeconds(Convert.ToInt32(configuration.GetSection("ScriptExecutionTimeout").Value)))
                .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
                .LogToConsole()
                .JournalToSqlTable("dbo", "SchemaVersions");

            if (bool.TrueString.Equals(configuration["EnableTransaction"], StringComparison.OrdinalIgnoreCase))
            {
                upgradeEngineBuilder.WithTransactionPerScript();
            }

            var upgrader = upgradeEngineBuilder.Build();


            if (bool.TrueString.Equals(configuration["GenerateReport"], StringComparison.OrdinalIgnoreCase))
            {
                upgrader.GenerateUpgradeHtmlReport($"UpgradeReport-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.html");
            }


            if (!upgrader.TryConnect(out var error))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(error);
                Console.ResetColor();
                return -1;
            }

            EnsureDatabase.For.SqlDatabase(connectionString);

            var result = upgrader.PerformUpgrade();

            if (!result.Successful)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(result.Error);
                Console.ResetColor();
                return -1;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Success!");
            Console.ResetColor();
            return 0;
        }
    }
}
