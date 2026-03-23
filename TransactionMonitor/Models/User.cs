namespace TransactionMonitor.Models
{
    public enum UserRole
    {
        Admin,
        Analyst,
        Operator
    }

    public class User
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string FullName { get; set; } = "";
        public UserRole Role { get; set; }

        public string RoleDisplayName => Role switch
        {
            UserRole.Admin => "Администратор",
            UserRole.Analyst => "Аналитик",
            UserRole.Operator => "Оператор",
            _ => ""
        };
    }
}