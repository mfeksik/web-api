using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : Controller
{
    // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public UsersController(IUserRepository userRepository, AutoMapper.IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    [Produces("application/json", "application/xml")]
    [HttpGet("{userId}", Name = nameof(GetUserById))]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = _userRepository.FindById(userId);
        return user == null ? NotFound() : Ok(_mapper.Map<UserDto>(user));
    }

    [HttpPost]
    [Produces("application/json", "application/xml")]
    public IActionResult CreateUser([FromBody] CreateUserDto user)
    {
        if (user == null)
            return BadRequest();

        if (user.Login != null && !user.Login.All(char.IsLetterOrDigit))
            ModelState.AddModelError(nameof(user.Login), "Login should contain only letters or digits");

        if (ModelState.IsValid)
        {
            var createdUserEntity = _userRepository.Insert(_mapper.Map<UserEntity>(user));
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = createdUserEntity.Id },
                createdUserEntity.Id);
        }

        return UnprocessableEntity(ModelState);
    }

    
    [HttpPut("{userId}")]
    [Produces("application/json", "application/xml")]

    public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UpdateUserDto? userDto)
    {
        if (userDto == null || userId == Guid.Empty)
            return BadRequest();
        
        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }
        
        var user = _mapper.Map(userDto, new UserEntity(userId));
        _userRepository.UpdateOrInsert(user, out var isInserted);
        
        if (isInserted)
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = user.Id },
                user.Id);
        return NoContent();
    }
}