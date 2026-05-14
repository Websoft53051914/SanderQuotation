
using Core.Utility.Web.HtmlHelperCustom;
using frontend.Common;
using frontend.Common.ConfigurationHelper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Localization;
using Microsoft.IdentityModel.Tokens;
using System.Text;


var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
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

builder.Services.AddHttpClient("api", client =>
{
    client.BaseAddress = new Uri(Method.GetAppSettingsDataByName("BackendURL"));
});

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<FrontendSystemLockFilter>();
});

builder.Services.AddSession();


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
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Issuer"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
        ),
    };

    // ⭐ 從 Cookie 讀 JWT
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (context.Request.Cookies.ContainsKey(Const.Value.JWT_TokenName))
            {
                context.Token = context.Request.Cookies[Const.Value.JWT_TokenName];
            }
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            context.HandleResponse();
            //context.Response.Redirect("/Login/index");
            context.Response.Redirect("/home/index");
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// Add services to the container.
builder.Services.AddRazorPages();


builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
 
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
})
 .AddCookie(options =>
 {
     //options.Cookie.HttpOnly = true;
     //options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
 });

builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".net.core.Session";
    options.IdleTimeout = TimeSpan.FromMinutes(15);
    //options.Cookie.IsEssential = true; //架設http 非 https 要註解

    //options.Cookie.HttpOnly = true; //架設http 非 https 要註解
    //options.Cookie.SecurePolicy = CookieSecurePolicy.Always; //架設http 非 https 要註解
});

builder.Services.AddAntiforgery(options =>
{
    //options.Cookie.SecurePolicy = CookieSecurePolicy.Always; //架設http 非 https 要註解
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
builder.Services.AddSingleton<ConfigurationHelper>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    //app.UseHsts(); //架設http 非 https 要註解
}
if (useHttps)
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

#region Localization
//app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);
app.UseRequestLocalization(localizationoptions);
#endregion

app.UseRouting();
//app.UseHangfireDashboard();


//app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

app.MapRazorPages();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=login}/{action=Index}/{id?}");

frontend.Common.HttpContext.Configure(app.Services.GetRequiredService<IHttpContextAccessor>());

app.Run();