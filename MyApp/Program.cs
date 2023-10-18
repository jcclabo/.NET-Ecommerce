using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Rewrite;
using MyApp.App.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // ensure client side scripts cannot access the session cookie
});

builder.Services.AddTransient<GlobalErrorHandlingMiddleware>();

var app = builder.Build();

// Global error handler
app.UseMiddleware<GlobalErrorHandlingMiddleware>();

// Enforce lowercase urls
var options = new RewriteOptions().Add(new RedirectLowerCaseRule());
app.UseRewriter(options);

if (!app.Environment.IsDevelopment()) {
    app.UseHsts(); // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
}

app.UseCookiePolicy(new CookiePolicyOptions {
    Secure = CookieSecurePolicy.Always // require secure cookies
});

app.UseSession();

app.UseHttpsRedirection();

// validate requests for account pages
app.Use(async (context, next) => {
    string path = context.Request.Path.ToString();
    // admin pages
    if (path.Contains("admin", StringComparison.CurrentCultureIgnoreCase) && context.Session.GetString("admin") == null) {
        if (path.Contains("/admin/login", StringComparison.CurrentCultureIgnoreCase)) {
            // log
        } else {
            context.Response.Redirect("/", false);
        }
    }
    // user pages
    if (path.Contains("myaccount") && context.Session.GetString("customer") == null) {
         context.Response.Redirect("/login", false);
    }
    await next(context);
});

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
