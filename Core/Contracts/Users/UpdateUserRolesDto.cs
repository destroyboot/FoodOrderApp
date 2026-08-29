using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Contracts.Users
{
    public class UpdateUserRolesDto
    {
        public List<string> Roles { get; set; } = new();
    }
}
