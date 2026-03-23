using System.Linq;
using System.Collections.Generic;
using TransactionMonitor.Models;

namespace TransactionMonitor.Services
{
    public static class SessionService
    {
        public static User? CurrentUser { get; private set; }

        public static bool IsLoggedIn => CurrentUser != null;

        // Права доступа
        public static bool CanCreate => CurrentUser?.Role != UserRole.Operator;
        public static bool CanDelete => CurrentUser?.Role == UserRole.Admin;
        public static bool CanExport => CurrentUser?.Role != UserRole.Operator;
        public static bool CanViewRisks => true;
        public static bool CanUseCalculator => true;

        private static readonly List<User> _users = new()
        {
            new User
            {
                Username = "admin",
                Password = "admin123",
                FullName = "Иванов Администратор",
                Role = UserRole.Admin
            },
            new User
            {
                Username = "analyst",
                Password = "analyst123",
                FullName = "Петрова Аналитик",
                Role = UserRole.Analyst
            },
            new User
            {
                Username = "operator",
                Password = "operator123",
                FullName = "Сидоров Оператор",
                Role = UserRole.Operator
            }
        };

        public static bool Login(string username, string password)
        {
            var user = _users.FirstOrDefault(u =>
                u.Username == username && u.Password == password);

            if (user != null)
            {
                CurrentUser = user;
                return true;
            }
            return false;
        }

        public static void Logout()
        {
            CurrentUser = null;
        }
    }
}