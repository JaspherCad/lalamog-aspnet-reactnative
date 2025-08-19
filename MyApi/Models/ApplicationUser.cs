using Microsoft.AspNetCore.Identity;

namespace MyApi.Models
{ 
    public class ApplicationUser : IdentityUser<Guid> //EXTENDS to built in IdentityUser<Guid>
    //behind the scenes, there's a lot of other properties and methods that IdentityUser provides... di ko lang alam kung ano mga iyon but that's it.
    {
        public string? FullName { get; set; }
        public DateTime? BirthDate { get; set; }
        
        // One-to-one
        public Profile? Profile { get; set; }
    }
}
