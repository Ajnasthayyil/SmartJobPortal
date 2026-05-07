using System;
using BCrypt.Net;

namespace GenHash
{
    class Program
    {
        static void Main(string[] args)
        {
            string pass = args.Length > 0 ? args[0] : "Ajnas@1234";
            Console.WriteLine(BCrypt.Net.BCrypt.HashPassword(pass));
        }
    }
}
