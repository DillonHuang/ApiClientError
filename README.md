# ASP.Net Core 2.2 RESTful Web API发出错误时，自定义Response。
[使用ASP.NET Core构建Web API](https://docs.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-2.2)
关于[RESTful API HTTP Status Codes](https://restfulapi.net/http-status-codes/)
Web API调用正常时StatusCode=**200**段，出错时StatusCode=**400**段和**500**段

# Web API 错误来源分为以下三种
1. 数据模型验证失败
2. 业务验证失败
3. 未处理异常
