using EasyGames.Data;
using EasyGames.Models;
using EasyGames.Options;
using EasyGames.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;


var builder = WebApplication.CreateBuilder(args);

//  identity configuration with confirmed accounts
//  this registers asp.net core identity with applicationuser and identityrole using ef core stores
//  option o.signin.requireconfirmedaccount = true means users must confirm their email before they can log in
//  this enforces the security pattern discussed in lecture 1 (prevent fake/spam accounts until confirmed)
//  adddefaulttokenproviders wires up email confirmation and password reset token generators

//  email sender service
//  here iemailsender is bound to consoleemailsender as a singleton so that confirmation links are written to the console during development
//  in production you would swap this out for smtp or another provider
//  matches assignment requirement to support account confirmation even if only via console log
//  cart services and session
//  registers httpcontextaccessor so services can read/write cookies and session state
//  icarstervice is scoped so a fresh instance is created per request
//  adddistributedmemorycache + addsession configure in-memory session storage
//  idle timeout = 30 minutes ensures carts expire after inactivity
//  aligns with lecture 2’s cart + dependency injection example:contentReference

//  anonymous cart cookie middleware
//  this inline middleware ensures every visitor has an anon_cart_id cookie if they aren’t logged in
//  cookie is httponly essential and expires in 30 days
//  supports merging anonymous carts into user carts on login as implemented in loginmodel
//  mitigates fixation risks by generating a new guid rather than using user-supplied ids

//  session middleware in pipeline
//  app.usesession() must run before mvc routing so controllers can access session data
//  needed for storing cart data server-side under a stable key per user
//  corresponds to lecture advice that session is required if using cart backed by httpcontext 

//  These notes are more for me at this point... Add services to container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();


// bind TierRules from appsettings
builder.Services.Configure<TierRulesOptions>(
    builder.Configuration.GetSection("TierRules"));

// add TierService
builder.Services.AddScoped<ITierService, TierService>();

// ... existing services (DbContext, Identity, MVC, etc.)

builder.Services.AddScoped<ICustomerProfileService, CustomerProfileService>();

builder.Services.AddScoped<ISalesService, SalesService>();

//POS service
builder.Services.AddScoped<IPosCartService, PosCartService>();
builder.Services.AddScoped<IPosStateService, PosStateService>();

//  Requria a confirmed account, based
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(o =>
    {
        o.SignIn.RequireConfirmedAccount = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
//  Create a singletone emailsender interface for dev confirmation
builder.Services.AddSingleton<IEmailSender, ConsoleEmailSender>();

//  Cart constructor
builder.Services.AddHttpContextAccessor(); //   Req for cookies from DI
builder.Services.AddScoped<ICartService, CartService>(); // Fresh scoped Cart service per http request
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o => { o.Cookie.HttpOnly = true; o.IdleTimeout = TimeSpan.FromMinutes(30); }); //   Security stuff to prevent XSS

var app = builder.Build();
//  Create a cookie for the anonymous cart id thingy
const string AnonCartCookie = "anon_cart_id";
app.Use(async (ctx, next) =>
{
    if (!ctx.Request.Cookies.ContainsKey(AnonCartCookie))
    {
        ctx.Response.Cookies.Append(
            AnonCartCookie,
            Guid.NewGuid().ToString(),
            new CookieOptions { IsEssential = true, HttpOnly = true, Expires = DateTimeOffset.UtcNow.AddDays(30) }
        );
    }
    await next();
});
// Configure the HTTP request pipeline.



if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}



//  To detect whether we are seeding or deseeding the database
if (args.FirstOrDefault(a => a == "--seed" || a == "--deseed") is string flag)
{
    using (var scope = app.Services.CreateScope())
    {
        var service = scope.ServiceProvider;
        Seed.SeedingTime(flag, service);
    }
}
//  Just call it anyways lol
using (var scope = app.Services.CreateScope())
{
    var service = scope.ServiceProvider;
    Seed.SeedingTime("--seed", service);
}



app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();


async Task SeedAsync(IHost app)
{
    using var scope = app.Services.CreateScope();
    var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    if (!await roles.RoleExistsAsync("Staff"))
        await roles.CreateAsync(new IdentityRole("Staff"));

    // assign yourself as staff to see staff view:
    var me = await users.FindByEmailAsync("you@example.com");
    if (me != null && !await users.IsInRoleAsync(me, "Staff"))
        await users.AddToRoleAsync(me, "Staff");
}

// call it once at startup (don’t keep in production)
await SeedAsync(app);

