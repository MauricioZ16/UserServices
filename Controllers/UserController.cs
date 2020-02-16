using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserServices.Data;
using UserServices.Model;

namespace UserServices.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserContext _userContext;
        public UserController(UserContext usercontext)
        {
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
                    .OrderBy(u => u.Name)
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
        public async Task<IActionResult> GetUserById([FromQuery] Guid id)
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
        public async Task<IActionResult> Register([FromBody]User user)
        {
            try
            {
                _userContext.Add(user);
                await _userContext.SaveChangesAsync();
                return Ok();
            }
            catch (Exception)
            {
                return BadRequest(new { Message = "Desculpe, algo deu errado." });
            }
        }
        [HttpPut]
        [Route(nameof(Update))]       
        public async Task<IActionResult> Update([FromBody] User updatedUser)
        {
            try
            {
                var user = await _userContext.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Id == updatedUser.Id);
                if (user == null)
                    return NotFound(new { Message = $"Usuário não encontrado." });                
                _userContext.Users.Update(updatedUser);
                _userContext.Entry(updatedUser).Property(user => user.Password).IsModified = false;
                await _userContext.SaveChangesAsync();
                return Ok();
            }
            catch (Exception)
            {
                return BadRequest(new { Message="Desculpe, algo deu errado." });
            }
        }
        [HttpDelete]
        [Route(nameof(Delete))]
        public async Task<IActionResult> Delete([FromQuery] Guid id)
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

    }
}