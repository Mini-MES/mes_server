using mes_server.Data;
using Microsoft.EntityFrameworkCore;

namespace mes_server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddDbContext<MESDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MESDbConnection")));

            var app = builder.Build();

            app.MapGet("/", () => "Hello World!");

            app.Run();
        }
    }
}
