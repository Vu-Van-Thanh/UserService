using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace EmployeeService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DynamicController : ControllerBase
    {
        private readonly string _connectionString;
        public DynamicController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // Endpoint để thực thi câu lệnh SQL động
        [HttpPost("execute-query")]
        public IActionResult ExecuteQuery([FromBody] string sqlQuery)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var result = connection.Query(sqlQuery); // Dùng Dapper để thực thi câu SQL
                    return Ok(result);
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Có lỗi xảy ra: {ex.Message}");
            }
        }
    }
}
