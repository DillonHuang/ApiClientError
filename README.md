[RFC7807]: https://tools.ietf.org/html/rfc7807#page-5
[ProblemDetails]: https://docs.microsoft.com/zh-cn/dotnet/api/microsoft.aspnetcore.mvc.problemdetails?view=aspnetcore-2.2
[ValidationProblemDetails]: https://docs.microsoft.com/zh-cn/dotnet/api/microsoft.aspnetcore.mvc.validationproblemdetails?view=aspnetcore-2.2
[InvalidModelStateResponseFactory]: https://docs.microsoft.com/zh-cn/dotnet/api/microsoft.aspnetcore.mvc.apibehavioroptions.invalidmodelstateresponsefactory?view=aspnetcore-2.2#Microsoft_AspNetCore_Mvc_ApiBehaviorOptions_InvalidModelStateResponseFactory
[ASP.NET Core 2.2 Web API]: https://docs.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-2.2
[BadReqeustResult]: https://docs.microsoft.com/zh-cn/dotnet/api/microsoft.aspnetcore.mvc.badrequestobjectresult?view=aspnetcore-2.2
[RESTful API]: https://restfulapi.net
[RESTful API Status Codes]: https://restfulapi.net
[IMyProblemDetails]: /ApiClientError/IMyProblemDetails.cs
# 如何自定义 [ASP.NET Core 2.2 Web API  RESTful Web API][ASP.NET Core 2.2 Web API]调用错误时的Response？
* [使用ASP.NET Core构建Web API][ASP.NET Core 2.2 Web API]
* [RESTful API][RESTful API]
* [RESTful API HTTP Status Codes][RESTful API Status Codes]

构建Web API时，调用发生错误时，响应的结构一般有统一的定义的。结构可能如下：
```json
{
    "errorCode":"XXXXX",
    "errorMessage":"YYYYYYYY"
}
```

# Web API 错误来源主要分为以下三种
1. 数据模型验证异常
2. 业务验证异常
3. 未处理异常
```csharp
public class ValueDto
{
    [Required()] // 1. 数据模型异常
    public string No { get; set; }

    [MaxLength(10) ]// 1. 数据模型异常
    public string Name { get; set; }
}

[Route("api/[controller]")]
[ApiController]
public class ValuesController : ControllerBase
{       
    [HttpPost]
    public IActionResult Post([FromBody] ValueDto value)
    {
        if (value.No == 1)
        {
            // 2.业务验证失败
            return this.BadRequest(new {errorMessage="编号已经存在"});
        }

        // value.Name=null时，会发生 3.未处理异常
        Console.WriteLine(value.Name.Length)
        return this.Ok(value);
    }
}

```
## HTTP 400响应的标准[RFC 7807规范][RFC7807]
从ASP.NET CORE 2.2开始，HTTP 400的默认响应类型符合[RFC 7807规范][RFC7807]

按[RFC 7807规范][RFC7807]定义响应类型接口如下
```csharp
public interface IMyProblemDetails
{
    string Type { get; set; }

    string Title { get; set; }

    string Detail { get; set; }

    int? Status { get; set; }

    string Instance { get; set; }

    IDictionary<string, object> Extensions { get; }    
}
```
`Extensions`是用于添加扩展属性的，并不属于[RFC 7807规范][RFC7807]  
接口[IMyProblemDetails][IMyProblemDetails]和 [ASP.NET CORE][ASP.NET Core 2.2 Web API]中的[ProblemDetails][ProblemDetails]结构是一样的  
创建类`MyProblemDetails`类实现接口`IMyProblemDetails`

## IClientErrorFactory & IClientErrorActionResult
```csharp
public interface IClientErrorFactory
{ 
    IActionResult GetClientError(ActionContext actionContext, IClientErrorActionResult clientError);
}

public interface IClientErrorActionResult : IStatusCodeActionResult
{
}

public interface IStatusCodeActionResult : IActionResult
{
    /// <summary>
    /// Gets or sets the HTTP status code.
    /// </summary>
    int? StatusCode { get; }
}
```
ASP.NET Core中，HTTP 400响应最终都是`IActionResult`的实现，如果实现了`IClientErrorActionResult`接口，都会调用`IClientErrorFactory.GetClientError`处理，得到一个新的`IActionResult`，然后返回给客户端。  
只要在发生异常的地方**1. 数据模型验证异常**、**2. 业务验证异常**、**3. 未处理异常**返回的ActionResult都实现了`IClientErrorActionResult`，这样错误响应就会由`IClientErrorFactory`统一处理。  
这样我们就需要实现`IClientErrorFactory`和`IClientErrorActionResult`
先定义个接口`IMyProblemDetailsActionResult`继承`IClientErrorActionResult`
```csharp
public interface IMyProblemDetailsActionResult : IClientErrorActionResult
{
        IMyProblemDetails ProblemDetails { get; }
}
```
再创建类`MyProblemDetailsActionResult`实现接口`IMyProblemDetails`，只要当发生异常时都返回`MyProblemDetailsActionResult`就会统一处理。  
`IClientErrorFactory`的实现`MyProblemDetailsClientErrorFactory`
```csharp
public class MyProblemDetailsClientErrorFactory : IClientErrorFactory
{
    private readonly ApiBehaviorOptions _options;

    public MyProblemDetailsClientErrorFactory(IOptions<ApiBehaviorOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public IActionResult GetClientError(ActionContext actionContext, IClientErrorActionResult clientError)
    {
        IMyProblemDetails problemDetails;
        if (clientError is IMyProblemDetailsActionResult problemDetailsActionResult)
        {
            problemDetails = problemDetailsActionResult.ProblemDetails;
        }
        else
        {
            problemDetails = new MyProblemDetails()
            {
                Status = clientError.StatusCode,
                Type = "about:blank",
            };
            if (clientError.StatusCode is int statusCode &&
            _options.ClientErrorMapping.TryGetValue(statusCode, out var errorData))
            {
                problemDetails.Title = errorData.Title;
                problemDetails.Type = errorData.Link;
            }
        }
        return MyProblemDetailsActionResult.GetActionResult(actionContext, problemDetails);
    }、
}
```

