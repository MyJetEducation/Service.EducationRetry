using System;
using System.Threading.Tasks;
using ProtoBuf.Grpc.Client;
using Service.EducationRetry.Client;
using Service.EducationRetry.Grpc.Models;

namespace TestApp
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            GrpcClientFactory.AllowUnencryptedHttp2 = true;

            Console.Write("Press enter to start");
            Console.ReadLine();

            //var factory = new EducationRetryClientFactory("http://localhost:5001");
            //var client = factory.GetEducationRetryService();

            //var resp = await  client.SayHelloAsync(new IncreaseRetryCountGrpcRequest(){Name = "Alex"});
            //Console.WriteLine(resp?.Message);

            Console.WriteLine("End");
            Console.ReadLine();
        }
    }
}
