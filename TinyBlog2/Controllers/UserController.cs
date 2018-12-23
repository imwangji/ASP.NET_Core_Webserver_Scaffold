using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TinyBlog2.Areas.Identity.Data;
using TinyBlog2.Model;

namespace TinyBlog2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserManager<TinyBlog2User> _userManager;
        private readonly IConfiguration _config;
        private readonly SignInManager<TinyBlog2User> _signInManager;
        public UserController(UserManager<TinyBlog2User> userManager, IConfiguration configuration, SignInManager<TinyBlog2User> signInManager)
        {
            _config = configuration;
            _signInManager = signInManager;
            _userManager = userManager;
        }
        [HttpPost]
        [Route("Reg")]
        public async Task<IActionResult> Post([FromBody] UserRegisterInput Input)
        {
            var user = new TinyBlog2User { UserName = Input.UserName, Email = Input.Email};
            var result = await _userManager.CreateAsync(user, Input.Password);
            //给用户增加一个Claim
            var addClaimResult = await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("Role", "DefaultUser"));
            if (result.Succeeded && addClaimResult.Succeeded)
            {
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
                //将加密后的密码用JWT指定算法进行加密
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                //拿到当前登录用户
                TinyBlog2User currentUser = await _userManager.FindByEmailAsync(Input.Email);
                //获取当前用户的Claims
                IList<Claim> claimsList = await _userManager.GetClaimsAsync(currentUser);
                var unSecruityToken = new JwtSecurityToken(_config["Jwt:Issuer"], _config["Jwt:Issuer"], claimsList, expires: DateTime.Now.AddMinutes(30), signingCredentials: creds);
                var token = new JwtSecurityTokenHandler().WriteToken(unSecruityToken);
                return Ok(new { user = user, token = token });
            }
            else
            {
                return BadRequest();
            }
        }

        /// <summary>
        /// 前后端分离，前端的登录请求发送到这里。
        /// 返回200或者401，代表登录成功和失败，如果登录成功，返回一个token。
        /// </summary>
        /// <param name="inputUser"></param>
        /// <returns>
        /// {"token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJlbWFpbCI6IjFAMS5jb20iLCJqdGkiOiI0ZDNiZGFjMC1hNjYzLTQwNTMtYjU1Yy02Njg2YjAyNjk0MmIiLCJFbWFpbCI6IjFAMS5jb20iLCJleHAiOjE1NDQxODgwMDcsImlzcyI6Imh0dHA6Ly9sb2NhbGhvc3Q6NjM5MzkvIiwiYXVkIjoiaHR0cDovL2xvY2FsaG9zdDo2MzkzOS8ifQ.GTFmUKiAfLTaOuv7rZ-g4Cns033RWehB8u3iFB59rFM"}
        /// </returns>
        [HttpPost]
        [Route("Login")]
        public async Task<IActionResult> Login([FromBody]UserLoginInput inputUser)
        {
            //拿到用户名和密码，用asp.net Core 自带的Identity来进行登录
            var result = await _signInManager.PasswordSignInAsync(inputUser.UserName, inputUser.Password, inputUser.RememberMe, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                //把你自己的密码进行对称加密
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
                //将加密后的密码用JWT指定算法进行加密，这个加密算法有很多，可以去JWT官网上看
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                //拿到当前登录用户
                TinyBlog2User user = await _userManager.FindByEmailAsync(inputUser.Email);
                //获取当前用户的Claims
                IList<Claim> claimsList = await _userManager.GetClaimsAsync(user);
                //用各种信息组成一个JWT
                var unSecruityToken = new JwtSecurityToken(_config["Jwt:Issuer"], _config["Jwt:Issuer"], claimsList, expires: DateTime.Now.AddMinutes(30), signingCredentials: creds);
                //把JWT加密一下返回给客户端
                var token = new JwtSecurityTokenHandler().WriteToken(unSecruityToken);
                return Ok(new { token = token });
            }
            else
            {
                return Unauthorized();
            }
        }



        [HttpGet]
        public ActionResult<IEnumerable<string>> Get()
        {
            return new string[] { "value1", "value2" };
        }
    }
}