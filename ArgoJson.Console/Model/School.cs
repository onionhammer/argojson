using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArgoJson.Console.Model
{
    [DebuggerDisplay("School: {Name}, Id: {Id}")]
    public class School
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public List<Employee> Staff { get; set; }

        public List<Student> Students { get; set; }
    }
}