using Microsoft.Data.SqlClient;
using System.Configuration;

namespace B2B.WinForms.Database;

public static class DbConnectionFactory
{
    public static SqlConnection Create()
    {
        // Read the project's App.config explicitly (copied beside the executable at build time).
        var map = new ExeConfigurationFileMap
        {
            ExeConfigFilename = Path.Combine(AppContext.BaseDirectory, "App.config")
        };
        Configuration config = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None);
        string? connectionString = config.ConnectionStrings.ConnectionStrings["B2BWholesale"]?.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Missing connection string 'B2BWholesale' in App.config.");
        return new SqlConnection(connectionString);
    }
}
