using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ApiClientError.Controllers
{
    public class ValueDto
    {
        public int? Id { get; set; }

        [Display(Name = "编号")]
        [Required(ErrorMessage = "{0}必须输入")]
        [StringLength(8, ErrorMessage = "{0}必须是{1}位", MinimumLength = 8)]
        public string No { get; set; }

        [Required(ErrorMessage = "{0}必须输入")]
        [MaxLength(10, ErrorMessage = "{0}最大长度为{1}")]
        [Display(Name = "名称")]
        public string Name { get; set; }
    }
}
