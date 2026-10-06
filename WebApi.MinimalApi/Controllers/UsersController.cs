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
        // return user == null
        //     ? NotFound()
        //     : Ok(new UserDto
        //     {
        //         FullName = $"{user.LastName} {user.FirstName}", Id = user.Id, CurrentGameId = user.CurrentGameId, GamesPlayed = user.GamesPlayed,
        //         Login = user.Login
        //     });
        return user == null ? NotFound() : Ok(_mapper.Map<UserDto>(user));
    }

    [HttpPost]
    public IActionResult CreateUser([FromBody] CreateUserDto user)
    {
        if (user == null)
        {
            return BadRequest();
        }

        if (user.Login != null && !user.Login.All(char.IsLetterOrDigit))
            ModelState.AddModelError(nameof(user.Login), "Логин должен состоять из цифр или букв!");

        if (ModelState.IsValid)
        {
            var createdUserEntity = _userRepository.Insert(_mapper.Map<UserEntity>(user));
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = createdUserEntity.Id },
                _mapper.Map<UserDto>(createdUserEntity));
        }

        return UnprocessableEntity(ModelState);
    }
}