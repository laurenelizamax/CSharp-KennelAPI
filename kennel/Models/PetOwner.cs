using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace kennel.Models
{
    public class PetOwner
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PetName { get; set; }
    }
}
