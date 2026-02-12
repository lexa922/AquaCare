using AquaClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AquaCareClasses
{
    public static class UserSession
    {
        public static User CurrentUser { get; private set; }
        public static string UserRole { get; private set; }
        public static string CurrentConnectionString { get; private set; }

        public static void Login(User user, string role, string connectionString)
        {
            CurrentUser = user;
            UserRole = role;
            CurrentConnectionString = connectionString;
        }
        public static void Logout()
        {
            CurrentUser = null;
            UserRole = null;
            CurrentConnectionString = null;
        }
    }
}
