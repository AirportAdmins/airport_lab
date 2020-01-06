using System;

namespace AirportLibrary
{
    public static class Airport
    {
        public static class Something
        {
            public static class More
            {
                public const string Const = "Hi";
            }
        }

        public static void Main(String[] args)
        {
            Console.WriteLine(Airport.Something.More.Const);
        }
    }
}
