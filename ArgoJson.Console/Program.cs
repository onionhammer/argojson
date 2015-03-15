using ArgoJson.Console.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ArgoJson.Console
{
    class Program
    {
        delegate void Bench(ICollection<School> schools);

        #region Test Data Generators

        static IEnumerable<Employee> GetEmployees(Random rand) 
        {
            for (int i = 0, numEmployees = rand.Next(50, 100); i < numEmployees; ++i)
                yield return new Employee
                {
                    Id          = rand.NextGuid(),
                    FirstName   = rand.NextString(5, 10),
                    LastName    = rand.NextString(8, 12),
                    DateLeft    = rand.NextNullableDateTime(.2f),
                    DateStarted = rand.NextDateTime(),
                    Email       = rand.NextString(8, 18)
                };
        }


        private static IEnumerable<Student> GetStudents(Random rand)
        {
            for (int i = 0, numStudents = rand.Next(500, 3000); i < numStudents; ++i)
                yield return new Student
                {
                    Id            = rand.NextGuid(),
                    FirstName     = rand.NextString(5, 10),
                    LastName      = rand.NextString(8, 12),
                    DateGraduated = rand.NextNullableDateTime(.8f),
                    DateStarted   = rand.NextDateTime(),
                    Email         = rand.NextString(8, 18)
                };
        }

        static IEnumerable<School> GetSchools()
        {
            const int SEED = 0x1e5;
            var rand = new Random(SEED);

            for (int i = 0, numSchools = rand.Next(200, 400); i < numSchools; ++i)
                yield return new School
                {
                    Id       = rand.NextGuid(),
                    Name     = rand.NextString(8, 15),
                    Staff    = GetEmployees(rand).ToList(),
                    Students = GetStudents(rand).ToList()
                };
        }

        #endregion

        #region ArgoJson

        static void BenchArgoJsonLarge(ICollection<School> schools)
        {
            ArgoJson.Serializer.Serialize(schools);
            //File.WriteAllText("output.dat", ArgoJson.Serializer.Serialize(schools));
        }

        static void BenchArgoJsonSmall(ICollection<School> schools)
        {
            foreach (var school in schools)
            foreach (var student in school.Students)
            {
                ArgoJson.Serializer.Serialize(student);
            }
        }

        #endregion

        #region Newtonsoft Json.NET

        static void BenchJSONDotNetLarge(ICollection<School> schools)
        {
            Newtonsoft.Json.JsonConvert.SerializeObject(schools);
        }

        static void BenchJSONDotNetSmall(ICollection<School> schools)
        {
            foreach (var school in schools)
            foreach (var student in school.Students)
            {
                Newtonsoft.Json.JsonConvert.SerializeObject(student);
            }
        }

        #endregion

        #region Microsoft DataContract Serializer

        const int MAX_SIZE_GB = int.MaxValue;

        static void BenchDataContractLarge(ICollection<School> schools)
        {
            var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            serializer.MaxJsonLength = MAX_SIZE_GB;

            serializer.Serialize(schools);
        }

        static void BenchDataContractSmall(ICollection<School> schools)
        {
            var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            serializer.MaxJsonLength = MAX_SIZE_GB;

            foreach (var school in schools)
            foreach (var student in school.Students)
            {
                serializer.Serialize(student);
            }
        }

        #endregion

        #region ServiceStack Serializer

        static void BenchServiceStackLarge(ICollection<School> schools)
        {
            ServiceStack.Text.JsonSerializer.SerializeToString(schools);
        }

        static void BenchServiceStackSmall(ICollection<School> schools)
        {
            foreach (var school in schools)
            foreach (var student in school.Students)
            {
                ServiceStack.Text.JsonSerializer.SerializeToString(student);
            }
        }

        #endregion

        static void Main(string[] args)
        {
            System.Console.Write("Generating data... ");
            var allSchools = GetSchools().ToList();
            System.Console.WriteLine("Done.");
            
            // Serialize all data with JSON.NET
            Bench[] operations = {
                BenchArgoJsonLarge,
                BenchArgoJsonSmall,
                BenchJSONDotNetLarge,
                BenchJSONDotNetSmall,
                BenchDataContractLarge,
                BenchDataContractSmall,
                BenchServiceStackLarge,
                BenchServiceStackSmall
            };

            for (var i = 0; i < operations.Length; ++i)
            {
                var operationName = operations[i].Method.Name;
                System.Console.Write("Benching {0}... ", operationName);
                var stopwatch = new Stopwatch();
                {
                    stopwatch.Start();
                    operations[i](allSchools);
                    stopwatch.Stop();
                }
                System.Console.WriteLine("{0}s", stopwatch.ElapsedMilliseconds / 1000.0);
            }
        }
    }
}