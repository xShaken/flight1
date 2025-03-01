using Microsoft.AspNetCore.Identity;

namespace flight.Models
{
    public class Users : IdentityUser
    {
        public string FullName { get; set; }
       
    }   
}
