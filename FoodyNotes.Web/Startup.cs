using FluentValidation.AspNetCore;
using FoodyNotes.DataAccess.Postgres;
using FoodyNotes.Infrastructure.Implementation;
using FoodyNotes.Infrastructure.Implementation.Authentication;
using FoodyNotes.Infrastructure.Implementation.PipelineBehaviors;
using FoodyNotes.Infrastructure.Interfaces;
using FoodyNotes.Infrastructure.Interfaces.Authentication;
using FoodyNotes.Infrastructure.Interfaces.Authentication.Dtos;
using FoodyNotes.Infrastructure.Interfaces.Persistence;
using FoodyNotes.UseCases.Authentication.Commands;
using FoodyNotes.UseCases.Validators.Dtos;
using FoodyNotes.Web.Middlewares;
using FoodyNotes.Web.Services;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Serilog;

namespace FoodyNotes.Web
{
  public class Startup
  {
    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
      services.AddDbContext<ApplicationDbContext>(options => 
        options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection")));
      
      services.AddScoped(typeof(IRepositoryBase<,>), typeof(RepositoryBase<,>));
      
      services.AddCors();

      services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));
      
      services.AddScoped<IUserService, UserService>();
      services.AddScoped<IJwtTokenService, JwtTokenService>();
      services.AddScoped<IRefreshTokenService, RefreshTokenService>();
      services.AddScoped<IGoogleService, GoogleService>();
      services.AddScoped<HttpService>();

      services.AddMediatR(typeof(AuthenticateCommand));

      services.AddTransient(typeof(IPipelineBehavior<AuthenticateCommand,AuthenticateResponseDto>), typeof(TestPipelineBehavior));
      services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TestGenericConstraintsPipelineBehavior<,>));
      services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingPipelineBehaviour<,>));
      services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehavior<,>));

      services.AddControllers()
        .AddFluentValidation(x => x.RegisterValidatorsFromAssembly(typeof(AuthenticateRequestDtoValidator).Assembly));

      services.AddApiVersioning(options =>
      {
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.DefaultApiVersion = ApiVersion.Default;
        options.ApiVersionReader = new HeaderApiVersionReader("x-ms-version");
        options.ReportApiVersions = true;
      });
      
      services.AddSwaggerGen(c =>
      {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "FoodyNotes.Web", Version = "v1" });
      });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "FoodyNotes.Web v1"));
      }

      app.UseSerilogRequestLogging();
      
      app.UseMiddleware<ErrorHandlerMiddleware>();
      
      app.UseHttpsRedirection();

      app.UseRouting();

      app.UseCors(x => x
        .SetIsOriginAllowed(origin => true)
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials());

      //app.UseAuthorization(); - our authorization is based on IAuthorizationFilter
      app.UseMiddleware<JwtMiddleware>();

      app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
    }
  }
}