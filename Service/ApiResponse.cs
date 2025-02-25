using SWD392.Service;

namespace SWD392.Service
{
    /*
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        var loginResponse = AuthenticateUser(request); // Replace with your logic to authenticate the user

        if (loginResponse != null)
        {
            return Ok(ApiResponse<object>.Success(loginResponse));
        }
        else
        {
            return BadRequest(ApiResponse<object>.Error("Invalid username or password."));
        }
    }

     */
    public class ApiResponse<T>
    {
        public string? Status { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
        public Pagination? Pagination { get; set; }

        // Static helper methods for convenience
        public static ApiResponse<T> Success(T data, Pagination? pagination = null, string message = "")
        {
            return new ApiResponse<T>
            {
                Status = "successful",
                Message = message,
                Pagination = pagination,
                Data = data
            };
        }

        public static ApiResponse<T> Error(string message)
        {
            return new ApiResponse<T>
            {
                Status = "error",
                Message = message,
                Data = default
            };
        }
    }

    // Default type of object if T is not defined
    public class ApiResponse : ApiResponse<object>
    {
    }
}


