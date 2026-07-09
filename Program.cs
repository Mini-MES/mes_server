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
using mes_server.Models.Settings;
using mes_server.Services.Interface;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace mes_server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

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


            // 4. 인증 관련
            var JwtSettings = builder.Configuration.GetSection("Jwt");
            var keyString = builder.Configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT Key가 설정되지 않았습니다!");
            var key = Encoding.ASCII.GetBytes(keyString);

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true, // 만료 시간 체크
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = JwtSettings["Issuer"],
                    ValidAudience = JwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero // 만료 시간 오차 없이 즉시 만료되도록 설정
                };
            });

            builder.Services.AddAuthorization();
            builder.Services.AddControllers();

            var app = builder.Build();

            // 미들웨어 등록 (순서 중요)
            app.UseAuthentication();
            app.UseAuthorization();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.MapControllers();

            app.Run();
        }
    }
}
