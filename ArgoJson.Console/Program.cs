using ArgoJson.Console.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ArgoJson.Console
{
    class Program
    {
        #region Delegates

        delegate void Bench(ICollection<School> schools);

        #endregion

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

            for (int i = 0, numSchools =  rand.Next(200, 400); i < numSchools; ++i)
                yield return new School
                {
                    Id       = rand.NextGuid(),
                    Name     = rand.NextString(8, 15),
                    Staff    = GetEmployees(rand).ToArray(),
                    Students = GetStudents(rand).ToList()
                };
        }

        static string GetRandomSerialized()
        {
            const int SEED = 0x1e5;
            const int LENGTH = 10000;

            var rand = new Random(SEED);
            var testItems = new List<TestItem>(capacity: LENGTH);
            for (int i = 0; i < LENGTH; ++i)
            {
                testItems.Add(new TestItem
                {
                    Id        = rand.NextGuid(),
                    Graduated = rand.NextDateTime(),
                    Name      = rand.NextString(4, 14),
                    Checkins  = new[] { rand.NextString(2, 10), rand.NextString(2, 10) }
                });
            }

            return ArgoJson.Serializer.Serialize(testItems);
        }

        #endregion

        #region ArgoJson

        static void BenchArgoJsonLarge(ICollection<School> schools)
        {
            ArgoJson.Serializer.Serialize(schools);
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

        #region fastJSON

        static void BenchFastJsonLarge(ICollection<School> schools)
        {
            fastJSON.JSON.ToJSON(schools);
        }

        static void BenchFastJsonSmall(ICollection<School> schools)
        {
            foreach (var school in schools)
            foreach (var student in school.Students)
            {
                fastJSON.JSON.ToJSON(student);
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

        #region Deserializers

        private static string Data;

        static void BenchArgoJsonDeserialize(ICollection<School> schools)
        {
            // Deserialize data
            var deserialized = ArgoJson.Deserializer.DeserializeTests(Data);
        }

        static void BenchJSONDotNetDeserialize(ICollection<School> schools)
        {
            // Deserialize data
            var deserialized = Newtonsoft.Json.JsonConvert.DeserializeObject<ICollection<School>>(Data);
        }

        #endregion

        static void Main(string[] args)
        {
            // Generating deserliazation data
            Data = GetRandomSerialized();

            System.Console.Write("Generating data... ");
            var allSchools = GetSchools().ToList();
            System.Console.WriteLine("Done.");
            
            // Serialize all data with JSON.NET
            Bench[] operations = {
                BenchJSONDotNetLarge,
                BenchJSONDotNetSmall,
                BenchArgoJsonLarge,
                BenchArgoJsonSmall,
                BenchServiceStackLarge,
                BenchServiceStackSmall,
                BenchFastJsonLarge,
                BenchFastJsonSmall,
                // BenchDataContractLarge,
                // BenchDataContractSmall
                //BenchArgoJsonDeserialize,
                //BenchJSONDotNetDeserialize
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

                System.Console.WriteLine("{0}s", 
                    stopwatch.ElapsedMilliseconds / 1000.0);
            }
        }
    }
}