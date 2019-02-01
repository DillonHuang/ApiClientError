[RFC7807]: https://tools.ietf.org/html/rfc7807#page-5
[ProblemDetails]: https://docs.microsoft.com/zh-cn/dotnet/api/microsoft.aspnetcore.mvc.problemdetails?view=aspnetcore-2.2
[ValidationProblemDetails]: https://docs.microsoft.com/zh-cn/dotnet/api/microsoft.aspnetcore.mvc.validationproblemdetails?view=aspnetcore-2.2
[InvalidModelStateResponseFactory]: https://docs.microsoft.com/zh-cn/dotnet/api/microsoft.aspnetcore.mvc.apibehavioroptions.invalidmodelstateresponsefactory?view=aspnetcore-2.2#Microsoft_AspNetCore_Mvc_ApiBehaviorOptions_InvalidModelStateResponseFactory
[ASP.NET Core 2.2 Web API]: https://docs.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-2.2
[BadReqeustResult]: https://docs.microsoft.com/zh-cn/dotnet/api/microsoft.aspnetcore.mvc.badrequestobjectresult?view=aspnetcore-2.2
[RESTful API]: https://restfulapi.net
[IMyProblemDetails]: /ApiClientError/IMyProblemDetails.cs
[IMyProblemDetailsActionResult]: /ApiClientError/IMyProblemDetailsActionResult.cs
[MyProblemDetailsClientErrorFactory]: /ApiClientError/MyProblemDetailsClientErrorFactory.cs

# 统一定义ASP.NET Core 2.2 Web API 理客户端调用错误响应
设计Web API的时候，对于客户端调用错误响应，都会定义统一的结构  
ASP.NET Core 2.2对Web API处理有不少的改变

* [使用ASP.NET Core构建Web API][ASP.NET Core 2.2 Web API]
* [RESTful API][RESTful API]

使用**ASP.NET Core 2.2 Web API**需要以下两点
1. 在Controller上加上`ApiController`特性
   ```csharp
   [Route("api/[controller]")]
   [ApiController]
   public class ProductsController : ControllerBase
   ```
2. 设置`CompatibilityVersion`
   ```csharp
   services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
   ```


# Web API 客户端调用错误来源主要分为以下三种
1. 数据模型验证错误
2. 业务验证错误
3. 未处理异常
```csharp
public class ValueDto
{
    [Required()] // 1. 数据模型错误
    public string No { get; set; }

    [MaxLength(10) ]// 1. 数据模型错误
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
            // 2.业务验证错误
            return this.BadRequest(new {errorMessage="编号已经存在"});
        }

        // value.Name=null时，会发生 3.未处理异常
        Console.WriteLine(value.Name.Length)
        return this.Ok(value);
    }
}

```
## 客户段错误的响应标准[RFC 7807规范][RFC7807]
从ASP.NET CORE 2.2开始，客户端错误的默认响应类型符合[RFC 7807规范][RFC7807]  
按[RFC 7807规范][RFC7807]定义接口[IMyProblemDetails][IMyProblemDetails]和 [ASP.NET CORE][ASP.NET Core 2.2 Web API]中的[ProblemDetails][ProblemDetails]结构是一样的 
```csharp
public interface IMyProblemDetails
{
    string Type { get; set; }

    string Title { get; set; }

    string Detail { get; set; }

    int? Status { get; set; }

    string Instance { get; set; }
    
    // 扩展属性
    [JsonExtensionData]
    IDictionary<string, object> Extensions { get; }    
}
```
## 标准定义的扩展
使用`IProblemDetails.Extensions`来扩展客户端错误响应的属性  
`Extensions`属性需要增加[JsonExtensionData](https://www.newtonsoft.com/json/help/html/T_Newtonsoft_Json_JsonExtensionDataAttribute.htm)特性使其扁平化

```csharp
problemDetails.Extensions.Add("extension1","value1");
problemDetails.Extensions.Add("extension2","value2");
```
`IMyProblemDetails.Extensions`序列化后呈**扁平**状
```json
{
    "title": "XXXXX",
    "status": 400,
    "extension1": "value1",
    "extension2": "value2"
}
```

## 客户错误处理工厂 IClientErrorFactory & IClientErrorActionResult
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
ASP.NET Core中，客户端请求通过`Controller`都会产生`IActionResult`  
如果返回的`IActionResult`实例也实现了`IClientErrorActionResult`接口，那么将会交给`IClientErrorFactory.GetClientError`处理，得到一个新的`IActionResult`，然后返回给客户端。  
只要在客户端调用发生错误的的地方，都返回`IClientErrorActionResult`的实现，客户端错误响应就会由`IClientErrorFactory`统一处理。  
这样为了统一处理，就需要实现`IClientErrorFactory`和`IClientErrorActionResult`   
为了让`IClientErrorFactory`能拿到[IMyProblemDetails][IMyProblemDetails]，需要把[IMyProblemDetails][IMyProblemDetails]放入`IClientErrorActionResult`，因此需要定义接口[IMyProblemDetailsActionResult][IMyProblemDetailsActionResult]继承`IClientErrorActionResult`
```csharp
public interface IMyProblemDetailsActionResult : IClientErrorActionResult
{
        IMyProblemDetails ProblemDetails { get; }
}
```
只要当发生客户端调用错误时都返回[IMyProblemDetailsActionResult][IMyProblemDetailsActionResult]，都将交给`IClientErrorFactory`统一处理。  

[MyProblemDetailsClientErrorFactory][MyProblemDetailsClientErrorFactory]是`IClientErrorFactory`的实现，用`MyProblemDetailsClientErrorFactory`处理`IMyProblemDetailsActionResult`接口

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
```csharp
services.TryAddSingleton<IClientErrorFactory, ProblemDetailsClientErrorFactory>();
```

## 自定义数据模型验证错误的响应
从ASP.NET Core 2.2开始, 模型验证错误的默认类型是[ValidationProblemDetails][ValidationProblemDetails]。该`ValidationProblemDetails`类型符合[RFC 7807规范][RFC7807]。  
我们要将默认类型改为接口[IMyProblemDetails][IMyProblemDetails]  
必须要使用`InvalidModelStateResponseFactory`自定义响应。

```csharp
 services.AddMvc()
            .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory =MyProblemDetailsClientErrorFactory.ProblemDetailsInvalidModelStateResponse;
            });

```
`MyProblemDetailsClientErrorFactory.ProblemDetailsInvalidModelStateResponse`的实现如下：
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

## 自定义业务验证错误的响应
检查到业务错误后，不能返回`this.BadReqeust(XXX)`,因为该方法返回的`BadRequestObjectResult`并没有实现IClientErrorActionResult
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
