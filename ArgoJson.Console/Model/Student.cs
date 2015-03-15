using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ArgoJson.Console.Model
{
    [DebuggerDisplay("Student: {FirstName} {LastName}, Id: {Id}")]
    public class Student : Person
    {
        public DateTime DateStarted { get; set; }

        public DateTime? DateGraduated { get; set; }
    }
}
