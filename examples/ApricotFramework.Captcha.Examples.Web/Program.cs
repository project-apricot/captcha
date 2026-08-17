using ApricotFramework.Captcha.AspNetCore;
using ApricotFramework.Captcha.AspNetCore.Extensions;
using ApricotFramework.Captcha.ErrorDefinitions.Extensions;
using ApricotFramework.Captcha.Model;
using ApricotFramework.ErrorDefinitions.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// The host owns error handling; the library only contributes a mapper to it.
builder.Services.AddErrorDefinitions();

// One call: the provider instances come from configuration, not from code.
builder.Services.AddCaptcha(builder.Configuration);

// Without this a rejected captcha is an unrecognised exception, and answers 500 with no detail.
builder.Services.AddCaptchaErrorDefinitions();

var app = builder.Build();

app.UseExceptionHandler();

app.MapControllers();

// The same guard without MVC, for a host that does not use controllers.
app.MapPost("/api/minimal", async (HttpContext context, ICaptchaGuard guard, CancellationToken cancellationToken) =>
{
    await guard.EnsureAsync(context, new CaptchaRequirements(), options: null, cancellationToken);

    return Results.Ok(new { Reached = "minimal" });
});

app.Run();
