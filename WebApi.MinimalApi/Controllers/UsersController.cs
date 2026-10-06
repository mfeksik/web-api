using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
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
    private readonly LinkGenerator _linkGenerator;

    private const int DefaultPageNumber = 1;
    private const int DefaultPageSize = 10;
    private const int MinPageNumber = 1;
    private const int MinPageSize = 1;
    private const int MaxPageSize = 20;

    public UsersController(IUserRepository userRepository, AutoMapper.IMapper mapper, LinkGenerator linkGenerator)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _linkGenerator = linkGenerator;
    }

    [HttpGet("{userId}")]
    [Produces("application/json", "application/xml")]
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
    public IActionResult CreateUser([FromBody] object user)
    {
        throw new NotImplementedException();
    }
    
    /// <summary>
    /// Удалить запись о пользователе.
    /// </summary>
    /// <param name="userId">Id пользователя</param>
    /// <returns></returns>
    [HttpDelete("{userId}")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        var user = _userRepository.FindById(userId);
        if (user is null)
            return NotFound();
        
        _userRepository.Delete(userId);
        return NoContent();
    }

    /// <summary>
    /// Проверить наличие пользователя.
    /// </summary>
    /// <param name="userId">Id пользователя</param>
    /// <returns></returns>
    [HttpHead("{userId}")]
    public IActionResult HeadUserById([FromRoute] Guid userId)
    {
        var user = _userRepository.FindById(userId);
        if (user is null)
            return NotFound();

        Response.ContentType = "application/json; charset=utf-8";
        return Ok();
    }
    
    /// <summary>
    /// Получить список пользователей с пагинацией.
    /// </summary>
    /// <param name="pageNumber">Номер возвращаемой страницы</param>
    /// <param name="pageSize">Размер возвращаемой страницы</param>
    /// <returns></returns>
    [HttpGet(Name = nameof(GetUsers))]
    public IActionResult GetUsers(
        [FromQuery] int pageNumber = DefaultPageNumber,
        [FromQuery] int pageSize = DefaultPageSize)
    {
        pageNumber = Math.Max(pageNumber, MinPageNumber);
        pageSize = Math.Clamp(pageSize, MinPageSize, MaxPageSize);

        var pageList = _userRepository.GetPage(pageNumber, pageSize);

        var previousPageLink = pageList.HasPrevious
            ? _linkGenerator.GetUriByRouteValues(
                HttpContext,
                nameof(GetUsers),
                new { pageNumber = pageNumber - 1, pageSize })
            : null;
        var nextPageLink = pageList.HasNext
            ? _linkGenerator.GetUriByRouteValues(
                HttpContext,
                nameof(GetUsers),
                new { pageNumber = pageNumber + 1, pageSize })
            : null;

        var paginationHeader = new
        {
            previousPageLink,
            nextPageLink,
            totalCount = pageList.TotalCount,
            pageSize = pageList.PageSize,
            currentPage = pageList.CurrentPage,
            totalPages = pageList.TotalPages
        };
        Response.Headers.Append("X-Pagination", JsonConvert.SerializeObject(paginationHeader));

        var users = _mapper.Map<IEnumerable<UserDto>>(pageList);
        return Ok(users);
    }

    /// <summary>
    /// Получить список методов, доступных в контексте коллекции пользователей.
    /// </summary>
    [HttpOptions]
    public IActionResult GetUserOptions()
    {
        Response.Headers.AppendCommaSeparatedValues(
            HeaderNames.Allow,
            HttpMethods.Get, HttpMethods.Post, HttpMethods.Options
        );

        return Ok();
    }
}