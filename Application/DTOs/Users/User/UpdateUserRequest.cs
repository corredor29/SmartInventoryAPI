namespace Application.DTOs.Users.User
{
    public class UpdateUserRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int RoleId { get; set; }
        /// <summary>Opcional: si viene vacío no se cambia la contraseña.</summary>
        public string? Password { get; set; }
    }
}
