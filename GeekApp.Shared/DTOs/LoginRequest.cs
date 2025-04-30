using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GeekApp.Shared.DTOs
{
    public class LoginRequest
    {
        [JsonPropertyName("email")]
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email.")]
        public string Email { get; set; } = "";

        [JsonPropertyName("password")]
        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; } = "";
    }
}