using mes_server.Data;
using mes_server.Repositories.Generic;
using mes_server.Repositories.History;
using mes_server.Repositories.Interface.Generic;
using mes_server.Repositories.Interface.History;
using mes_server.Repositories.Interface.MasterData;
using mes_server.Repositories.Interface.Production;
using mes_server.Repositories.MasterData;
using mes_server.Repositories.Production;
using mes_server.Services;
using mes_server.Services.Interface;
using Microsoft.EntityFrameworkCore;

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

            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

            // 2. Custom Repository 등록
            builder.Services.AddScoped<IPerformanceRepository, PerformanceRepository>();
            builder.Services.AddScoped<IWorkOrderRepository, WorkOrderRepository>();
            builder.Services.AddScoped<ILotRepository, LotRepository>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IBOMRepository, BOMRepository>();
            builder.Services.AddScoped<IToolHistoryRepository, ToolHistoryRepository>();


            builder.Services.AddScoped(typeof(IGenericService<>), typeof(BaseSerivce<>));

            // 3. Service 등록
            builder.Services.AddScoped<IToolService, ToolService>();
            builder.Services.AddScoped<IMasterDataService, MasterDataService>();
            builder.Services.AddScoped<IInventoryService, InventoryService>();
            builder.Services.AddScoped<IProductionService, ProductionService>();
            builder.Services.AddScoped<IUserService, UserService>();

            builder.Services.AddControllers();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            if(app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.MapControllers();

            app.Run();
        }
    }
}
