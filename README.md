# ApricotFramework.Captcha

[![NuGet](https://img.shields.io/nuget/v/ApricotFramework.Captcha.svg?label=ApricotFramework.Captcha)](https://www.nuget.org/packages/ApricotFramework.Captcha/)
[![NuGet](https://img.shields.io/nuget/v/ApricotFramework.Captcha.AspNetCore.svg?label=ApricotFramework.Captcha.AspNetCore)](https://www.nuget.org/packages/ApricotFramework.Captcha.AspNetCore/)
[![NuGet](https://img.shields.io/nuget/v/ApricotFramework.Captcha.ErrorDefinitions.svg?label=ApricotFramework.Captcha.ErrorDefinitions)](https://www.nuget.org/packages/ApricotFramework.Captcha.ErrorDefinitions/)
[![CI](https://github.com/project-apricot/captcha/actions/workflows/ci.yml/badge.svg)](https://github.com/project-apricot/captcha/actions/workflows/ci.yml)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](https://github.com/project-apricot/captcha/blob/main/LICENSE)

reCAPTCHA, hCaptcha and Cloudflare Turnstile behind one interface, with each provider's answer
normalised into the same result and judged against what the endpoint declared it needs — a minimum
score, a set of actions, a set of hostnames. Providers are named instances, so one service can hold a
different key per surface or per tenant.

`ApricotFramework.Captcha` is the **zero-dependency** core.

## Install

```bash
dotnet add package ApricotFramework.Captcha.AspNetCore
dotnet add package ApricotFramework.Captcha.ErrorDefinitions   # to answer with problem+json
```

## Usage

```csharp
builder.Services.AddCaptcha(builder.Configuration);
```

```jsonc
{
  "Captcha": {
    "DefaultProvider": "Default",
    "Providers": {
      "Default":     { "Type": "recaptcha", "SiteKey": "6Lc...", "Secret": "..." },
      "AdminPortal": { "Type": "recaptcha", "SiteKey": "6Lc...", "Secret": "..." }
    }
  }
}
```

```csharp
// Provider names a configured instance, so this surface uses a key of its own.
[HttpPost("sign-in")]
[ValidateCaptcha(Provider = "AdminPortal", Policy = CaptchaValidationPolicy.High,
                 AllowedActions = ["sign_in"])]
public Task<SignInResult> SignIn(SignInRequest request) => this.service.SignIn(request);
```

The client sends `Captcha-Response` with the token, plus `Captcha-Type` and `Captcha-SiteKey`
describing what it solved. Those describe the challenge; the server decides which configured instance
verifies it.

> **Note.** A requirement the provider cannot answer fails rather than passing. reCAPTCHA v2 and
> Turnstile report no score, so naming any `Policy` tier rejects their tokens as `score_unavailable`;
> `Unspecified` is the only tier that accepts a provider without one. Declaring no tier and no actions
> is how an endpoint accepts anything genuine.

Full documentation at [projectapricot.dev](https://projectapricot.dev).
