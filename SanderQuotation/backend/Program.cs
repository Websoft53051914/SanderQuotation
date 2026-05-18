using backend.AI;
using backend.Common;
using backend.EIPSource;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Localization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.SemanticKernel;
using System.Reflection;
using System.Text;

IConfiguration Config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// 指定 wwwroot 作為 WebRoot
builder.WebHost.UseWebRoot("wwwroot"); // 如果你要用 asset 也可以 "asset"

#region Localization
var localizationoptions = new RequestLocalizationOptions();
var supportedCultures = new List<System.Globalization.CultureInfo> {
        new System.Globalization.CultureInfo("zh-TW"),
        new System.Globalization.CultureInfo("en-US")
    };
localizationoptions.SupportedCultures = supportedCultures;
localizationoptions.SupportedUICultures = supportedCultures;
localizationoptions.SetDefaultCulture("en-US");
localizationoptions.ApplyCurrentCultureToResponseHeaders = true;

localizationoptions.RequestCultureProviders = new List<IRequestCultureProvider>
{
    new QueryStringRequestCultureProvider(),
    new CookieRequestCultureProvider()
    // ❌ 刻意不放 AcceptLanguageHeaderRequestCultureProvider
};
#endregion

builder.Services.AddMemoryCache();
builder.Services.AddScoped<ISystemLockService, SystemLockService>();

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ApiSystemLockFilter>();
});

builder.Services.AddControllers();
builder.Services.AddScoped<AdAuthService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddSingleton<SynoNasHelper>();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
});

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

// 註冊服務
builder.Services.AddControllersWithViews();

// 註冊 Session 服務
builder.Services.AddDistributedMemoryCache(); // 使用內存快取

builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".net.core.Session";
    options.IdleTimeout = TimeSpan.FromMinutes(15);
    //options.Cookie.IsEssential = true; //架設http 非 https 要註解 2025/12/8解除

    //options.Cookie.HttpOnly = true; //架設http 非 https 要註解 2025/12/8解除
    //options.Cookie.SecurePolicy = CookieSecurePolicy.Always; //架設http 非 https 要註解 2025/12/8解除

    // ⭐ 新增：跨埠/跨站點傳輸必須設定為 None
    //options.Cookie.SameSite = SameSiteMode.None;
});



builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddHttpClient();
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        // response 回傳屬性不強制改成 camelcase
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });


builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile("message.json", optional: true, reloadOnChange: true);
var useHttps = builder.Configuration.GetValue<bool>("IsHttps");
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
})
 .AddCookie(options =>
 {
     options.Cookie.HttpOnly = true;
     options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // 改為隨請求決定

     // SameSite=None 需要 Secure=true (HTTPS)，HTTP 環境改為 Lax
     if (useHttps)
     {
         options.Cookie.SameSite = SameSiteMode.None; // 跨站點傳輸
     }
     else
     {
        options.Cookie.SameSite = SameSiteMode.Lax; // HTTP 環境使用 Lax
     }
 });

// 註冊 CORS - 統一設定，適用於所有環境
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowHost",
        policy =>
        {
            policy.WithOrigins(
                  "https://localhost:7067",          // Windows 前端 local HTTPS 開發環境   
                  "http://localhost:7067",           // Windows 前端 local 開發環境 
                  "http://localhost:5134",           // Windows 前端 local HTTP 開發環境
                  "http://192.168.1.46:1011",       // Linux 前端環境 
                  "http://localhost:1011"            // Linux 前端環境 
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
          .WithExposedHeaders("Content-Disposition"); // <- 重要;
        });
});


// JWT 設定
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Issuer"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
        ),

        ClockSkew = TimeSpan.Zero
    };

    // ⭐ 改從 Cookie 取 Token
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (context.Request.Cookies.ContainsKey(Const.Value.JWT_TokenName))
            {
                context.Token = context.Request.Cookies[Const.Value.JWT_TokenName];
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();



builder.Services.AddHangfire(config =>
    config.UseInMemoryStorage(new()
    {
        MaxExpirationTime = TimeSpan.FromHours(1),
    })
);
builder.Services.AddHangfireServer(options =>
{
    options.SchedulePollingInterval = TimeSpan.FromSeconds(5);
    options.WorkerCount = Environment.ProcessorCount * 5;
});
builder.Services.AddSingleton<ExtractKeywordHandler>();
builder.Services.AddSingleton<ManualBatchEmbedding>();
builder.Services.AddSingleton<ExtractKeywordJob>();
builder.Services.AddSingleton<DesideSanderModuleItemNoJob>();
builder.Services.AddSingleton<HangfireSchedulerHelper>();
#if !DEBUG
//builder.Services.AddHostedService<EIPSourceScheduleHostService>();
#endif
builder.Services.AddSingleton<TransferJob>();
builder.Services.AddSingleton<ExtractKeywordJob>();

#region AI - Semantic Kernel
builder.Services.AddHttpClient("GeminiHttpClient", client =>
{
    client.Timeout = TimeSpan.FromMinutes(10);
});
string geminiApiKey = builder.Configuration["GeminiApiKey"] ?? string.Empty;
var httpClientFactory = builder.Services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();
var geminiHttpClient = httpClientFactory.CreateClient("GeminiHttpClient");
builder.Services.AddKernel()
                .AddGoogleAIGeminiChatCompletion(
                    modelId: "gemini-3.1-flash-lite-preview",
                    apiKey: geminiApiKey,
                    httpClient: geminiHttpClient
                );
#endregion

builder.Services.AddSingleton<ManualBatchEmbedding>();

var app = builder.Build();

app.UseStaticFiles(); // ⭐ 必須有

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

if (useHttps)
{
    app.UseHttpsRedirection();
}


// 使用 CORS
app.UseCors("AllowHost");


app.UseHangfireDashboard();

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

app.MapControllers();



app.UseRequestLocalization(localizationoptions);
//// 專案啟動時載入
var container = new Unity.UnityContainer();
Business.BusinessFactory.Register(container);
backend.Common.HttpContext.Configure(app.Services.GetRequiredService<IHttpContextAccessor>());

#if DEBUG
// 測試用：啟動時立即將 ExtractKeywordJob.ExecuteAsync 排入 Hangfire Queue
//BackgroundJob.Enqueue<ExtractKeywordJob>(job => job.ExecuteAsync());
#endif

app.Run();
