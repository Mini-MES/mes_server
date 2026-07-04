using mes_server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace mes_server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var connectionString = builder.Configuration.GetConnectionString("MESDbConnection")
                 ?? throw new InvalidOperationException("Connection string 'MESDbConnection' was not found.");
            builder.Services.AddDbContext<MESDbContext>(options => options.UseSqlServer(connectionString));

            var app = builder.Build();

            app.MapGet("/", () => "Hello World!");

            app.Run();
        }
    }
}
