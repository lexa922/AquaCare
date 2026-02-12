using AquaCareClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AquaClasses
{
   public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public ICollection<UserRole> UserRoles { get; set; }
        public ICollection<Aquarium> Aquariums { get; set; }
    }
}
