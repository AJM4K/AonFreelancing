using AonFreelancing.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AonFreelancing.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class ProjectsController : ControllerBase
    {
        private static List<Project> projectsList = new List<Project>();
        [HttpGet]
        public IActionResult GetAll()
        {
            
            return Ok(projectsList);
        }

        [HttpPost]
        public IActionResult Create([FromBody] Project project) {
            projectsList.Add(project);
            return CreatedAtAction("Create", new { Id = project.Id }, projectsList);
        }

        [HttpGet("{id}")]
        public IActionResult GetProject(int id)
        {
            Project fr = projectsList.FirstOrDefault(f => f.Id == id);

            if (fr == null)
            {
                return NotFound("The resoucre is not found!");
            }

            return Ok(fr);
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
           // Project f = projectsList.FirstOrDefault(f=>f.Id == id);
            bool d = projectsList.Exists(f => f.Id == id);

            if (d)
            {
                projectsList.Remove(f);
                return Ok("Deleted");

            }

            return NotFound();
        }



    }
}
