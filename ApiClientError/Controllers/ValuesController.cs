using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ApiClientError.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {

        public ValuesController()
        {
        }

        // GET api/values
        [HttpGet]
        public ActionResult<IEnumerable<string>> Get()
        {

            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            if (id == 0)
            {
                return this.Problem(new MyProblemDetails()
                {
                    Status = StatusCodes.Status400BadRequest,
                });
            }
            else if (id == 1)
            {
                throw new Exception($"error:{id}");
            }

            return this.Ok("value");
        }

        // POST api/values
        [HttpPost]
        public IActionResult Post([FromBody] ValueDto value)
        {
            if (value.Id.HasValue)
            {
                return this.Problem(new MyProblemDetails() {
                     Status =StatusCodes.Status400BadRequest,
                     Title="ID不可输入",
                     Type=$"http://xxxxx/value/post/IdForbidden",
                     Detail="创建资源时，系统会自动生成ID，请不要输入ID。"

                });
            }

            if (!long.TryParse(value.No, out var noValue))
            {
                throw new Exception($"编号必须是数值");
            }
            value.Id = new Random().Next(1, 1000);
            return this.Ok(value);

        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] ValueDto value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
