using System;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserServices.Data;
using UserServices.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;

namespace UserServices.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly UserContext _userContext;
        public UserController(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager, IConfiguration configuration, UserContext usercontext)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _userContext = usercontext ?? throw new ArgumentNullException(nameof(usercontext));
        }
        [HttpGet]
        [Route(nameof(GetUsers))]
        [ProducesResponseType(typeof(DynamicObject), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetUsers([FromQuery]int pageSize = 10, [FromQuery]int pageIndex = 0)
        {
            try
            {
                dynamic result = new ExpandoObject();
                result.Users = await _userContext.Users
                    .OrderBy(u => u.UserName)
                    .Skip(pageSize * pageIndex)
                    .Take(pageSize)
                    .ToListAsync();
                result.UserCount = await _userContext.Users.LongCountAsync();                
                return Ok(result);
            }
            catch (Exception)
            {
                return BadRequest(new { Message = $"Desculpe, algo deu errado." });
            }
        }
        [HttpGet]
        [Route(nameof(GetUserById))]
        [ProducesResponseType(typeof(User),(int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetUserById([FromQuery] string id)
        {
            try
            {
                var user = await _userContext.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Id == id);
                return user == null ? NotFound(new { Message = $"Usuário com o Id {id} não encontrado!" }) : (IActionResult)Ok(user);
            }
            catch (Exception)
            {
                return BadRequest(new { Message = "Desculpe, algo deu errado." });
            }
        }
        [HttpPost]
        [Route(nameof(Register))]        
        public async Task<ActionResult<UserToken>> Register([FromBody]User model)
        {            
            try
            {
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    return BuildToken(model);
                }
                else
                {
                    return BadRequest("Usuário ou senha inválidos");
                }                
            }
            catch (Exception e)
            {
                return BadRequest(new { Message = "Desculpe, algo deu errado." });
            }
        }
        [HttpPost]
        [Route(nameof(Login))]
        public async Task<ActionResult<UserToken>> Login([FromBody] User userInfo)
        {
            var result = await _signInManager.PasswordSignInAsync(userInfo.Email, userInfo.Password,
                 isPersistent: false, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                return BuildToken(userInfo);
            }
            else
            {
                ModelState.AddModelError(string.Empty, "login inválido.");
                return BadRequest(ModelState);
            }
        }
        [HttpPut]
        [Route(nameof(Update))]
        public async Task<IActionResult> Update([FromBody] User updatedUser)
        {
            try
            {
                var user = await _userContext.Users.SingleOrDefaultAsync(user => user.Id == updatedUser.Id);
                if (user == null)
                    return NotFound(new { Message = $"Usuário não encontrado." });
                user.Email = updatedUser.Email;
                user.NormalizedEmail = updatedUser.Email.ToUpper().Normalize();
                await _userContext.SaveChangesAsync();
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(new { Message = "Desculpe, algo deu errado." });
            }
        }
        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpPut]
        [Route(nameof(UpdatePassword))]        
        public async Task<IActionResult> UpdatePassword([FromBody] User updatedUser)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return NotFound(new { Message = $"Usuário não encontrado." });
                var passwordValidator = new PasswordValidator<IdentityUser>();
                var result = await passwordValidator.ValidateAsync(null, user, updatedUser.Password);
                if (!result.Succeeded)
                {
                    return BadRequest(new { Message = "Senha inválida" });
                }                
                user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, updatedUser.Password);                
                await _userContext.SaveChangesAsync();
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(new { Message = "Desculpe, algo deu errado." });
            }
        }
        [HttpPut]
        [Route(nameof(ForgotPassword))]
        public async Task<ActionResult<string>> ForgotPassword([FromBody] User updatedUser)
        {
            try
            {
                var user = await _userManager.Users.SingleOrDefaultAsync(user => user.Email == updatedUser.Email);
                if (user == null)
                    return NotFound(new { Message = $"Usuário não encontrado." });
                Random generator = new Random();
                String newPassword = generator.Next(0, 999999).ToString("D6");
                user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, newPassword);
                await _userContext.SaveChangesAsync();
                return newPassword;
            }
            catch (Exception e)
            {
                return BadRequest(new { Message = "Desculpe, algo deu errado." });
            }
        }
        [HttpDelete]
        [Route(nameof(Delete))]
        public async Task<IActionResult> Delete([FromQuery] string id)
        {
            try
            {               
                var user = await _userContext.Users.SingleOrDefaultAsync(user => user.Id == id);
                if (user == null)
                    return NotFound(new { Message = $"Usuário não encontrado." });
                _userContext.Users.Remove(user);              
                await _userContext.SaveChangesAsync();
                return Ok();
            }
            catch (Exception)
            {
                return BadRequest(new { Message = "Desculpe, algo deu errado." });
            }
        }
        private UserToken BuildToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(Settings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Email, user.Email.ToString())
                   
                }),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return new UserToken()
            {
                Token = tokenHandler.WriteToken(token),
                Expiration = DateTime.UtcNow.AddHours(2)
            };
            
        }
    }
}