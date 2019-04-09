# AGE.Extensions.Logging.Http

This package is intended for structured logging in different targets that can have basic or certificate base authorization by implementing a Logger Provider.
### Getting Started

Please go through the following instructions to add Provider to your project.


### Installing

Install the following package from [NuGet](https://www.nuget.org/packages/AGE.Extensions.Logging.Http/)

```
Install-Package AGE.Extensions.Logging.Http	

```

Then follow the instructions from Configuration and Usage sections below.

### Configuration

Add the following configuration section to your **appsettings.json**:
```
{
	"Logging":
	...
		"HttpWithCertificate": {
			"Url": "urlToLogService",
			"CertificatePath": "pathToPrivateCertificate",
			"CertificatePassword": "certPassword",
			"UseScopeMappings": true, // if true log only properties from "ScopeMappings" if are present in logs
			"ScopeMappings": {
				"Test": "test"
			}
		},
		"HttpWithBasicAuth": {
			"Url": "urlToLogService",
			"Username": "",
			"Password": "",
			"UseScopeMappings": false, // if false log all properties
			"ScopeMappings": {
				"Test": "test"
			}
		}
	...
}
```

### Usage

Add the following code snippet to your **Startup.ConfigureServices** method:
```
public void ConfigureServices(IServiceCollection services)
{
...
	services.AddLogging(builder => builder
                   .AddConfiguration(Configuration)
                   .AddHttp(() =>
                   {
                       var authwithCertOptions = new HttpOptions { ErrorLogger = mlogErrorLogger };
                       Configuration.Bind("Logging:HttpWithCertificate", authwithCertOptions);
                       return authwithCertOptions;
                   })
                   .AddHttp(() =>

                   {
                       var authwithAuthHeaderOptions = new HttpOptions();
                       Configuration.Bind("Logging:HttpWithBasicAuth", authwithAuthHeaderOptions);
                       return authwithAuthHeaderOptions;
                   }));
}
```
```

using (logger.BeginScope("{event_type}{service}{event_time}", "Signature.Success", "testService", DateTime.UtcNow))
{
	logger.LogError("Logging with scope => {Test}", "scope");
}
logger.LogError("Took place an error with event type: {event_type} with data: {test}", "ERROR", "test");
                        
```