最后需要把`MyProblemDetailsClientErrorFactory`注入到Service中  
`services.TryAddSingleton<IClientErrorFactory, ProblemDetailsClientErrorFactory>();`

## 自定义数据模型验证异常的响应
[ASP.NET Core 2.2][ASP.NET Core 2.2]开始, HTTP 400响应的默认类型是[ValidationProblemDetails][ValidationProblemDetails]。该`ValidationProblemDetails`类型符合[RFC 7807规范][RFC7807]。  
我们要将HTTP 400响应的默认类型是[ValidationProblemDetails][ValidationProblemDetails]修改为接口[IMyProblemDetails][IMyProblemDetails]  
修改HTTP 400的默认响应,需要使用`InvalidModelStateResponseFactory`自定义生成的响应的输出。

```csharp
 services.AddMvc()
            .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory =MyProblemDetailsClientErrorFactory.ProblemDetailsInvalidModelStateResponse;
            });

```
MyProblemDetailsClientErrorFactory.ProblemDetailsInvalidModelStateResponse

```csharp
public static IMyProblemDetailsActionResult ProblemDetailsInvalidModelStateResponse(ActionContext actionContext)
        {
            IMyProblemDetails problemDetails = new MyValidationProblemDetails(actionContext.ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
            };
            return new MyProblemDetailsActionResult(problemDetails);
        }
```
`MyValidationProblemDetails`的结构和ASP.NET Core中的[ValidationProblemDetails][ValidationProblemDetails]一样

## 自定义业务验证异常的响应
检查到业务异常后，不能调用`this.BadReqeust(XXX)`,因为该方法返回的`BadRequestObjectResult`并没有实现IClientErrorActionResult
```csharp
[HttpPost]
public IActionResult Post([FromBody] ValueDto value)
{
    if (value.Id.HasValue)
    {
        var problemDetails = (new MyProblemDetails()
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "ID不可输入",
            Type = $"http://xxxxx/value/post/IdForbidden",
            Detail = "创建资源时，系统会自动生成ID，请不要输入ID。"
        });
        return new MyProblemDetailsActionResult(problemDetails);
    }
    return this.Ok(value);
}
```

## 自定未处理异常的响应
要拦截未处理异常需要实现`IExceptionFilter`
```csharp
public class MyExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var exception = context.Exception;
        var loggerFactory = (ILoggerFactory)context.HttpContext.RequestServices.GetRequiredService(typeof(ILoggerFactory));
        var logging = loggerFactory.CreateLogger<MyExceptionFilter>();
        logging.LogCritical(exception, "未处理异常");
        var hostEnviroment = (IHostingEnvironment)context.HttpContext.RequestServices.GetRequiredService(typeof(IHostingEnvironment));
        var problemDetails = new MyProblemDetails()
        {
            Type = $"http://XXXXX/unhandledexception/{exception.GetType()}",
            Title = exception.Message,
            Status = StatusCodes.Status500InternalServerError
        };

        if (!hostEnviroment.IsProduction())
        {
            problemDetails.Detail = exception.ToString();
        }
        context.Result = new MyProblemDetailsActionResult(problemDetails);
    }
}
```
然后把`MyExceptionFilter`加到MVC的Filters中
```csharp
    services.AddMvc(opts =>
    {
        opts.Filters.Add<MyExceptionFilter>();
    });
```

# 对客户端错误增加扩展属性
客户端错误往往没有按[RFC 7807规范][RFC7807]来设计。
通常可能设计为
```json
{
    "errorCode":"XXXXXX",
    "errorMessage":"YYYYYYYYYYYYYY"
}
```
增加这两个属性到`IMyProblemDetails.Extensions`中
```csharp
public class MyProblemDetailsActionResult :IMyProblemDetailsActionResult
{
    public static IActionResult GetActionResult(ActionContext actionContext,IMyProblemDetails problemDetails)
        {
            problemDetails.Extensions.Add("errorCode", problemDetails.Type);
            problemDetails.Extensions.Add("errorMessage", problemDetails.Title);
            problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? actionContext.HttpContext.TraceIdentifier;
            var actionResult = new ObjectResult(problemDetails)
            {
                StatusCode = problemDetails.Status??StatusCodes.Status400BadRequest,
                ContentTypes =
                {
                    "application/problem+json",
                    "application/problem+xml",
                },
            };
            return actionResult;
        }
}
```
最终响应结果如下:
```json
{
   "type":"AAAAA",
   "title":"BBBBB",
   "errorCode":"CCCCC",
   "errorMessage":"DDDDD",
   "traceId":"EEEEE"
}
```
