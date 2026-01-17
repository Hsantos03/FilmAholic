using FilmAholic.Server.Data;
using Microsoft.AspNetCore.Mvc;

namespace FilmAholic.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FilmesController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(FilmSeed.Filmes);
        }

        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var filme = FilmSeed.Filmes.FirstOrDefault(f => f.Id == id);
            if (filme == null) return NotFound();
            return Ok(filme);
        }
    }
}
